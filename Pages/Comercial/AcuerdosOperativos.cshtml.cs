using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtilsonCargoSpa.Pages.Comercial
{
    public class AcuerdosOperativosModel : PageModel
    {
        private readonly AtilsonContext _context;

        public AcuerdosOperativosModel(AtilsonContext context)
        {
            _context = context;
        }

        public class TxDTO
        {
            public int Id { get; set; }
            public string GrupoCobro { get; set; } = string.Empty;
            public string TipoMovimiento { get; set; } = string.Empty;
            public string Concepto { get; set; } = string.Empty;
            public decimal MontoNeto { get; set; }
            public string Moneda { get; set; } = string.Empty;
            public string? Justificacion { get; set; }

            // AGREGA ESTA LÍNEA
            public bool TarifaManual { get; set; }
        }

        public class BookingPricingDTO
        {
            public int IdOperacion { get; set; }
            public string NumeroBooking { get; set; } = string.Empty;
            public string Cliente { get; set; } = string.Empty;
            public string ServicioContratado { get; set; } = string.Empty;
            public string Pol { get; set; } = string.Empty;
            public string Pod { get; set; } = string.Empty;
            public string TipoContenedor { get; set; } = string.Empty;
            public string Naviera { get; set; } = string.Empty;
            public List<TxDTO> Transacciones { get; set; } = new();

            // PROPIEDADES PARA INTERPLANTA
            public bool TieneInterplanta { get; set; }
            public string RutaInterplanta { get; set; } = string.Empty;
        }

        public List<BookingPricingDTO> Bookings { get; set; } = new();

        public async Task OnGetAsync()
        {
            var operaciones = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.IdPuertoOrigenNavigation)
                .Include(o => o.IdPuertoDestinoNavigation)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.OperacionesDocumentales)
                .Include(o => o.OperacionesAlmacenamientos) // <--- INYECTADO ALMACENAMIENTO
                .Include(o => o.TransaccionesFinancieras)
                .Where(o => !o.IsDeleted)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            foreach (var op in operaciones)
            {
                var dto = new BookingPricingDTO
                {
                    IdOperacion = op.Id,
                    NumeroBooking = string.IsNullOrWhiteSpace(op.NumeroBooking) ? $"OP-{op.Id}" : op.NumeroBooking,
                    Cliente = op.IdClienteNavigation?.RazonSocial ?? "SIN MANDANTE",
                    Pol = op.IdPuertoOrigenNavigation?.NombrePuerto ?? "N/A",
                    Pod = op.IdPuertoDestinoNavigation?.NombrePuerto ?? "N/A",
                    TipoContenedor = op.TipoContenedor ?? "N/A",
                    Naviera = op.IdNavieraNavigation?.NombreNaviera ?? "S/N"
                };

                List<string> serv = new List<string>();
                if (op.IdNaviera > 0) serv.Add("MAR");
                if (op.OperacionesTerrestres != null && op.OperacionesTerrestres.Any()) serv.Add("TER");
                if (op.OperacionesDocumentales != null && op.OperacionesDocumentales.Any()) serv.Add("DOC");

                // Agregamos la etiqueta ALM al Booking
                if (op.OperacionesAlmacenamientos != null && op.OperacionesAlmacenamientos.Any(a => a.IdProveedor != null))
                    serv.Add("ALM");

                dto.ServicioContratado = serv.Any() ? string.Join(" + ", serv) : "SRV BASE";

                // LÓGICA INTERPLANTA
                var terrDb = op.OperacionesTerrestres?.FirstOrDefault();
                if (terrDb != null && terrDb.AplicaInterplanta == true)
                {
                    dto.TieneInterplanta = true;
                    var plantas = new List<string>();

                    if (!string.IsNullOrWhiteSpace(terrDb.PlantaCarga)) plantas.Add(terrDb.PlantaCarga);
                    if (!string.IsNullOrWhiteSpace(terrDb.PlantaCarga2)) plantas.Add(terrDb.PlantaCarga2);
                    if (!string.IsNullOrWhiteSpace(terrDb.PlantaCarga3)) plantas.Add(terrDb.PlantaCarga3);

                    dto.RutaInterplanta = plantas.Any() ? string.Join(" - ", plantas) : "Ruta Múltiple";
                }

                dto.Transacciones = op.TransaccionesFinancieras.Select(t =>
                {
                    string grupoOriginal = string.IsNullOrWhiteSpace(t.GrupoCobro) ? "Otros" : t.GrupoCobro;

                    // Transformación estructurada e inmutable
                    if (grupoOriginal.Contains("Depósito") || grupoOriginal.Contains("Deposito"))
                        grupoOriginal = "Gate-Out/In";

                    return new TxDTO
                    {
                        Id = t.Id,
                        GrupoCobro = grupoOriginal,
                        TipoMovimiento = t.TipoMovimiento,
                        Concepto = t.Concepto,
                        MontoNeto = t.MontoNeto,
                        Moneda = t.Moneda,
                        Justificacion = t.JustificacionManual,
                        TarifaManual = t.TarifaManual
                    };
                }).ToList();

                Bookings.Add(dto);
            }
        }

        public async Task<IActionResult> OnPostGuardarAcuerdosAsync(int idOperacion, [FromBody] List<TxUpdatePayload> updates)
        {
            if (updates == null || !updates.Any()) return new JsonResult(new { success = false, message = "No hay datos para actualizar." });

            var transacciones = await _context.TransaccionesFinancieras.Where(t => t.IdOperacion == idOperacion).ToListAsync();

            foreach (var update in updates)
            {
                var tx = transacciones.FirstOrDefault(t => t.Id == update.IdTx);
                if (tx != null)
                {
                    tx.MontoNeto = update.NuevoMonto;
                    tx.TarifaManual = true;
                    tx.JustificacionManual = update.Justificacion;
                    tx.FechaModificacion = System.DateTime.Now;
                    tx.UsuarioModificador = User.Identity?.Name ?? "Comercial";
                }
            }

            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public class TxUpdatePayload
        {
            public int IdTx { get; set; }
            public decimal NuevoMonto { get; set; }
            public string Justificacion { get; set; } = string.Empty;
        }

        public class NuevoTramoPayload
        {
            public int IdOperacion { get; set; }
            public string Concepto { get; set; } = string.Empty;
            public decimal MontoNeto { get; set; }
            public string Moneda { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAñadirTramoInterplantaAsync([FromBody] NuevoTramoPayload data)
        {
            if (data == null || data.MontoNeto <= 0) return new JsonResult(new { success = false, message = "Datos inválidos" });

            bool existeCosto = await _context.TransaccionesFinancieras.AnyAsync(t => t.IdOperacion == data.IdOperacion && t.Concepto == data.Concepto && t.TipoMovimiento == "EGRESO");
            if (existeCosto && data.Concepto.Contains("TARIFA PLANA"))
                return new JsonResult(new { success = false, message = "Esta tarifa plana ya fue añadida anteriormente." });

            int? idCliente = await _context.Operaciones.Where(o => o.Id == data.IdOperacion).Select(o => o.IdCliente).FirstOrDefaultAsync();

            var nuevaTxEgreso = new TransaccionesFinanciera
            {
                IdOperacion = data.IdOperacion,
                GrupoCobro = "Transporte",
                TipoMovimiento = "EGRESO",
                Concepto = data.Concepto,
                MontoNeto = data.MontoNeto,
                Moneda = data.Moneda,
                EstadoFila = "PROVISIÓN",
                TarifaManual = true,
                FechaCreacion = System.DateTime.Now,
                UsuarioCreador = User.Identity?.Name ?? "Comercial"
            };

            var nuevaTxIngreso = new TransaccionesFinanciera
            {
                IdOperacion = data.IdOperacion,
                GrupoCobro = "Transporte",
                TipoMovimiento = "INGRESO",
                Concepto = data.Concepto,
                MontoNeto = data.MontoNeto,
                Moneda = data.Moneda,
                EstadoFila = "PROVISIÓN",
                TarifaManual = true,
                FechaCreacion = System.DateTime.Now,
                UsuarioCreador = User.Identity?.Name ?? "Comercial",
                IdCliente = idCliente
            };

            _context.TransaccionesFinancieras.Add(nuevaTxEgreso);
            _context.TransaccionesFinancieras.Add(nuevaTxIngreso);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
    }
}