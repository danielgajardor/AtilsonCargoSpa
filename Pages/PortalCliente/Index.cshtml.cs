using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AtilsonCargoSpa.Pages.PortalCliente
{
    public class IndexModel : PageModel
    {
        private readonly AtilsonContext _context;

        public IndexModel(AtilsonContext context)
        {
            _context = context;
        }

        // 1. LA LISTA BLINDADA DE OPERACIONES DEL CLIENTE
        public IList<Operacione> OperacionesCliente { get; set; } = default!;

        // 2. VARIABLES PARA LAS TARJETAS (KPIs) SUPERIORES
        public int CargasActivas { get; set; }
        public int EmbarquesFinalizados { get; set; }
        public int AccionesRequeridas { get; set; }
        public decimal SaldoPendienteTotal { get; set; }

        // 3. BUSCADOR MINIMALISTA
        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // --- EL ESCUDO DE SEGURIDAD AUTOMATIZADO ---
            int idClienteLogueado = 0;

            // Leemos el Claim "IdCliente" que guardamos en la cookie al iniciar sesión
            var claimCliente = User.FindFirst("IdCliente")?.Value;

            if (!string.IsNullOrEmpty(claimCliente) && int.TryParse(claimCliente, out int parsedId))
            {
                idClienteLogueado = parsedId;
            }
            else
            {
                // Si por alguna razón el usuario no tiene cliente asociado, lo mandamos al Login por seguridad
                return RedirectToPage("/Auth/Login"); // Ajusta la ruta de tu login si es distinta
            }

            // Construimos la consulta trayendo solo lo que le pertenece a ESTE cliente en sesión
            var query = _context.Operaciones
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.IdPuertoOrigenNavigation)
                .Include(o => o.IdPuertoDestinoNavigation)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.OperacionesDocumentales)
                .Include(o => o.TransaccionesFinancieras) // Agregamos Finanzas para el saldo
                .Include(o => o.Unidadestecnicas)
                .Where(o => o.IdCliente == idClienteLogueado && o.IsDeleted == false); // AISLAMIENTO TOTAL

            // --- LÓGICA DEL BUSCADOR ---
            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(o =>
                    (o.NumeroBooking != null && o.NumeroBooking.Contains(SearchString)) ||
                    (o.NumeroSello != null && o.NumeroSello.Contains(SearchString))
                );
            }

            OperacionesCliente = await query.OrderByDescending(o => o.Id).ToListAsync();

            // --- CÁLCULO AUTOMÁTICO DE LOS KPIs ---
            CargasActivas = OperacionesCliente.Count(o => o.EstadoWorkflow != "Cerrado" && o.EstadoWorkflow != "Cancelado" && o.EstadoWorkflow != "Finalizado");
            EmbarquesFinalizados = OperacionesCliente.Count(o => o.EstadoWorkflow == "Cerrado" || o.EstadoWorkflow == "Finalizado");

            // Calculamos acciones requeridas (Ej: Borrador BL por aprobar)
            AccionesRequeridas = OperacionesCliente.Count(o =>
                o.OperacionesDocumentales.Any(d => d.EstadoDocumental == "BORRADOR ENVIADO" || d.LogSanitario != null && d.LogSanitario.Contains("ENVÍO A CLIENTE") && !d.LogSanitario.Contains("RESPUESTA")));

            // Calculamos Saldo Pendiente del Cliente (Solo facturas de INGRESO emitidas que no están pagadas)
            SaldoPendienteTotal = OperacionesCliente.SelectMany(o => o.TransaccionesFinancieras)
                .Where(t => t.TipoMovimiento == "INGRESO" && t.Moneda == "USD" && t.EstadoFila == "FACTURADO")
                .Sum(t => t.MontoNeto);

            return Page();
        }
    }
}