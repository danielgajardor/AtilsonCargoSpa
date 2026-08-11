using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class SolicitudProveedorModel : PageModel
    {
        private readonly AtilsonContext _context;

        public SolicitudProveedorModel(AtilsonContext context)
        {
            _context = context;
        }

        public Operacione Operacion { get; set; } = default!;

        public string CorreoTransporte { get; set; } = "";
        public string AsuntoTransporte { get; set; } = "";
        public string TextoTransporte { get; set; } = "";

        public string CorreoAduana { get; set; } = "";
        public string AsuntoAduana { get; set; } = "";
        public string TextoDoc { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Operacion = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.IdPuertoOrigenNavigation)
                .Include(o => o.IdPuertoDestinoNavigation)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.OperacionesDocumentales)
                .Include(o => o.Unidadestecnicas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Operacion == null) return NotFound();

            var terr = Operacion.OperacionesTerrestres?.FirstOrDefault();
            var doc = Operacion.OperacionesDocumentales?.FirstOrDefault();

            // Calculo de Cantidad de Contenedores y Tipo
            int qty = 1 + (Operacion.Unidadestecnicas?.Count ?? 0);
            string tipoCargaStr = Operacion.IdTipoCarga == 2 ? "REEFER" : "DRY";
            string unidad = Operacion.TipoContenedor ?? "40'HC";
            string containerStr = $"{qty}X{unidad} {tipoCargaStr}".ToUpper();

            // Configuración cultural para fechas en español
            var culture = new CultureInfo("es-ES");
            string fechaCargaStr = "TBA";
            if (terr?.FechaCarga != null)
            {
                fechaCargaStr = terr.FechaCarga.Value.ToString("dddd dd/MM – HH:mm 'Hrs.'", culture).ToUpper();
            }

            string nombreCliente = (!string.IsNullOrWhiteSpace(Operacion.IdClienteNavigation?.NombreCliente)
                ? Operacion.IdClienteNavigation.NombreCliente
                : Operacion.IdClienteNavigation?.RazonSocial ?? "CLIENTE").ToUpper();

            string empresaTrans = (terr?.EmpresaTransporte ?? "PROVEEDOR").ToUpper();
            string naveStr = (string.IsNullOrWhiteSpace(Operacion.Nave) ? "TBA" : Operacion.Nave).ToUpper();
            string naviera = (Operacion.IdNavieraNavigation?.NombreNaviera ?? "TBA").ToUpper();
            string ptoOrigen = (Operacion.IdPuertoOrigenNavigation?.NombrePuerto ?? "TBA").ToUpper();

            // ==========================================
            // PREPARACIÓN: CORREO TRANSPORTE
            // ==========================================
            CorreoTransporte = terr?.CorreoTransporte ?? "";

            // ASUNTO EXACTO AL EJEMPLO
            AsuntoTransporte = $"SOLICITUD SERVICIO TRANSPORTE {empresaTrans}***** MN {naveStr} // BOOKING: {Operacion.NumeroBooking} ({containerStr}) // {nombreCliente} // {naviera} // ATILSON CARGO SPA";

            // CUERPO EXACTO AL EJEMPLO
            TextoTransporte = "Estimado/a equipo, buenos días:\n\n" +
                              "Junto con saludar.\n\n" +
                              "Según lo conversado, favor considerar el siguiente servicio, notar detalle:\n\n" +
                              $"BOOKING: {Operacion.NumeroBooking}\n" +
                              $"T/UNIDAD: {containerStr}\n" +
                              $"T/CARGA: {(Operacion.Commodity ?? "NO ESPECIFICADA").ToUpper()}\n" +
                              $"RETIRO VACIO: {(terr?.DepositoRetiro ?? "TBA").ToUpper()}\n" +
                              $"ENTREGA FULL: {ptoOrigen}\n" +
                              "PAGO: 15 DÍAS UNA VEZ EMITIDA LA FACTURA POR EL SERVICIO FINALIZADO.\n" +
                              "TARIFA: TARIFA ACORDADA\n\n";

            // Fechas de presentación (Bucle por cantidad de contenedores)
            // Fechas de presentación (Bucle por cantidad de contenedores)
            TextoTransporte += $"PRESENTACION EN PLANTA: {fechaCargaStr} ({Operacion.NumeroBooking})\n";
            if (Operacion.Unidadestecnicas != null && Operacion.Unidadestecnicas.Any())
            {
                foreach (var u in Operacion.Unidadestecnicas)
                {
                    TextoTransporte += $"PRESENTACION EN PLANTA: {fechaCargaStr} ({Operacion.NumeroBooking})\n";
                }
            }

            string interplantaTxt = "";
            if (terr != null && terr.AplicaInterplanta == true)
            {
                if (!string.IsNullOrWhiteSpace(terr.PlantaCarga2))
                {
                    interplantaTxt += $"\nSEGUNDA PLANTA DE CARGA: {terr.PlantaCarga2.ToUpper()}\n" +
                                      $"ENLACE MAPS: {(string.IsNullOrWhiteSpace(terr.LinkMaps2) ? "Pendiente" : terr.LinkMaps2)}\n";
                }
                if (!string.IsNullOrWhiteSpace(terr.PlantaCarga3))
                {
                    interplantaTxt += $"\nTERCERA PLANTA DE CARGA: {terr.PlantaCarga3.ToUpper()}\n" +
                                      $"ENLACE MAPS: {(string.IsNullOrWhiteSpace(terr.LinkMaps3) ? "Pendiente" : terr.LinkMaps3)}\n";
                }
            }

            TextoTransporte += $"\n{(terr?.PlantaCarga ?? "PLANTA TBA").ToUpper()}\n" +
                               $"DIRECCION PLANTA: {(terr?.ZonaCarga ?? terr?.ZonaEmbarque ?? "TBA").ToUpper()}\n" +
                               $"ENLACE MAPS: {(string.IsNullOrWhiteSpace(terr?.LinkTracking) ? "Pendiente" : terr.LinkTracking)}\n" +
                               $"{interplantaTxt}\n" +
                               "Quedamos atentos a recepción de servicio.\n" +
                               "Aguardo asignaciones una vez disponibles.\n" +
                               "Desde ya, muchas gracias!\n\n" +
                               "--\n" +
                               "Saludos Cordiales / Best Regards\n\n" +
                               "Operaciones Atilson\n" +
                               "ATILSON CARGO SPA\n" +
                               "___________________________________________________________\n" +
                               "Edificio Mar del Sur, Calle Blanco Nº 1623 Of. 1104 Valparaíso, Chile.\n" +
                               "(+56) 9 89012691\n" +
                               "operaciones@atilson.com\n" +
                               "www.atilson.com";
            // ==========================================
            // PREPARACIÓN: CORREO ADUANA
            // ==========================================
            CorreoAduana = "";
            AsuntoAduana = $"SOLICITUD TRAMITACIÓN DUS // BOOKING: {Operacion.NumeroBooking} // {nombreCliente} // ATILSON CARGO SPA";

            TextoDoc = "Estimado equipo de Aduanas, buenos días:\n\n" +
                       "Junto con saludar, favor considerar la siguiente tramitación documental para inicio de gestión:\n\n" +
                       $"BOOKING: {Operacion.NumeroBooking}\n" +
                       $"EXPORTADOR: {nombreCliente}\n" +
                       $"POL (ORIGEN): {ptoOrigen}\n" +
                       $"POD (DESTINO): {(Operacion.IdPuertoDestinoNavigation?.NombrePuerto ?? "TBA").ToUpper()}\n" +
                       $"NAVE: {naveStr} // NAVIERA: {naviera}\n" +
                       $"ETD ESTIMADO: {(Operacion.EtdPol?.ToString("dd/MM/yyyy") ?? "TBA")}\n\n" +
                       "REQUERIMIENTOS:\n" +
                       "- Favor generar DUS en base a matriz enviada por el cliente.\n" +
                       "- Compartir matriz de aprobación a la brevedad posible.\n\n" +
                       "Quedamos atentos a sus comentarios.\n" +
                       "Desde ya, muchas gracias!\n\n" +
                       "--\n" +
                       "Saludos Cordiales / Best Regards\n\n" +
                       "Operaciones Atilson\n" +
                       "ATILSON CARGO SPA\n" +
                       "___________________________________________________________\n" +
                       "Edificio Mar del Sur, Calle Blanco Nº 1623 Of. 1104 Valparaíso, Chile.\n" +
                       "www.atilson.com";

            return Page();
        }
    }
}