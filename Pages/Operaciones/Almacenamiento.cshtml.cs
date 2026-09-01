using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using AtilsonCargoSpa.Services;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class AlmacenamientoModel : PageModel
    {
        private readonly AtilsonContext _context;
        private readonly EmailService _emailService;
        private readonly IWebHostEnvironment _env;

        public AlmacenamientoModel(AtilsonContext context, EmailService emailService, IWebHostEnvironment env)
        {
            _context = context;
            _emailService = emailService;
            _env = env;
        }

        public IList<Operacione> Operaciones { get; set; } = default!;
        public SelectList ListaCadenas { get; set; } = default!;
        public List<ConductorAutoDto> ChoferesHistorial { get; set; } = new();

        public class ConductorAutoDto
        {
            public string? Nombre { get; set; }
            public string? Rut { get; set; }
            public string? Telefono { get; set; }
            public string? Patente { get; set; }
        }

        public async Task OnGetAsync()
        {
            Operaciones = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.OperacionesAlmacenamientos)
                    .ThenInclude(a => a.IdProveedorNavigation)
                .Where(o => o.Activo == 1)
                .OrderByDescending(o => o.Id)
                .Take(100)
                .ToListAsync();

            // Tip: 12 es el ID que manejas para Proveedores de Depósito/Patio
            var cadenas = await _context.Proveedores
                .Where(p => p.IdTipoProveedor == 12 && p.Activo == 1)
                .OrderBy(p => p.NombreProveedor)
                .ToListAsync();
            ListaCadenas = new SelectList(cadenas, "Id", "NombreProveedor");

            ChoferesHistorial = await _context.Conductores
                .Where(c => c.Activo == true)
                .Select(c => new ConductorAutoDto { Nombre = c.Nombre, Rut = c.Rut, Telefono = c.Telefono, Patente = c.Patente })
                .ToListAsync();
        }

        // ==========================================
        // HANDLER DE AUTOGUARDADO SEGURO
        // ==========================================
        public async Task<IActionResult> OnPostAutoGuardarAsync(int IdOperacion, int? IdProveedor, DateTime? FechaIngreso, string? ConductorIngresoNombre, string? ConductorIngresoRut, string? ConductorIngresoTelefono, string? CamionIngresoPatente, string? Comentarios, DateTime? FechaSalida, string? ConductorSalidaNombre, string? ConductorSalidaRut, string? ConductorSalidaTelefono, string? CamionSalidaPatente)
        {
            try
            {
                var operacion = await _context.Operaciones.Include(o => o.OperacionesAlmacenamientos).FirstOrDefaultAsync(o => o.Id == IdOperacion);
                if (operacion == null) return new JsonResult(new { success = false, message = "Operación no encontrada" });

                var almacen = operacion.OperacionesAlmacenamientos.FirstOrDefault();
                if (almacen == null)
                {
                    almacen = new OperacionesAlmacenamiento { IdOperacion = IdOperacion, FechaCreacion = System.DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema" };
                    _context.OperacionesAlmacenamientos.Add(almacen);
                }

                if (Request.Form.ContainsKey("IdProveedor")) almacen.IdProveedor = IdProveedor;
                if (Request.Form.ContainsKey("FechaIngreso")) almacen.FechaIngreso = FechaIngreso;
                if (Request.Form.ContainsKey("ConductorIngresoNombre")) almacen.ConductorIngresoNombre = ConductorIngresoNombre;
                if (Request.Form.ContainsKey("ConductorIngresoRut")) almacen.ConductorIngresoRut = ConductorIngresoRut;
                if (Request.Form.ContainsKey("ConductorIngresoTelefono")) almacen.ConductorIngresoTelefono = ConductorIngresoTelefono;
                if (Request.Form.ContainsKey("CamionIngresoPatente")) almacen.CamionIngresoPatente = CamionIngresoPatente;
                if (Request.Form.ContainsKey("Comentarios")) almacen.Comentarios = Comentarios;

                if (Request.Form.ContainsKey("FechaSalida")) almacen.FechaSalida = FechaSalida;
                if (Request.Form.ContainsKey("ConductorSalidaNombre")) almacen.ConductorSalidaNombre = ConductorSalidaNombre;
                if (Request.Form.ContainsKey("ConductorSalidaRut")) almacen.ConductorSalidaRut = ConductorSalidaRut;
                if (Request.Form.ContainsKey("ConductorSalidaTelefono")) almacen.ConductorSalidaTelefono = ConductorSalidaTelefono;
                if (Request.Form.ContainsKey("CamionSalidaPatente")) almacen.CamionSalidaPatente = CamionSalidaPatente;

                await _context.SaveChangesAsync();

                // --- DISPARADOR FINANCIERO (CON MANEJO DE TARIFAS VACÍAS) ---
                await SincronizarAlmacenamientoAFinanzasAsync(IdOperacion);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // HANDLER DE NOTIFICACIÓN DE INGRESO
        // ==========================================
        public async Task<IActionResult> OnPostNotificarIngresoAsync(int IdOperacion, int? IdProveedor, DateTime? FechaIngreso, string? ConductorIngresoNombre, string? ConductorIngresoRut, string? ConductorIngresoTelefono, string? CamionIngresoPatente, string? Comentarios, string? EmailDestinatario, string? EmailAsunto, string? EmailCuerpo)
        {
            try
            {
                var operacion = await _context.Operaciones.Include(o => o.OperacionesAlmacenamientos).FirstOrDefaultAsync(o => o.Id == IdOperacion);
                if (operacion == null) return new JsonResult(new { success = false, message = "Operación no encontrada" });

                var almacen = operacion.OperacionesAlmacenamientos.FirstOrDefault();
                if (almacen == null)
                {
                    almacen = new OperacionesAlmacenamiento { IdOperacion = IdOperacion, FechaCreacion = System.DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema" };
                    _context.OperacionesAlmacenamientos.Add(almacen);
                }

                if (Request.Form.ContainsKey("IdProveedor")) almacen.IdProveedor = IdProveedor;
                if (Request.Form.ContainsKey("FechaIngreso")) almacen.FechaIngreso = FechaIngreso;
                if (Request.Form.ContainsKey("ConductorIngresoNombre")) almacen.ConductorIngresoNombre = ConductorIngresoNombre;
                if (Request.Form.ContainsKey("ConductorIngresoRut")) almacen.ConductorIngresoRut = ConductorIngresoRut;
                if (Request.Form.ContainsKey("ConductorIngresoTelefono")) almacen.ConductorIngresoTelefono = ConductorIngresoTelefono;
                if (Request.Form.ContainsKey("CamionIngresoPatente")) almacen.CamionIngresoPatente = CamionIngresoPatente;
                if (Request.Form.ContainsKey("Comentarios")) almacen.Comentarios = Comentarios;
                await _context.SaveChangesAsync();

                // --- DISPARADOR FINANCIERO (CON MANEJO DE TARIFAS VACÍAS) ---
                await SincronizarAlmacenamientoAFinanzasAsync(IdOperacion);

                if (!string.IsNullOrWhiteSpace(EmailDestinatario) && !string.IsNullOrWhiteSpace(EmailCuerpo))
                {
                    string htmlFinal = $@"
                    <div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                        <div style='background: linear-gradient(135deg, #0f172a 0%, #6d2d9e 100%); padding: 25px; text-align: center;'>
                            <h2 style='color: #fff; margin: 0; letter-spacing: 1px;'>Notificación Logística de Almacenamiento</h2>
                        </div>
                        <div style='padding: 30px; background-color: #ffffff;'>
                            <p style='font-size: 14px; line-height: 1.6; color: #475569;'>{EmailCuerpo.Replace("\n", "<br>")}</p>
                            <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 30px 0;'>
                            <p style='font-size: 11px; color: #94a3b8; text-align: center; margin: 0;'>Atilson Cargo SpA.</p>
                        </div>
                    </div>";

                    await _emailService.EnviarCorreoAsync(EmailDestinatario, EmailAsunto ?? "Notificación de Almacenamiento", htmlFinal);
                }
                else
                {
                    return new JsonResult(new { success = false, message = "El campo de destino (Para) es obligatorio." });
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Error de Servidor: " + ex.Message });
            }
        }

        // ==========================================
        // HANDLER DE NOTIFICACIÓN DE SALIDA
        // ==========================================
        public async Task<IActionResult> OnPostNotificarSalidaAsync(int IdOperacion, int? IdProveedor, DateTime? FechaSalida, string? ConductorSalidaNombre, string? ConductorSalidaRut, string? ConductorSalidaTelefono, string? CamionSalidaPatente, string? EmailDestinatario, string? EmailAsunto, string? EmailCuerpo)
        {
            try
            {
                var operacion = await _context.Operaciones.Include(o => o.OperacionesAlmacenamientos).FirstOrDefaultAsync(o => o.Id == IdOperacion);
                if (operacion == null) return new JsonResult(new { success = false, message = "Operación no encontrada" });

                var almacen = operacion.OperacionesAlmacenamientos.FirstOrDefault();
                if (almacen == null)
                {
                    almacen = new OperacionesAlmacenamiento { IdOperacion = IdOperacion, FechaCreacion = System.DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema" };
                    _context.OperacionesAlmacenamientos.Add(almacen);
                }

                if (Request.Form.ContainsKey("IdProveedor")) almacen.IdProveedor = IdProveedor;
                if (Request.Form.ContainsKey("FechaSalida")) almacen.FechaSalida = FechaSalida;
                if (Request.Form.ContainsKey("ConductorSalidaNombre")) almacen.ConductorSalidaNombre = ConductorSalidaNombre;
                if (Request.Form.ContainsKey("ConductorSalidaRut")) almacen.ConductorSalidaRut = ConductorSalidaRut;
                if (Request.Form.ContainsKey("ConductorSalidaTelefono")) almacen.ConductorSalidaTelefono = ConductorSalidaTelefono;
                if (Request.Form.ContainsKey("CamionSalidaPatente")) almacen.CamionSalidaPatente = CamionSalidaPatente;
                await _context.SaveChangesAsync();

                // --- DISPARADOR FINANCIERO (CON MANEJO DE TARIFAS VACÍAS) ---
                await SincronizarAlmacenamientoAFinanzasAsync(IdOperacion);

                if (!string.IsNullOrWhiteSpace(EmailDestinatario) && !string.IsNullOrWhiteSpace(EmailCuerpo))
                {
                    string htmlFinal = $@"
                    <div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                        <div style='background: linear-gradient(135deg, #0f172a 0%, #6d2d9e 100%); padding: 25px; text-align: center;'>
                            <h2 style='color: #fff; margin: 0; letter-spacing: 1px;'>Notificación Logística de Retiro</h2>
                        </div>
                        <div style='padding: 30px; background-color: #ffffff;'>
                            <p style='font-size: 14px; line-height: 1.6; color: #475569;'>{EmailCuerpo.Replace("\n", "<br>")}</p>
                            <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 30px 0;'>
                            <p style='font-size: 11px; color: #94a3b8; text-align: center; margin: 0;'>Atilson Cargo SpA.</p>
                        </div>
                    </div>";

                    await _emailService.EnviarCorreoAsync(EmailDestinatario, EmailAsunto ?? "Notificación de Retiro", htmlFinal);
                }
                else
                {
                    return new JsonResult(new { success = false, message = "El campo de destino (Para) es obligatorio." });
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Error de Servidor: " + ex.Message });
            }
        }

        // =========================================================================================
        // MOTOR DE SINCRONIZACIÓN COMERCIAL -> FINANZAS (ESPEJO INTELIGENTE)
        // =========================================================================================
        private async Task SincronizarAlmacenamientoAFinanzasAsync(int idOperacion)
        {
            var op = await _context.Operaciones
                .Include(o => o.OperacionesAlmacenamientos)
                .Include(o => o.TransaccionesFinancieras)
                .FirstOrDefaultAsync(o => o.Id == idOperacion);

            if (op == null) return;

            var almacen = op.OperacionesAlmacenamientos.FirstOrDefault();
            int? idProveedor = almacen?.IdProveedor;
            int? idCliente = op.IdCliente;

            string usuario = User.Identity?.Name ?? "Sistema";
            DateTime ahora = DateTime.Now;

            var txsEgresoActuales = op.TransaccionesFinancieras
                .Where(t => t.TipoMovimiento == "EGRESO" && t.GrupoCobro == "Almacenamiento").ToList();

            var txsIngresoActuales = op.TransaccionesFinancieras
                .Where(t => t.TipoMovimiento == "INGRESO" && t.GrupoCobro == "Almacenamiento").ToList();

            // Si Operaciones asignó un proveedor de patio, DEBEMOS crear los cobros, existan tarifas o no.
            if (idProveedor.HasValue)
            {
                // Buscamos si Comercial dejó tarifas preconfiguradas
                var tarifasCosto = await _context.TarifasAlmacenamientos.Where(t => t.IdProveedor == idProveedor.Value && t.EsActiva).ToListAsync();
                var tarifasVenta = await _context.TarifasClientes.Where(t => t.IdCliente == idCliente && t.GrupoCobro == "Almacenamiento" && t.EsActiva).ToListAsync();

                // ----------------------------------------------------
                // 1. INYECTAR COSTO PROVEEDOR (EGRESO)
                // ----------------------------------------------------
                decimal montoCostoBase = 0m;
                decimal montoCostoReefer = 0m;
                string conceptoCostoBase = "Tarifa Base Almacenaje";

                if (tarifasCosto.Any())
                {
                    var costoPrincipal = tarifasCosto.First();
                    montoCostoBase = costoPrincipal.TarifaBase;
                    montoCostoReefer = costoPrincipal.TarifaConexionReefer;
                    conceptoCostoBase = $"Tarifa Almacenaje ({costoPrincipal.DiasLibres} Días Libres)";
                }

                // Generar/Actualizar Egreso Base
                var txEgresoBase = txsEgresoActuales.FirstOrDefault(t => (t.Concepto ?? "").Contains("Tarifa Almacenaje") || t.Concepto == "Tarifa Base Almacenaje");
                if (txEgresoBase == null)
                {
                    _context.TransaccionesFinancieras.Add(new TransaccionesFinanciera { IdOperacion = op.Id, GrupoCobro = "Almacenamiento", TipoMovimiento = "EGRESO", Concepto = conceptoCostoBase, MontoNeto = montoCostoBase, Moneda = "CLP", EstadoFila = montoCostoBase > 0 ? "PROVISIÓN" : "PENDIENTE VALORIZAR", FechaCreacion = ahora, UsuarioCreador = usuario, IdProveedor = idProveedor.Value });
                }
                else if (!txEgresoBase.TarifaManual)
                {
                    txEgresoBase.Concepto = conceptoCostoBase; txEgresoBase.MontoNeto = montoCostoBase; txEgresoBase.IdProveedor = idProveedor.Value; txEgresoBase.EstadoFila = montoCostoBase > 0 ? "PROVISIÓN" : "PENDIENTE VALORIZAR";
                }

                // ----------------------------------------------------
                // 2. INYECTAR VENTA CLIENTE (INGRESO)
                // ----------------------------------------------------
                decimal montoVentaBase = 0m;
                string monedaVentaBase = "CLP";
                string conceptoVentaBase = "Servicio de Almacenaje";

                var ventaPrincipal = tarifasVenta.FirstOrDefault(v => !(v.Concepto ?? "").ToUpper().Contains("REEFER"));
                if (ventaPrincipal != null)
                {
                    montoVentaBase = ventaPrincipal.PrecioPactado;
                    monedaVentaBase = ventaPrincipal.Moneda ?? "CLP";
                    conceptoVentaBase = ventaPrincipal.Concepto;
                }

                // Generar/Actualizar Ingreso Base
                var txIngresoBase = txsIngresoActuales.FirstOrDefault(t => t.Concepto == conceptoVentaBase || (t.Concepto ?? "").Contains("Almacenaje"));
                if (txIngresoBase == null)
                {
                    _context.TransaccionesFinancieras.Add(new TransaccionesFinanciera { IdOperacion = op.Id, GrupoCobro = "Almacenamiento", TipoMovimiento = "INGRESO", Concepto = conceptoVentaBase, MontoNeto = montoVentaBase, Moneda = monedaVentaBase, EstadoFila = montoVentaBase > 0 ? "PROVISIÓN" : "PENDIENTE VALORIZAR", FechaCreacion = ahora, UsuarioCreador = usuario, IdCliente = idCliente });
                }
                else if (!txIngresoBase.TarifaManual)
                {
                    txIngresoBase.Concepto = conceptoVentaBase; txIngresoBase.MontoNeto = montoVentaBase; txIngresoBase.Moneda = monedaVentaBase; txIngresoBase.EstadoFila = montoVentaBase > 0 ? "PROVISIÓN" : "PENDIENTE VALORIZAR";
                }

                // ----------------------------------------------------
                // 3. INYECCIÓN ESPECIAL SI EL CONTENEDOR ES REEFER
                // ----------------------------------------------------
                if (op.IdTipoCarga == 2)
                {
                    // Costo Reefer
                    string conceptoReeferCosto = "Conexión Reefer Almacén (x Hora)";
                    var txEgresoReefer = txsEgresoActuales.FirstOrDefault(t => (t.Concepto ?? "").Contains("Reefer"));
                    if (txEgresoReefer == null)
                    {
                        _context.TransaccionesFinancieras.Add(new TransaccionesFinanciera { IdOperacion = op.Id, GrupoCobro = "Almacenamiento", TipoMovimiento = "EGRESO", Concepto = conceptoReeferCosto, MontoNeto = montoCostoReefer, Moneda = "CLP", EstadoFila = montoCostoReefer > 0 ? "PROVISIÓN" : "PENDIENTE VALORIZAR", FechaCreacion = ahora, UsuarioCreador = usuario, IdProveedor = idProveedor.Value });
                    }
                    else if (!txEgresoReefer.TarifaManual)
                    {
                        txEgresoReefer.MontoNeto = montoCostoReefer; txEgresoReefer.EstadoFila = montoCostoReefer > 0 ? "PROVISIÓN" : "PENDIENTE VALORIZAR";
                    }

                    // Venta Reefer
                    decimal montoVentaReefer = 0m;
                    string monedaVentaReefer = "CLP";
                    string conceptoVentaReefer = "Conexión Reefer Almacén (Venta x Hora)";

                    var ventaReefer = tarifasVenta.FirstOrDefault(v => (v.Concepto ?? "").ToUpper().Contains("REEFER"));
                    if (ventaReefer != null)
                    {
                        montoVentaReefer = ventaReefer.PrecioPactado;
                        monedaVentaReefer = ventaReefer.Moneda ?? "CLP";
                        conceptoVentaReefer = ventaReefer.Concepto;
                    }

                    var txIngresoReefer = txsIngresoActuales.FirstOrDefault(t => (t.Concepto ?? "").Contains("Reefer"));
                    if (txIngresoReefer == null)
                    {
                        _context.TransaccionesFinancieras.Add(new TransaccionesFinanciera { IdOperacion = op.Id, GrupoCobro = "Almacenamiento", TipoMovimiento = "INGRESO", Concepto = conceptoVentaReefer, MontoNeto = montoVentaReefer, Moneda = monedaVentaReefer, EstadoFila = montoVentaReefer > 0 ? "PROVISIÓN" : "PENDIENTE VALORIZAR", FechaCreacion = ahora, UsuarioCreador = usuario, IdCliente = idCliente });
                    }
                    else if (!txIngresoReefer.TarifaManual)
                    {
                        txIngresoReefer.Concepto = conceptoVentaReefer; txIngresoReefer.MontoNeto = montoVentaReefer; txIngresoReefer.Moneda = monedaVentaReefer; txIngresoReefer.EstadoFila = montoVentaReefer > 0 ? "PROVISIÓN" : "PENDIENTE VALORIZAR";
                    }
                }
            }
            else
            {
                // Si Operaciones limpia el campo y lo deja "Sin Asignar", borramos los cobros automáticos para no ensuciar finanzas
                foreach (var tx in txsEgresoActuales.Where(t => !t.TarifaManual)) _context.TransaccionesFinancieras.Remove(tx);
                foreach (var tx in txsIngresoActuales.Where(t => !t.TarifaManual)) _context.TransaccionesFinancieras.Remove(tx);
            }

            await _context.SaveChangesAsync();
        }
        // ==========================================
        // HANDLER PARA SUBIR COMPROBANTE DE PAGO
        // ==========================================
        public async Task<IActionResult> OnPostSubirComprobantePatioAsync(int IdOperacion, IFormFile? ArchivoTransferencia)
        {
            try
            {
                var operacion = await _context.Operaciones.Include(o => o.OperacionesAlmacenamientos).FirstOrDefaultAsync(o => o.Id == IdOperacion);
                if (operacion == null) return new JsonResult(new { success = false, message = "Operación no encontrada." });

                var almacen = operacion.OperacionesAlmacenamientos.FirstOrDefault();
                if (almacen == null)
                {
                    almacen = new OperacionesAlmacenamiento { IdOperacion = IdOperacion, FechaCreacion = System.DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema" };
                    _context.OperacionesAlmacenamientos.Add(almacen);
                }

                if (ArchivoTransferencia != null && ArchivoTransferencia.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "evidencias");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = $"{IdOperacion}_ComprobantePatio_{DateTime.Now.Ticks}_{Path.GetFileName(ArchivoTransferencia.FileName).Replace(" ", "_")}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ArchivoTransferencia.CopyToAsync(stream);
                    }

                    // USANDO EL NOMBRE EXACTO DE TU TABLA SQL
                    almacen.RutaArchivoTransferencia = $"/uploads/evidencias/{uniqueFileName}";

                    await _context.SaveChangesAsync();
                    return new JsonResult(new { success = true, ruta = almacen.RutaArchivoTransferencia });
                }

                return new JsonResult(new { success = false, message = "No se recibió ningún archivo." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}