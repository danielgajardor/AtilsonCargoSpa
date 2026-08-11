using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class DocumentalModel : PageModel
    {
        private readonly AtilsonContext _context;

        public DocumentalModel(AtilsonContext context)
        {
            _context = context;
        }

        public IList<Operacione> Operaciones { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public async Task OnGetAsync()
        {
            int[] serviciosDoc = { 1, 3, 4, 7, 8, 10, 11, 14 };

            var query = _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.OperacionesDocumentales)
                .Where(o => !o.IsDeleted && o.IdTipoServicio.HasValue && serviciosDoc.Contains(o.IdTipoServicio.Value))
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                string s = SearchString.ToLower();
                query = query.Where(o =>
                    (o.NumeroBooking != null && o.NumeroBooking.ToLower().Contains(s)) ||
                    (o.IdClienteNavigation != null && o.IdClienteNavigation.RazonSocial != null && o.IdClienteNavigation.RazonSocial.ToLower().Contains(s)) ||
                    o.OperacionesDocumentales.Any(d => d.DusDin != null && d.DusDin.ToLower().Contains(s)) ||
                    o.OperacionesDocumentales.Any(d => d.AgenciaAduana != null && d.AgenciaAduana.ToLower().Contains(s))
                );
            }

            Operaciones = await query.OrderByDescending(o => o.Id).ToListAsync();
        }

        // HANDLER PARA GUARDAR LA EDICIÓN DESDE EL MODAL RÁPIDO DOCUMENTAL
        public async Task<IActionResult> OnPostUpdateDocumentalAsync(int id)
        {
            var opDb = await _context.Operaciones
                .Include(o => o.OperacionesDocumentales)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (opDb != null)
            {
                var docDb = opDb.OperacionesDocumentales.FirstOrDefault();
                if (docDb == null)
                {
                    docDb = new OperacionesDocumentale
                    {
                        FechaCreacion = DateTime.Now,
                        UsuarioCreador = User.Identity?.Name ?? "Sistema",
                        Activo = true
                    };
                    opDb.OperacionesDocumentales.Add(docDb);
                }

                // Actualizar campos documentales desde el Modal
                docDb.AgenciaAduana = Request.Form["ModAgencia"];
                docDb.DusDin = Request.Form["ModDus"];
                docDb.EstadoDocumental = Request.Form["ModEstadoDoc"];

                docDb.MatrizPresentada = Request.Form.TryGetValue("ModMatriz", out var mVal) && mVal == "true";
                docDb.GuiaVisado = Request.Form.TryGetValue("ModVisado", out var vVal) && vVal == "true";
                docDb.ExtensionDocumental = Request.Form.TryGetValue("ModExtension", out var eVal) && eVal == "true";

                opDb.FechaModificacion = DateTime.Now;
                opDb.UsuarioModificador = User.Identity?.Name ?? "Sistema";

                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Documental");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int? id)
        {
            if (id == null) return NotFound();
            var operacion = await _context.Operaciones.FindAsync(id);
            if (operacion != null) { operacion.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToPage("./Documental");
        }
    }
}