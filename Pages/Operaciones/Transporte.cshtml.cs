using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using AtilsonCargoSpa.Services;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class TransporteModel : PageModel
    {
        private readonly AtilsonContext _context;

        public TransporteModel(AtilsonContext context)
        {
            _context = context;
        }

        public IList<Operacione> Operaciones { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public async Task OnGetAsync()
        {
            int[] serviciosTerrestres = { 1, 2, 4, 6, 8, 9, 11, 13 };

            var query = _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.IdPuertoOrigenNavigation)
                .Include(o => o.IdPuertoDestinoNavigation)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.Unidadestecnicas)
                .Where(o => !o.IsDeleted && o.IdTipoServicio.HasValue && serviciosTerrestres.Contains(o.IdTipoServicio.Value))
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                string s = SearchString.ToLower();
                query = query.Where(o =>
                    o.NumeroBooking.ToLower().Contains(s) ||
                    o.OperacionesTerrestres.Any(t => t.EmpresaTransporte.ToLower().Contains(s)) ||
                    o.IdClienteNavigation.RazonSocial.ToLower().Contains(s)
                );
            }

            Operaciones = await query.OrderByDescending(o => o.Id).ToListAsync();
        }

        public async Task<IActionResult> OnPostUpdateTransporteAsync(int id)
        {
            var opDb = await _context.Operaciones
                .Include(o => o.OperacionesTerrestres)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (opDb != null)
            {
                var terrDb = opDb.OperacionesTerrestres.FirstOrDefault();
                if (terrDb == null)
                {
                    terrDb = new OperacionesTerrestre
                    {
                        FechaCreacion = DateTime.Now,
                        UsuarioCreador = User.Identity?.Name ?? "Sistema",
                        Activo = true
                    };
                    opDb.OperacionesTerrestres.Add(terrDb);
                }

                terrDb.EmpresaTransporte = Request.Form["ModEmpresa"];
                terrDb.RutTransporte = Request.Form["ModRut"];
                terrDb.CorreoTransporte = Request.Form["ModCorreo"];
                terrDb.NombreConductor = Request.Form["ModConductor"];
                terrDb.TelefonoConductor = Request.Form["ModTelefono"];
                terrDb.Patente = Request.Form["ModPatente"];
                terrDb.TipoUnidadTransporte = Request.Form["ModTipoUnidad"];
                terrDb.DepositoRetiro = Request.Form["ModDeposito"];
                terrDb.PlantaCarga = Request.Form["ModPlanta"];
                terrDb.ZonaEmbarque = Request.Form["ModZona"];
                terrDb.LinkTracking = Request.Form["ModTracking"];

                if (DateTime.TryParse(Request.Form["ModFechaCarga"], out DateTime dtCarga)) terrDb.FechaCarga = dtCarga; else terrDb.FechaCarga = null;
                if (DateTime.TryParse(Request.Form["ModLlegadaPlanta"], out DateTime dtInPlanta)) terrDb.LlegadaPlanta = dtInPlanta; else terrDb.LlegadaPlanta = null;
                if (DateTime.TryParse(Request.Form["ModSalidaPlanta"], out DateTime dtOutPlanta)) terrDb.SalidaPlanta = dtOutPlanta; else terrDb.SalidaPlanta = null;
                if (DateTime.TryParse(Request.Form["ModLlegadaPuerto"], out DateTime dtInPuerto)) terrDb.LlegadaPuerto = dtInPuerto; else terrDb.LlegadaPuerto = null;
                if (DateTime.TryParse(Request.Form["ModSalidaPuerto"], out DateTime dtOutPuerto)) terrDb.SalidaPuerto = dtOutPuerto; else terrDb.SalidaPuerto = null;

                terrDb.SorteoEscaner = Request.Form.TryGetValue("ModEscaner", out var eScan) && eScan == "true";

                opDb.FechaModificacion = DateTime.Now;
                opDb.UsuarioModificador = User.Identity?.Name ?? "Sistema";

                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Datos de logística guardados exitosamente.";
            }
            return RedirectToPage("./Transporte");
        }

        // ====================================================================
        // GESTOR UNIFICADO DE CORREOS (SOLICITUD, PRELIMINAR, COMPLETA)
        // ====================================================================
        public async Task<IActionResult> OnPostSendCorreosTransporteAsync(int idOperacion, string tipoCorreo, string destinatario, string cc, string asunto, string cuerpo)
        {
            try
            {
                var emailService = new EmailService();
                string htmlCuerpo = $"<div style='font-family: Arial, sans-serif; font-size: 14px; color: #1e293b;'><p>{cuerpo.Replace("\n", "<br>")}</p></div>";

                // Descomentar para enviar el correo real cuando el SMTP esté configurado:
                // await emailService.EnviarCorreoAsync(destinatario, asunto, htmlCuerpo, cc);

                var opDb = await _context.Operaciones
                    .Include(o => o.OperacionesTerrestres)
                    .FirstOrDefaultAsync(o => o.Id == idOperacion);

                if (opDb != null)
                {
                    var terrDb = opDb.OperacionesTerrestres.FirstOrDefault();
                    if (terrDb == null)
                    {
                        terrDb = new OperacionesTerrestre { FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema", Activo = true };
                        opDb.OperacionesTerrestres.Add(terrDb);
                    }

                    // Lógica de Estados y Alertas
                    if (tipoCorreo == "solicitud" || tipoCorreo == "preliminar")
                    {
                        terrDb.SolicitudEnviada = true; // Activa la alerta roja visual "Falta Asignación Completa"
                    }
                    else if (tipoCorreo == "completa")
                    {
                        terrDb.SolicitudEnviada = true;
                        terrDb.AsignacionEnviada = true; // Apaga la alerta roja, el proceso fue completado
                    }

                    opDb.FechaModificacion = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                string tipoFormat = tipoCorreo == "solicitud" ? "Solicitud al Proveedor" : (tipoCorreo == "preliminar" ? "Asignación Preliminar" : "Asignación Completa");
                TempData["SuccessMsg"] = $"Correo de <strong>{tipoFormat}</strong> procesado y registrado con éxito.";
            }
            catch (Exception ex)
            {
                TempData["SuccessMsg"] = $"Hubo un problema de conexión al enviar el correo. Revise credenciales SMTP. ({ex.Message})";
            }
            return RedirectToPage("./Transporte");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int? id)
        {
            if (id == null) return NotFound();
            var operacion = await _context.Operaciones.FindAsync(id);
            if (operacion != null) { operacion.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToPage("./Transporte");
        }
    }
}