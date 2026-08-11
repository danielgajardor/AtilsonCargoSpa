using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class MaritimoModel : PageModel
    {
        private readonly AtilsonContext _context;

        public MaritimoModel(AtilsonContext context)
        {
            _context = context;
        }

        public IList<Operacione> Operaciones { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public async Task OnGetAsync()
        {
            int[] serviciosMaritimos = { 1, 2, 3, 5, 8, 9, 10, 12 };

            var query = _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.IdPuertoOrigenNavigation)
                .Include(o => o.IdPuertoDestinoNavigation)
                .Include(o => o.Unidadestecnicas)
                .Where(o => !o.IsDeleted && o.IdTipoServicio.HasValue && serviciosMaritimos.Contains(o.IdTipoServicio.Value))
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                string s = SearchString.ToLower();
                query = query.Where(o =>
                    (o.NumeroBooking != null && o.NumeroBooking.ToLower().Contains(s)) ||
                    (o.Nave != null && o.Nave.ToLower().Contains(s)) ||
                    (o.IdClienteNavigation != null && o.IdClienteNavigation.RazonSocial != null && o.IdClienteNavigation.RazonSocial.ToLower().Contains(s))
                );
            }

            Operaciones = await query.OrderByDescending(o => o.Id).ToListAsync();

            // Listas para los selectores del Modal Rápido
            ViewData["IdNaviera"] = new SelectList(await _context.Navieras.Where(n => n.Activo == 1).ToListAsync(), "Id", "NombreNaviera");
            ViewData["IdPuerto"] = new SelectList(await _context.Puertos.Where(p => p.Activo == 1).ToListAsync(), "Id", "NombrePuerto");
        }

        // HANDLER PARA GUARDAR LA EDICIÓN DESDE EL MODAL RÁPIDO
        public async Task<IActionResult> OnPostUpdateMaritimoAsync(int id)
        {
            var opDb = await _context.Operaciones
                .Include(o => o.Unidadestecnicas)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (opDb != null)
            {
                if (int.TryParse(Request.Form["ModNaviera"], out int idNav)) opDb.IdNaviera = idNav;
                if (int.TryParse(Request.Form["ModPol"], out int idPol)) opDb.IdPuertoOrigen = idPol;
                if (int.TryParse(Request.Form["ModPod"], out int idPod)) opDb.IdPuertoDestino = idPod;

                opDb.TerminalPortuario = Request.Form["ModTerminal"];
                opDb.Nave = Request.Form["ModNave"];
                opDb.Transbordo = Request.Form["ModTransbordo"];
                opDb.EstadoLar = Request.Form["ModEstadoLar"];

                opDb.NumeroContenedor = Request.Form["ModNumeroContenedor"];
                if (string.IsNullOrWhiteSpace(opDb.NumeroContenedor)) opDb.NumeroContenedor = null;

                opDb.SelloNaviera = Request.Form["ModSello"];
                if (string.IsNullOrWhiteSpace(opDb.SelloNaviera)) opDb.SelloNaviera = null;

                if (DateTime.TryParse(Request.Form["ModEtd"], out DateTime etd)) opDb.EtdPol = etd; else opDb.EtdPol = null;
                if (DateTime.TryParse(Request.Form["ModEta"], out DateTime eta)) opDb.EtaPod = eta; else opDb.EtaPod = null;
                if (DateTime.TryParse(Request.Form["ModStacking"], out DateTime stk)) opDb.FechaStacking = stk; else opDb.FechaStacking = null;
                if (DateTime.TryParse(Request.Form["ModCutoff"], out DateTime cut)) opDb.CutOffMatriz = cut; else opDb.CutOffMatriz = null;
                if (DateTime.TryParse(Request.Form["ModLateArrival"], out DateTime lar)) opDb.LateArrival = lar; else opDb.LateArrival = null;

                opDb.ContenedorIngresado = Request.Form.TryGetValue("ModIngresado", out var cIng) && cIng == "true";

                // MAGIA: ACTUALIZAR TODOS LOS CONTENEDORES EXTRAS DESDE EL MODAL
                if (opDb.Unidadestecnicas != null)
                {
                    foreach (var u in opDb.Unidadestecnicas)
                    {
                        if (Request.Form.TryGetValue($"ModExtraCntr_{u.Id}", out var cVal))
                            u.NroContenedor = string.IsNullOrWhiteSpace(cVal) ? null : cVal.ToString();

                        if (Request.Form.TryGetValue($"ModExtraSello_{u.Id}", out var sVal))
                            u.SelloNaviera = string.IsNullOrWhiteSpace(sVal) ? null : sVal.ToString();
                    }
                }

                opDb.FechaModificacion = DateTime.Now;
                opDb.UsuarioModificador = User.Identity?.Name ?? "Sistema";

                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Maritimo");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int? id)
        {
            if (id == null) return NotFound();
            var operacion = await _context.Operaciones.FindAsync(id);
            if (operacion != null)
            {
                operacion.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Maritimo");
        }
    }
}