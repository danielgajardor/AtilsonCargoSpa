using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Text;

namespace AtilsonCargoSpa.Pages.Finanzas
{
    public class IndexModel : PageModel
    {
        private readonly AtilsonContext _context;

        public IndexModel(AtilsonContext context)
        {
            _context = context;
        }

        public IList<Operacione> Operaciones { get; set; } = default!;

        // --- FILTROS MINIMALISTAS ---
        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FechaInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FechaFin { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FilterEstadoFacturacion { get; set; } = "ALL";

        [BindProperty(SupportsGet = true)]
        public string FilterEstadoPago { get; set; } = "ALL";

        [BindProperty(SupportsGet = true)]
        public int? FilterClienteId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterProveedorId { get; set; }

        public List<Cliente> ClientesList { get; set; } = new();
        public List<Proveedore> ProveedoresList { get; set; } = new();

        // --- KPIS GERENCIALES MULTI-DIVISA ---
        public decimal TotalCostosMaritimosUSD { get; set; } = 0;
        public decimal TotalCostosTerrestresCLP { get; set; } = 0;
        public decimal TotalCostosGateCLP { get; set; } = 0;
        public decimal TotalCostosDocumentalesUSD { get; set; } = 0;
        public decimal TotalExtracostosUSD { get; set; } = 0;
        public decimal TotalExtracostosCLP { get; set; } = 0;

        public decimal GranTotalEgresosUSD { get; set; } = 0;
        public decimal GranTotalEgresosCLP { get; set; } = 0;
        public decimal GranTotalIngresosUSD { get; set; } = 0;
        public decimal GranTotalIngresosCLP { get; set; } = 0;

        public decimal ProfitNetoUSD => GranTotalIngresosUSD - GranTotalEgresosUSD;
        public decimal ProfitNetoCLP => GranTotalIngresosCLP - GranTotalEgresosCLP;

        public async Task OnGetAsync()
      
        {

            ClientesList = await _context.Clientes.OrderBy(c => c.RazonSocial).ToListAsync();
            ProveedoresList = await _context.Proveedores.OrderBy(p => p.NombreProveedor).ToListAsync();
            var query = _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.Finanzasoperacions)
                .Include(o => o.ExtracostosOperacions)
                .Include(o => o.TransaccionesFinancieras)
                    .ThenInclude(t => t.IdProveedorNavigation)
                .Include(o => o.TransaccionesFinancieras)
                    .ThenInclude(t => t.IdClienteNavigation)
                .Where(o => !o.IsDeleted)
                .AsQueryable();

            // ✅ Ahora (solo Booking o Contenedor)
            if (!string.IsNullOrEmpty(SearchString))
            {
                string s = SearchString.ToLower().Trim();
                query = query.Where(o =>
                    (o.NumeroBooking != null && o.NumeroBooking.ToLower().Contains(s)) ||
                    (o.NumeroContenedor != null && o.NumeroContenedor.ToLower().Contains(s))
                );
            }

            if (FechaInicio.HasValue) query = query.Where(o => o.FechaCreacion >= FechaInicio.Value);
            if (FechaFin.HasValue)
            {
                var fechaFinExacta = FechaFin.Value.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.FechaCreacion <= fechaFinExacta);
            }
            if (FilterClienteId.HasValue)
                query = query.Where(o => o.IdCliente == FilterClienteId.Value);

            if (FilterProveedorId.HasValue)
                query = query.Where(o => o.TransaccionesFinancieras.Any(t => t.IdProveedor == FilterProveedorId.Value));

            var listaBruta = await query.OrderByDescending(o => o.Id).ToListAsync();
            var listaFiltrada = new List<Operacione>();

