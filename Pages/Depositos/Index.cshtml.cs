using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using AtilsonCargoSpa.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtilsonCargoSpa.Pages.Depositos
{
    public class IndexModel : PageModel
    {
        private readonly AtilsonContext _context;

        public IndexModel(AtilsonContext context)
        {
            _context = context;
        }

        public PaginatedList<Deposito> Depositos { get; set; } = default!;

        // Diccionarios para mostrar Nombres en la tabla rápida sin Join complejos
        public Dictionary<int, string> NavierasDict { get; set; } = new();
        public Dictionary<int, string> CiudadesDict { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            // Cargar diccionarios para la tabla
            NavierasDict = await _context.Navieras.ToDictionaryAsync(n => n.Id, n => n.NombreNaviera);
            CiudadesDict = await _context.Ciudades.ToDictionaryAsync(c => c.Id, c => c.Nombre);

            // Consulta Base
            var query = _context.Depositos.Where(d => d.Activo == 1);

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(s => s.NombreDeposito.Contains(SearchString) || s.Direccion.Contains(SearchString));
            }

            // Ordenamiento por ID (Más recientes primero)
            query = query.OrderByDescending(d => d.Id);

            int pageSize = 10;
            Depositos = await PaginatedList<Deposito>.CreateAsync(query.AsNoTracking(), pageIndex ?? 1, pageSize);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var deposito = await _context.Depositos.FindAsync(id);
            if (deposito != null)
            {
                deposito.Activo = 0;
                deposito.FechaModificacion = DateTime.Now;
                deposito.UsuarioModificador = "Admin Atilson";
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }
    }
}