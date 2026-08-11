using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class ArchivosModel : PageModel
    {
        private readonly AtilsonContext _context;
        private readonly IWebHostEnvironment _env;

        public ArchivosModel(AtilsonContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public Operacione OperacionActual { get; set; } = default!;
        public List<ArchivoOperacion> ListaArchivos { get; set; } = new();

        [BindProperty]
        public IFormFile? ArchivoSubido { get; set; }

        [BindProperty]
        public string TipoDocumento { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            OperacionActual = await _context.Operaciones.FirstOrDefaultAsync(o => o.Id == id);
            if (OperacionActual == null) return NotFound();

            // Usamos Set<ArchivoOperacion>() para evitar el error de nombre del Contexto
            ListaArchivos = await _context.Set<ArchivoOperacion>()
                .Where(a => a.IdOperacion == id)
                .OrderByDescending(a => a.FechaSubida)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (ArchivoSubido == null || ArchivoSubido.Length == 0) return RedirectToPage(new { id = id });

            // 1. Definir carpeta única 'documentos'
            string carpetaDestino = Path.Combine(_env.WebRootPath, "uploads", "documentos");
            if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);

            // 2. Crear nombre único para el servidor (ID_Ticks_NombreOriginal.pdf)
            string nombreOriginal = Path.GetFileName(ArchivoSubido.FileName).Replace(" ", "_");
            string nombreUnico = $"{id}_{DateTime.Now.Ticks}_{nombreOriginal}";
            string rutaFisica = Path.Combine(carpetaDestino, nombreUnico);

            // 3. Guardar físicamente
            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await ArchivoSubido.CopyToAsync(stream);
            }

            // 4. Guardar en Base de Datos
            var nuevoDoc = new ArchivoOperacion
            {
                IdOperacion = id,
                NombreArchivo = nombreOriginal,
                RutaArchivo = $"/uploads/documentos/{nombreUnico}",
                TipoDocumento = TipoDocumento,
                FechaSubida = DateTime.Now,
                UsuarioSubida = User.Identity?.Name ?? "Sistema"
            };

            // Usamos Set<ArchivoOperacion>() para agregar el documento
            _context.Set<ArchivoOperacion>().Add(nuevoDoc);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostEliminarAsync(int id, int idArchivo)
        {
            // Usamos Set<ArchivoOperacion>() para buscar el documento
            var doc = await _context.Set<ArchivoOperacion>().FindAsync(idArchivo);
            if (doc != null)
            {
                // Borrar archivo físico
                string rutaFisica = Path.Combine(_env.WebRootPath, doc.RutaArchivo.TrimStart('/'));
                if (System.IO.File.Exists(rutaFisica)) System.IO.File.Delete(rutaFisica);

                // Borrar de BD
                _context.Set<ArchivoOperacion>().Remove(doc);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { id = id });
        }
    }
}