            foreach (var op in listaBruta)
            {
                var trans = op.TransaccionesFinancieras ?? new List<TransaccionesFinanciera>();
                var fin = op.Finanzasoperacions?.FirstOrDefault();

                bool tieneFacturaVenta = trans.Any(t => t.TipoMovimiento == "INGRESO" && (t.EstadoFila == "FACTURADO" || t.EstadoFila == "CONCILIADO"));
                bool tienePagosPendientes = trans.Any(t => t.TipoMovimiento == "EGRESO" && (t.EstadoFila == "PROVISIÓN" || t.EstadoFila == "PROVISION"));

                if (FilterEstadoFacturacion == "PENDIENTE" && tieneFacturaVenta) continue;
                if (FilterEstadoFacturacion == "FACTURADO" && !tieneFacturaVenta) continue;
                if (FilterEstadoPago == "PENDIENTE" && !tienePagosPendientes) continue;
                if (FilterEstadoPago == "PAGADO" && tienePagosPendientes) continue;

                listaFiltrada.Add(op);

                decimal cMarUsd = trans.Where(t => t.TipoMovimiento == "EGRESO" && (t.GrupoCobro == "Marítimo" || t.GrupoCobro == "MARITIMO") && t.Moneda == "USD").Sum(t => t.MontoNeto);
                if (cMarUsd == 0) cMarUsd = fin?.CostoMaritimoNeto ?? 0m;

                decimal cTerrClp = trans.Where(t => t.TipoMovimiento == "EGRESO" && t.GrupoCobro == "Terrestre" && t.Moneda == "CLP").Sum(t => t.MontoNeto);
                if (cTerrClp == 0) cTerrClp = fin?.CostoTerrestreNeto ?? 0m;

                decimal cDocUsd = trans.Where(t => t.TipoMovimiento == "EGRESO" && t.GrupoCobro == "Documental" && t.Moneda == "USD").Sum(t => t.MontoNeto);
                if (cDocUsd == 0) cDocUsd = fin?.CostoAgenciaNeto ?? 0m;

                decimal cGateClp = trans.Where(t => t.TipoMovimiento == "EGRESO" && t.GrupoCobro == "Gate" && t.Moneda == "CLP").Sum(t => t.MontoNeto);
                if (cGateClp == 0) cGateClp = fin?.CostoGateNeto ?? 0m;

                decimal cExtUsd = trans.Where(t => t.TipoMovimiento == "EGRESO" && t.GrupoCobro == "Extracosto" && t.Moneda == "USD").Sum(t => t.MontoNeto);
                if (cExtUsd == 0) cExtUsd = op.ExtracostosOperacions?.Where(e => e.Moneda == "USD").Sum(e => e.Monto) ?? 0m;

                decimal cExtClp = trans.Where(t => t.TipoMovimiento == "EGRESO" && t.GrupoCobro == "Extracosto" && t.Moneda == "CLP").Sum(t => t.MontoNeto);
                if (cExtClp == 0) cExtClp = op.ExtracostosOperacions?.Where(e => e.Moneda == "CLP").Sum(e => e.Monto) ?? 0m;

                TotalCostosMaritimosUSD += cMarUsd;
                TotalCostosTerrestresCLP += cTerrClp;
                TotalCostosDocumentalesUSD += cDocUsd;
                TotalCostosGateCLP += cGateClp;
                TotalExtracostosUSD += cExtUsd;
                TotalExtracostosCLP += cExtClp;

                decimal ingUsd = trans.Where(t => t.TipoMovimiento == "INGRESO" && t.Moneda == "USD").Sum(t => t.MontoNeto);
                if (ingUsd == 0 && fin != null) ingUsd += (fin.VentaMaritimo ?? 0m) + (fin.VentaDocumental ?? 0m);

                decimal ingClp = trans.Where(t => t.TipoMovimiento == "INGRESO" && t.Moneda == "CLP").Sum(t => t.MontoNeto);
                if (ingClp == 0 && fin != null) ingClp += (fin.VentaTerrestre ?? 0m) + (fin.VentaGate ?? 0m);

                GranTotalIngresosUSD += ingUsd;
                GranTotalIngresosCLP += ingClp;
            }

            Operaciones = listaFiltrada;
            GranTotalEgresosUSD = TotalCostosMaritimosUSD + TotalCostosDocumentalesUSD + TotalExtracostosUSD;
            GranTotalEgresosCLP = TotalCostosTerrestresCLP + TotalCostosGateCLP + TotalExtracostosCLP;
        }

        // === EXPORTACIÓN EXCEL (.CSV) ===
        // === EXPORTACIÓN EXCEL (.CSV) OPTIMIZADA PARA LATAM ===
        public async Task<IActionResult> OnGetExportExcelAsync(string? ids)
        {
            var query = _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.Finanzasoperacions)
                .Include(o => o.TransaccionesFinancieras)
                .Where(o => !o.IsDeleted);

