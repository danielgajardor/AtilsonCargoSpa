using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class AlmacenamientoModel : PageModel
    {
        private readonly AtilsonContext _context;

        public AlmacenamientoModel(AtilsonContext context)
        {
            _context = context;
        }

        public IList<Operacione> Operaciones { get; set; } = default!;

        public void OnGet()
        {
            // Módulo en construcción
            Operaciones = new List<Operacione>();
        }
    }
}