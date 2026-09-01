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

        // 1. CAPTURAMOS LOS PARÁMETROS DEL BUSCADOR DE ITINERARIOS
        [BindProperty(SupportsGet = true)]
        public string OrigenQ { get; set; }

        [BindProperty(SupportsGet = true)]
        public string DestinoQ { get; set; }

        public void OnGet()
        {
            // 2. AUTOCOMPLETAMOS EL FORMULARIO PARA AHORRARLE TIEMPO AL CLIENTE
            if (!string.IsNullOrEmpty(OrigenQ))
            {
                NuevaCotizacion.Origen = OrigenQ;
            }

            if (!string.IsNullOrEmpty(DestinoQ))
            {
                NuevaCotizacion.Destino = DestinoQ;
            }
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
                NuevaCotizacion.IdCliente = null; // Para pruebas

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