            if (!string.IsNullOrEmpty(ids))
            {
                var idList = ids.Split(',').Select(int.Parse).ToList();
                query = query.Where(o => idList.Contains(o.Id));
            }

            var operaciones = await query.OrderByDescending(o => o.Id).ToListAsync();
            var sb = new StringBuilder();

            // Cabeceras claras
            sb.AppendLine("ID_Operacion;Numero_Booking;Cliente_Mandante;Naviera;Venta_USD;Costo_USD;Profit_USD;Venta_CLP;Costo_CLP;Profit_CLP;Estado_Financiero");

            // Forzamos cultura chilena para que los decimales usen coma (,) y Excel no se vuelva loco
            var culturaCL = new System.Globalization.CultureInfo("es-CL");

            foreach (var o in operaciones)
            {
                var fin = o.Finanzasoperacions?.FirstOrDefault();
                var egrUsd = o.TransaccionesFinancieras?.Where(t => t.TipoMovimiento == "EGRESO" && t.Moneda == "USD").Sum(t => t.MontoNeto) ?? (fin?.CostoMaritimoNeto ?? 0m) + (fin?.CostoAgenciaNeto ?? 0m);
                var ingUsd = o.TransaccionesFinancieras?.Where(t => t.TipoMovimiento == "INGRESO" && t.Moneda == "USD").Sum(t => t.MontoNeto) ?? (fin?.VentaMaritimo ?? 0m) + (fin?.VentaDocumental ?? 0m);
                var egrClp = o.TransaccionesFinancieras?.Where(t => t.TipoMovimiento == "EGRESO" && t.Moneda == "CLP").Sum(t => t.MontoNeto) ?? (fin?.CostoTerrestreNeto ?? 0m) + (fin?.CostoGateNeto ?? 0m);
                var ingClp = o.TransaccionesFinancieras?.Where(t => t.TipoMovimiento == "INGRESO" && t.Moneda == "CLP").Sum(t => t.MontoNeto) ?? (fin?.VentaTerrestre ?? 0m) + (fin?.VentaGate ?? 0m);

                string estado = o.TransaccionesFinancieras?.Any(t => t.EstadoFila == "FACTURADO" || t.EstadoFila == "CONCILIADO") == true ? "FACTURADO" : "PENDIENTE";

                // Armado de la fila con el formato seguro
                sb.AppendLine($"{o.Id};\"{o.NumeroBooking}\";\"{o.IdClienteNavigation?.RazonSocial}\";\"{o.IdNavieraNavigation?.NombreNaviera}\";{ingUsd.ToString("F2", culturaCL)};{egrUsd.ToString("F2", culturaCL)};{(ingUsd - egrUsd).ToString("F2", culturaCL)};{ingClp.ToString("F0", culturaCL)};{egrClp.ToString("F0", culturaCL)};{(ingClp - egrClp).ToString("F0", culturaCL)};\"{estado}\"");
            }

