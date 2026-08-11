using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AtilsonCargoSpa.Models;
using System;
using System.Threading.Tasks;

namespace AtilsonCargoSpa.Pages.PortalCliente
{
    public class SolicitarReservaModel : PageModel
    {
        private readonly AtilsonContext _context;

        public SolicitarReservaModel(AtilsonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Cotizacion NuevaCotizacion { get; set; } = new Cotizacion();

        public void OnGet()
        {
            // Aquí puedes precargar datos si lo necesitas a futuro
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Configurar valores por defecto del sistema
                NuevaCotizacion.FechaSolicitud = DateTime.Now;
                NuevaCotizacion.Estado = "NUEVA SOLICITUD";
                NuevaCotizacion.Activo = true;

                // TODO: Aquí debes asignar el ID del cliente que está logueado en el portal.
                // Si usas claims de autenticación, sería algo como:
                // int idCliente = int.Parse(User.FindFirst("IdCliente")?.Value ?? "0");
                // NuevaCotizacion.IdCliente = idCliente > 0 ? idCliente : null;

                // Para pruebas, lo dejamos NULL o le pones un ID fijo válido de tu BD
                NuevaCotizacion.IdCliente = null;

                // Guardar en la Base de Datos
                _context.Cotizaciones.Add(NuevaCotizacion);
                await _context.SaveChangesAsync();

                // Alerta de éxito para el HTML
                TempData["SolicitudExitosa"] = "true";

                // Refrescar para limpiar el formulario
                return RedirectToPage("./SolicitarReserva");
            }
            catch (Exception ex)
            {
                // En caso de error, muestra mensaje
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar: " + ex.Message);
                return Page();
            }
        }
    }
}