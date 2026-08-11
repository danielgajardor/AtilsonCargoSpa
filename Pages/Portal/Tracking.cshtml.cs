using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace AtilsonCargoSpa.Pages.Portal
{
    [Authorize(Roles = "Cliente")]
    public class TrackingModel : PageModel
    {
        private readonly AtilsonContext _context;

        public TrackingModel(AtilsonContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string BookingNumber { get; set; }

        public Operacione OperacionTracking { get; set; }
        public string EmpresaCliente { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            string idClienteStr = User.FindFirst("IdCliente")?.Value ?? "0";
            int idClienteLogueado = int.Parse(idClienteStr);

            // DETECTAMOS SI ES LA CUENTA DE PRUEBAS "MODO LIBRE"
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

            // BUSCAMOS EL BOOKING (Con o Sin Filtro de Seguridad)
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