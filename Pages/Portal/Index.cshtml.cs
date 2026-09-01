using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace AtilsonCargoSpa.Pages.Portal
{
    [Authorize(Roles = "Cliente")]
    public class IndexModel : PageModel
    {
        private readonly AtilsonContext _context;

        public IndexModel(AtilsonContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string BookingNumber { get; set; }

        public Operacione OperacionTracking { get; set; }
        public List<Operacione> MisOperaciones { get; set; } = new List<Operacione>();
        public SelectList NavierasList { get; set; }

        public int EmbarquesActivos { get; set; }
        public int EnTransito { get; set; }
        public int Entregados { get; set; }
        public string EmpresaCliente { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            string idClienteStr = User.FindFirst("IdCliente")?.Value ?? "0";
            int idClienteLogueado = int.Parse(idClienteStr);

            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            bool esModoLibre = userEmail.Equals("cliente@empresa.cl", StringComparison.OrdinalIgnoreCase) ||
                               userEmail.Equals("cliente@empresa.com", StringComparison.OrdinalIgnoreCase);

            if (esModoLibre)
            {
                EmpresaCliente = "Atilson Demo (Modo Libre)";
            }
            else
            {
                var clienteInfo = await _context.Clientes.FindAsync(idClienteLogueado);
                EmpresaCliente = clienteInfo?.RazonSocial ?? "Empresa no asignada";
            }

            // CARGAR NAVIERAS PARA EL MÓDULO DE COTIZACIÓN
            var navieras = await _context.Navieras
                .Where(n => !n.IsDeleted)
                .OrderBy(n => n.NombreNaviera)
                .ToListAsync();
            NavierasList = new SelectList(navieras, "Id", "NombreNaviera");

            var queryOperaciones = _context.Operaciones
                .Include(o => o.IdPuertoOrigenNavigation)
                .Include(o => o.IdPuertoDestinoNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.IdClienteNavigation)
                .Where(o => !o.IsDeleted);

            if (!esModoLibre)
            {
                queryOperaciones = queryOperaciones.Where(o => o.IdCliente == idClienteLogueado);
            }

            MisOperaciones = await queryOperaciones
                .OrderByDescending(o => o.Id)
                .Take(50)
                .ToListAsync();

            EmbarquesActivos = MisOperaciones.Count(o => o.EstadoWorkflow != "FINALIZADO" && o.EstadoWorkflow != "CONTENEDOR RETIRADO" && o.EstadoWorkflow != "CANCELADO");
            EnTransito = MisOperaciones.Count(o => o.EstadoWorkflow == "EMBARCADO" || o.EstadoWorkflow == "ZARPE" || o.EstadoWorkflow == "PROXIMO ARRIBO");
            Entregados = MisOperaciones.Count(o => o.EstadoWorkflow == "ARRIBADO" || o.EstadoWorkflow == "FINALIZADO" || o.EstadoWorkflow == "CONTENEDOR RETIRADO");

            if (!string.IsNullOrWhiteSpace(BookingNumber))
            {
                var queryTracking = _context.Operaciones
                    .Include(o => o.IdPuertoOrigenNavigation)
                    .Include(o => o.IdPuertoDestinoNavigation)
                    .Include(o => o.IdNavieraNavigation)
                    .Include(o => o.Unidadestecnicas)
                    .Include(o => o.IdClienteNavigation)
                    .Where(o => o.NumeroBooking.Trim().ToUpper() == BookingNumber.Trim().ToUpper() && !o.IsDeleted);

                if (!esModoLibre)
                {
                    queryTracking = queryTracking.Where(o => o.IdCliente == idClienteLogueado);
                }

                OperacionTracking = await queryTracking.FirstOrDefaultAsync();
            }

            return Page();
        }
    }
}