            // BOM UTF-8 para que Excel lea los tildes y las "Ñ" correctamente
            byte[] preamble = Encoding.UTF8.GetPreamble();
            byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
            return File(preamble.Concat(data).ToArray(), "text/csv", $"Reporte_Finanzas_Atilson_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        // === EXPORTACIÓN PDF EJECUTIVO ULTRA-MINIMALISTA (REGLA 3 COLORES) AQUI PROBAMOS TEXTO DE COMENTARIOS SOLO PARA PROBAR
        // ===
        // === EXPORTACIÓN PDF EJECUTIVO ULTRA-MINIMALISTA ===
        public async Task<IActionResult> OnGetExportPdfAsync(string? ids)
        {
            var query = _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.Finanzasoperacions)
                .Include(o => o.TransaccionesFinancieras)
                .Where(o => !o.IsDeleted);

            if (!string.IsNullOrEmpty(ids))
            {
                var idList = ids.Split(',').Select(int.Parse).ToList();
                query = query.Where(o => idList.Contains(o.Id));
            }

            var operaciones = await query.OrderByDescending(o => o.Id).ToListAsync();
            decimal sumIngUsd = 0, sumEgrUsd = 0, sumIngClp = 0, sumEgrClp = 0;
            var sb = new StringBuilder();

            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Reporte de Auditoría - ATILSON</title>");
            sb.Append("<style>");
            sb.Append("@page { size: A4 landscape; margin: 15mm; }");
            sb.Append("body { font-family: 'Segoe UI', system-ui, Helvetica, Arial, sans-serif; color: #1e293b; margin: 0; padding: 0; font-size: 11px; background: #ffffff; -webkit-print-color-adjust: exact; print-color-adjust: exact; }");
            sb.Append(".header { display: flex; justify-content: space-between; align-items: flex-end; border-bottom: 3px solid #0f172a; padding-bottom: 15px; margin-bottom: 25px; }");
            sb.Append(".logo-title { font-size: 26px; font-weight: 900; color: #0f172a; letter-spacing: -1px; margin: 0; }");
            sb.Append(".logo-sub { font-size: 11px; font-weight: 700; color: #64748b; text-transform: uppercase; letter-spacing: 1.5px; margin-top: 4px; }");
            sb.Append(".doc-meta { text-align: right; font-size: 11px; color: #475569; line-height: 1.5; } .doc-meta strong { color: #0f172a; }");

            // Cajas de resumen (Estilo Dashboard en el PDF)
            sb.Append(".summary-grid { display: flex; gap: 15px; margin-bottom: 30px; }");
            sb.Append(".summary-box { flex: 1; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 15px; border-left: 5px solid #0f172a; }");
            sb.Append(".box-label { font-size: 10px; font-weight: 800; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px; display: block; margin-bottom: 8px; }");
            sb.Append(".val-main { font-size: 20px; font-weight: 900; font-family: monospace; line-height: 1; display: block; }");
            sb.Append(".val-sub { font-size: 12px; font-weight: 700; color: #64748b; font-family: monospace; margin-top: 6px; padding-top: 6px; border-top: 1px dashed #cbd5e1; display: block; }");

            // Tabla
            sb.Append("table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
            sb.Append("th { background: #0f172a; color: #ffffff; font-size: 10px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.5px; padding: 12px; text-align: left; }");
            sb.Append("td { padding: 12px; border-bottom: 1px solid #e2e8f0; font-size: 11px; vertical-align: middle; }");
            sb.Append("tr:nth-child(even) td { background-color: #f8fafc; }");
            sb.Append(".num-td { font-family: monospace; font-weight: 700; text-align: right; }");
            sb.Append(".val-usd { font-size: 13px; font-weight: 800; display: block; } .val-clp { font-size: 10px; color: #64748b; display: block; margin-top: 3px; }");
            sb.Append(".badge { font-size: 9px; font-weight: 800; padding: 4px 8px; border-radius: 4px; text-transform: uppercase; display: inline-block; border: 1px solid transparent; }");
            sb.Append(".badge-ok { background: #d1fae5; color: #047857; border-color: #a7f3d0; } .badge-pend { background: #f1f5f9; color: #64748b; border-color: #cbd5e1; }");
            sb.Append(".footer { margin-top: 40px; padding-top: 15px; border-top: 2px solid #e2e8f0; display: flex; justify-content: space-between; font-size: 10px; color: #94a3b8; font-weight: 600; }");
            sb.Append("</style></head><body>");

            // --- Cálculos Previos para el Resumen ---
            foreach (var o in operaciones)
            {
                var fin = o.Finanzasoperacions?.FirstOrDefault();
                sumEgrUsd += o.TransaccionesFinancieras?.Where(t => t.TipoMovimiento == "EGRESO" && t.Moneda == "USD").Sum(t => t.MontoNeto) ?? (fin?.CostoMaritimoNeto ?? 0m) + (fin?.CostoAgenciaNeto ?? 0m);
                sumIngUsd += o.TransaccionesFinancieras?.Where(t => t.TipoMovimiento == "INGRESO" && t.Moneda == "USD").Sum(t => t.MontoNeto) ?? (fin?.VentaMaritimo ?? 0m) + (fin?.VentaDocumental ?? 0m);
                sumEgrClp += o.TransaccionesFinancieras?.Where(t => t.TipoMovimiento == "EGRESO" && t.Moneda == "CLP").Sum(t => t.MontoNeto) ?? (fin?.CostoTerrestreNeto ?? 0m) + (fin?.CostoGateNeto ?? 0m);
                sumIngClp += o.TransaccionesFinancieras?.Where(t => t.TipoMovimiento == "INGRESO" && t.Moneda == "CLP").Sum(t => t.MontoNeto) ?? (fin?.VentaTerrestre ?? 0m) + (fin?.VentaGate ?? 0m);
            }
            decimal profUsd = sumIngUsd - sumEgrUsd;
            decimal profClp = sumIngClp - sumEgrClp;

            // --- HTML Header ---
            sb.Append("<div class='header'>");
            sb.Append("<div><h1 class='logo-title'>ATILSON CARGO SPA</h1><div class='logo-sub'>Reporte Gerencial de Rentabilidad (P&L)</div></div>");
            sb.Append($"<div class='doc-meta'>Fecha Emisión: <strong>{DateTime.Now:dd/MM/yyyy HH:mm}</strong><br>Total Registros: <strong>{operaciones.Count}</strong><br>Usuario: <strong>{User.Identity?.Name ?? "Sistema"}</strong></div>");
            sb.Append("</div>");

            // --- HTML Summary Cards ---
            sb.Append("<div class='summary-grid'>");
            sb.Append($"<div class='summary-box' style='border-color: #0284c7;'><span class='box-label'>Ingresos (Ventas)</span><span class='val-main' style='color:#0284c7;'>USD ${sumIngUsd:N2}</span><span class='val-sub'>CLP ${sumIngClp:N0}</span></div>");
            sb.Append($"<div class='summary-box' style='border-color: #dc2626;'><span class='box-label'>Egresos (Costos)</span><span class='val-main' style='color:#dc2626;'>USD ${sumEgrUsd:N2}</span><span class='val-sub'>CLP ${sumEgrClp:N0}</span></div>");
            sb.Append($"<div class='summary-box' style='border-color: {(profUsd >= 0 ? "#10b981" : "#dc2626")}; background: {(profUsd >= 0 ? "#ecfdf5" : "#fef2f2")};'><span class='box-label'>Profit Neto Consolidado</span><span class='val-main' style='color:{(profUsd >= 0 ? "#059669" : "#dc2626")};'>USD ${profUsd:N2}</span><span class='val-sub' style='color:{(profClp >= 0 ? "#059669" : "#dc2626")}; border-color: {(profUsd >= 0 ? "#a7f3d0" : "#fecaca")};'>CLP ${profClp:N0}</span></div>");
            sb.Append("</div>");

            // --- HTML Table ---
            sb.Append("<table><thead><tr>");
            sb.Append("<th style='width:6%;'>ID</th><th style='width:20%;'>Booking / Naviera</th><th style='width:25%;'>Cliente Mandante</th><th style='width:14%;text-align:right;'>Venta</th><th style='width:14%;text-align:right;'>Costo</th><th style='width:14%;text-align:right;'>Rentabilidad</th><th style='width:7%;text-align:center;'>Estado</th>");
            sb.Append("</tr></thead><tbody>");

            foreach (var o in operaciones)
            {
                var fin = o.Finanzasoperacions?.FirstOrDefault();
                var eUsd = o.TransaccionesFinancieras?.Where(t => t.TipoMovimiento == "EGRESO" && t.Moneda == "USD").Sum(t => t.MontoNeto) ?? (fin?.CostoMaritimoNeto ?? 0m) + (fin?.CostoAgenciaNeto ?? 0m);
                var iUsd = o.TransaccionesFinancieras?.Where(t => t.TipoMovimiento == "INGRESO" && t.Moneda == "USD").Sum(t => t.MontoNeto) ?? (fin?.VentaMaritimo ?? 0m) + (fin?.VentaDocumental ?? 0m);
                var eClp = o.TransaccionesFinancieras?.Where(t => t.TipoMovimiento == "EGRESO" && t.Moneda == "CLP").Sum(t => t.MontoNeto) ?? (fin?.CostoTerrestreNeto ?? 0m) + (fin?.CostoGateNeto ?? 0m);
                var iClp = o.TransaccionesFinancieras?.Where(t => t.TipoMovimiento == "INGRESO" && t.Moneda == "CLP").Sum(t => t.MontoNeto) ?? (fin?.VentaTerrestre ?? 0m) + (fin?.VentaGate ?? 0m);

                decimal pUsd = iUsd - eUsd;
                decimal pClp = iClp - eClp;
                bool fact = o.TransaccionesFinancieras?.Any(t => t.EstadoFila == "FACTURADO" || t.EstadoFila == "CONCILIADO") == true;
                string badge = fact ? "<span class='badge badge-ok'>FACTURADO</span>" : "<span class='badge badge-pend'>PENDIENTE</span>";

                sb.Append("<tr>");
                sb.Append($"<td><strong>#{o.Id}</strong></td>");
                sb.Append($"<td><strong style='color:#0f172a; font-size:12px;'>{o.NumeroBooking}</strong><br><span style='color:#64748b; font-size:10px;'>{o.IdNavieraNavigation?.NombreNaviera ?? "S/N"}</span></td>");
                sb.Append($"<td style='font-weight:700; color:#334155;'>{o.IdClienteNavigation?.RazonSocial ?? "Sin Asignar"}</td>");
                sb.Append($"<td class='num-td'><span class='val-usd' style='color:#0284c7;'>${iUsd:N2}</span><span class='val-clp'>CLP ${iClp:N0}</span></td>");
                sb.Append($"<td class='num-td'><span class='val-usd' style='color:#dc2626;'>${eUsd:N2}</span><span class='val-clp'>CLP ${eClp:N0}</span></td>");
                sb.Append($"<td class='num-td'><span class='val-usd' style='color:{(pUsd >= 0 ? "#059669" : "#dc2626")};'>${pUsd:N2}</span><span class='val-clp' style='color:{(pClp >= 0 ? "#059669" : "#dc2626")};'>CLP ${pClp:N0}</span></td>");
                sb.Append($"<td style='text-align:center;'>{badge}</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");

            // --- Footer ---
            sb.Append("<div class='footer'><span>ATILSON CARGO SPA — ERP Logístico & Financiero</span><span>Documento Confidencial de Auditoría</span></div>");

            // Script para abrir diálogo de impresión automáticamente y cerrar al terminar
            sb.Append("<script>window.onload = function() { window.print(); setTimeout(function(){ window.close(); }, 500); }</script></body></html>");

            return Content(sb.ToString(), "text/html", Encoding.UTF8);
        }

        public async Task<IActionResult> OnPostConciliarTransaccionAsync(int idTransaccion, string numeroDocumento, decimal montoNeto)
        {
            var tx = await _context.TransaccionesFinancieras.FindAsync(idTransaccion);
            if (tx != null)
            {
                tx.NumeroDocumento = numeroDocumento;
                tx.MontoNeto = montoNeto;
                tx.EstadoFila = "CONCILIADO";
                tx.FechaModificacion = DateTime.Now;
                string usuario = User.Identity?.Name ?? "Finanzas";
                tx.UsuarioModificador = usuario;

                var fin = await _context.Finanzasoperacions.FirstOrDefaultAsync(f => f.IdOperacion == tx.IdOperacion);
                if (fin != null)
                {
                    string grupo = (tx.GrupoCobro ?? "").ToUpper();
                    if (tx.TipoMovimiento == "EGRESO")
                    {
                        if (grupo == "MARÍTIMO" || grupo == "MARITIMO") fin.CostoMaritimoNeto = montoNeto;
                        else if (grupo == "TERRESTRE") { fin.CostoTerrestreNeto = montoNeto; fin.CostoTerrestreManual = true; }
                        else if (grupo == "GATE") { fin.CostoGateNeto = montoNeto; fin.CostoGateManual = true; }
                        else if (grupo == "DOCUMENTAL") fin.CostoAgenciaNeto = montoNeto;
                    }
                    else if (tx.TipoMovimiento == "INGRESO")
                    {
                        if (grupo == "MARÍTIMO" || grupo == "MARITIMO") fin.VentaMaritimo = montoNeto;
                        else if (grupo == "TERRESTRE") fin.VentaTerrestre = montoNeto;
                        else if (grupo == "GATE") fin.VentaGate = montoNeto;
                        else if (grupo == "DOCUMENTAL") fin.VentaDocumental = montoNeto;
                    }
                    fin.FechaModificacion = DateTime.Now;
                    fin.UsuarioModificador = usuario;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = $"Factura N° {numeroDocumento} conciliada y contabilizada.";
            }
            return RedirectToPage("./Index");
        }
    }
}