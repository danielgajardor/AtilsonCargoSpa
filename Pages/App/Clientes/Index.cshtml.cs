using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using AtilsonCargoSpa.Helpers; // Agregamos el Helper
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace AtilsonCargoSpa.Pages.App.Clientes
{
    public class IndexModel : PageModel
    {
        private readonly AtilsonContext _context;

        public IndexModel(AtilsonContext context)
        {
            _context = context;
        }

        // CAMBIO 1: Ahora usamos PaginatedList en lugar de IList
        public PaginatedList<Cliente> Clientes { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CiudadId { get; set; }

        public SelectList Ciudades { get; set; } = default!;

        // CAMBIO 2: Recibimos el número de página (por defecto 1)
        public async Task OnGetAsync(int? pageIndex)
        {
            Ciudades = new SelectList(await _context.Ciudades.ToListAsync(), "Id", "Nombre");

            var query = _context.Clientes.Where(c => c.Activo == 1);

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(s => s.RazonSocial.Contains(SearchString)
                                      || s.Rut.Contains(SearchString)
                                      || s.NombreCliente.Contains(SearchString));
            }

            if (CiudadId.HasValue)
            {
                query = query.Where(x => x.IdCiudad == CiudadId);
            }

            // Ordenamos por fecha de creación (los más nuevos primero)
            query = query.OrderByDescending(c => c.Id);

            // CAMBIO 3: Tamaño de página (ej. 10 registros por página)
            int pageSize = 10;
            Clientes = await PaginatedList<Cliente>.CreateAsync(query.AsNoTracking(), pageIndex ?? 1, pageSize);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente != null)
            {
                cliente.Activo = 0;
                cliente.FechaModificacion = DateTime.Now;
                cliente.UsuarioModificador = "Admin Atilson";
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }

        // ... Mismos métodos de exportación Excel y PDF sin cambios ...
        public async Task<IActionResult> OnPostExportExcelAsync()
        {
            var query = _context.Clientes.Where(c => c.Activo == 1);

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(s => s.RazonSocial.Contains(SearchString)
                                      || s.Rut.Contains(SearchString)
                                      || s.NombreCliente.Contains(SearchString));
            }
            if (CiudadId.HasValue)
            {
                query = query.Where(x => x.IdCiudad == CiudadId);
            }

            var lista = await query.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Clientes Atilson");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "RUT";
                worksheet.Cell(currentRow, 2).Value = "Razón Social";
                worksheet.Cell(currentRow, 3).Value = "Nombre Fantasía";
                worksheet.Cell(currentRow, 4).Value = "Contacto";

                var headerRange = worksheet.Range("A1:D1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2c1b4d");
                headerRange.Style.Font.FontColor = XLColor.White;

                foreach (var c in lista)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = c.Rut;
                    worksheet.Cell(currentRow, 2).Value = c.RazonSocial;
                    worksheet.Cell(currentRow, 3).Value = c.NombreCliente;
                    worksheet.Cell(currentRow, 4).Value = c.Contacto;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte_Clientes_Atilson.xlsx");
                }
            }
        }

        public async Task<IActionResult> OnPostExportPdfAsync()
        {
            var query = _context.Clientes.Where(c => c.Activo == 1);

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(s => s.RazonSocial.Contains(SearchString)
                                      || s.Rut.Contains(SearchString)
                                      || s.NombreCliente.Contains(SearchString));
            }
            if (CiudadId.HasValue)
            {
                query = query.Where(x => x.IdCiudad == CiudadId);
            }

            var lista = await query.ToListAsync();

            using (var stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.LETTER.Rotate(), 25, 25, 30, 30);
                PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                var titleFont = FontFactory.GetFont("Arial", 16, Font.BOLD, new BaseColor(44, 27, 77));
                pdfDoc.Add(new Paragraph("ATILSON CARGO - REPORTE MAESTRO DE CLIENTES\n\n", titleFont));

                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 15f, 40f, 25f, 20f });

                string[] headers = { "RUT", "Razón Social", "Nombre Fantasía", "Contacto" };
                foreach (var h in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(h, FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.WHITE)));
                    cell.BackgroundColor = new BaseColor(44, 27, 77);
                    cell.Padding = 8;
                    table.AddCell(cell);
                }

                var normalFont = FontFactory.GetFont("Arial", 9);
                foreach (var c in lista)
                {
                    table.AddCell(new PdfPCell(new Phrase(c.Rut ?? "", normalFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(c.RazonSocial ?? "", normalFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(c.NombreCliente ?? "", normalFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(c.Contacto ?? "", normalFont)) { Padding = 5 });
                }

                pdfDoc.Add(table);
                pdfDoc.Close();
                return File(stream.ToArray(), "application/pdf", "Reporte_Clientes_Atilson.pdf");
            }
        }
    }
}