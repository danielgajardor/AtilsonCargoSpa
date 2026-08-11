using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AtilsonCargoSpa.Pages.Finanzas
{
    public class SetCobroModel : PageModel
    {
        private readonly AtilsonContext _context;

        public SetCobroModel(AtilsonContext context)
        {
            _context = context;
        }

        public Operacione Operacion { get; set; } = default!;
        public Finanzasoperacion? Finanzas { get; set; }

        // El "Libro Mayor" de la operación
        public List<TransaccionesFinanciera> FacturasFlete { get; set; } = new();
        public List<TransaccionesFinanciera> NotasCobro { get; set; } = new();

        // Reagregamos Extracostos por si tu HTML aún intenta leerlo en alguna línea
        public List<ExtracostosOperacion> Extracostos { get; set; } = new();

        // Totales consolidados para la portada de liquidación
        public decimal TotalFleteUSD { get; set; }
        public decimal TotalFleteCLP { get; set; }
        public decimal TotalExtrasUSD { get; set; }
        public decimal TotalExtrasCLP { get; set; }
        public decimal GranTotalUSD { get; set; }
        public decimal GranTotalCLP { get; set; }

        // --- NUEVAS PROPIEDADES PARA PAGOS PARCIALES ---
        public decimal TotalPagadoUSD { get; set; }
        public decimal TotalPagadoCLP { get; set; }
        public decimal SaldoPendienteUSD { get; set; }
        public decimal SaldoPendienteCLP { get; set; }
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var op = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.Finanzasoperacions)
                .Include(o => o.TransaccionesFinancieras)
                .Include(o => o.ExtracostosOperacions) // Incluimos esto por seguridad
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (op == null) return NotFound();

            Operacion = op;
            Finanzas = op.Finanzasoperacions.FirstOrDefault();
            Extracostos = op.ExtracostosOperacions?.ToList() ?? new();

            // 1. OBTENER SOLO LOS INGRESOS (LO QUE SE LE COBRA AL CLIENTE)
            var ingresos = op.TransaccionesFinancieras?
                .Where(t => t.TipoMovimiento == "INGRESO")
                .ToList() ?? new();

            // 2. SEPARAR FLETES/SERVICIOS BASE vs EXTRACOSTOS
            FacturasFlete = ingresos.Where(t => t.GrupoCobro?.ToUpper() != "EXTRACOSTO").ToList();
            NotasCobro = ingresos.Where(t => t.GrupoCobro?.ToUpper() == "EXTRACOSTO").ToList();

            // 3. CALCULAR TOTALES EXACTOS (Sin el ?? porque tu base de datos ya asegura que no son nulos)
            TotalFleteUSD = FacturasFlete.Where(t => t.Moneda == "USD").Sum(t => t.MontoNeto);
            TotalFleteCLP = FacturasFlete.Where(t => t.Moneda == "CLP").Sum(t => t.MontoNeto);

            TotalExtrasUSD = NotasCobro.Where(t => t.Moneda == "USD").Sum(t => t.MontoNeto);
            TotalExtrasCLP = NotasCobro.Where(t => t.Moneda == "CLP").Sum(t => t.MontoNeto);

            // 4. GRAN TOTAL
            GranTotalUSD = TotalFleteUSD + TotalExtrasUSD;
            GranTotalCLP = TotalFleteCLP + TotalExtrasCLP;

            // 5. CÁLCULO DE ABONOS Y SALDOS PENDIENTES (MAGIA ATILSON)
            var todosIngresos = FacturasFlete.Concat(NotasCobro).ToList();

            TotalPagadoUSD = todosIngresos.Where(t => t.Moneda == "USD" && (t.EstadoFila == "PAGADO" || t.EstadoFila == "CONCILIADO")).Sum(t => t.MontoNeto);
            TotalPagadoCLP = todosIngresos.Where(t => t.Moneda == "CLP" && (t.EstadoFila == "PAGADO" || t.EstadoFila == "CONCILIADO")).Sum(t => t.MontoNeto);

            SaldoPendienteUSD = GranTotalUSD - TotalPagadoUSD;
            SaldoPendienteCLP = GranTotalCLP - TotalPagadoCLP;

            return Page();
        }
    }
}