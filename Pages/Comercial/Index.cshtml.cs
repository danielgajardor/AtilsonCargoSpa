using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AtilsonCargoSpa.Pages.Comercial
{
    public class IndexModel : PageModel
    {
        private readonly AtilsonContext _context;
        private readonly IWebHostEnvironment _env;

        public IndexModel(AtilsonContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public SelectList ListaAgencias { get; set; } = default!;
        public IList<TarifasDocumentale> CostosAgencias { get; set; } = default!;

        [BindProperty]
        public TarifasDocumentale NuevoCostoDocumental { get; set; } = new TarifasDocumentale();
        // ==================== LISTAS PARA TABLAS ====================
        public IList<TarifasCliente> TarifasClientes { get; set; } = default!;
        public IList<TarifasMaritima> TarifasMaritimas { get; set; } = default!;
        public IList<Tarifasterrestre> TarifasTerrestres { get; set; } = default!;
        public IList<TarifasMaestra> TarifasMaestrasDocumentales { get; set; } = default!; // <-- NUEVO


        // ==================== MÉTRICAS (KPIs) ====================
        public int CountClientesActivas { get; set; }
        public int CountMaritimasActivas { get; set; }
        public int CountTerrestresActivas { get; set; }
        public int CountPorExpirar { get; set; }

        // ==================== BINDINGS PARA MODALES ====================
        [BindProperty] public TarifasCliente NuevaVentaMaritima { get; set; } = new();
        [BindProperty] public TarifasCliente NuevaVentaTerrestre { get; set; } = new();
        [BindProperty] public TarifasMaritima NuevoCostoMaritimo { get; set; } = new();
        [BindProperty] public Tarifasterrestre NuevoCostoTerrestre { get; set; } = new();

        // RESTAURADOS: Bindings para los Maestros Superiores
        [BindProperty] public Proveedore NuevoProveedor { get; set; } = new();
        [BindProperty] public Conductore NuevoConductor { get; set; } = new();

        // ==================== SELECT LISTS ====================
        public SelectList ListaClientes { get; set; } = default!;
        public SelectList ListaProveedores { get; set; } = default!;
        public SelectList ListaCiudades { get; set; } = default!;
        public SelectList ListaNavieras { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Cargar lista de Agencias de Aduana para el Select
            ListaAgencias = new SelectList(await _context.AgenciasAduanas.Where(a => a.Activo == 1).ToListAsync(), "Id", "NombreAgencia");

            // Cargar los costos documentales que estén asociados a una agencia
            CostosAgencias = await _context.TarifasDocumentales
                .Include(t => t.IdAgenciaAduanaNavigation)
                .Where(t => t.EsActiva == true && t.IdAgenciaAduana != null)
                .ToListAsync();

            await CargarDatosYMetricas();
        }

        private async Task CargarDatosYMetricas()
        {
            DateTime hoyDt = DateTime.Now.Date;
            DateTime limiteExpiracionDt = hoyDt.AddDays(30);

            // 1. Cargar Maestros Base
            var clientes = await _context.Clientes.Where(c => c.Activo == 1).OrderBy(c => c.RazonSocial).ToListAsync();
            ListaClientes = new SelectList(clientes, "Id", "RazonSocial");

            var proveedores = await _context.Proveedores.Where(p => p.Activo == 1).OrderBy(p => p.NombreProveedor).ToListAsync();
            ListaProveedores = new SelectList(proveedores, "Id", "NombreProveedor");

            ListaCiudades = new SelectList(await _context.Set<Ciudade>().OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre");
            ListaNavieras = new SelectList(await _context.Navieras.Where(n => n.Activo == 1).OrderBy(n => n.NombreNaviera).ToListAsync(), "Id", "NombreNaviera");

            // 2. Cargar Tarifas y Tablas
            TarifasClientes = await _context.TarifasClientes
                .Include(t => t.IdClienteNavigation)
                .Include(t => t.IdNavieraNavigation)
                .Where(t => t.EsActiva == true)
                .OrderByDescending(t => t.Id)
                .ToListAsync();

            TarifasMaritimas = await _context.TarifasMaritimas
                .Include(t => t.IdNavieraNavigation)
                .OrderByDescending(t => t.Id)
                .ToListAsync();

            TarifasTerrestres = await _context.Tarifasterrestres
                .Include(t => t.IdProveedorNavigation)
                .Include(t => t.IdCiudadOrigenNavigation)
                .Include(t => t.IdCiudadDestinoNavigation)
                .OrderByDescending(t => t.Id)
                .ToListAsync();

            TarifasMaestrasDocumentales = await _context.TarifasMaestras
                .Where(t => t.EsActiva && t.Categoria == "Documental")
                .OrderByDescending(t => t.Id)
                .ToListAsync();

            // 3. Métricas
            CountClientesActivas = TarifasClientes.Count(t => t.FechaFinVigencia.Date >= hoyDt);
            CountMaritimasActivas = TarifasMaritimas.Count;
            CountTerrestresActivas = TarifasTerrestres.Count;
            CountPorExpirar = TarifasClientes.Count(t => t.FechaFinVigencia.Date >= hoyDt && t.FechaFinVigencia.Date <= limiteExpiracionDt);
        }

        // ==================== HANDLERS: VENTAS A CLIENTES ====================

        public async Task<IActionResult> OnPostCrearVentaMaritimaAsync()
        {
            NuevaVentaMaritima.GrupoCobro = "Marítimo";
            NuevaVentaMaritima.EsActiva = true;
            NuevaVentaMaritima.UsuarioCreador = User.Identity?.Name ?? "Comercial";

            _context.TarifasClientes.Add(NuevaVentaMaritima);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostCrearVentaTerrestreAsync()
        {
            NuevaVentaTerrestre.GrupoCobro = "Terrestre";
            NuevaVentaTerrestre.EsActiva = true;
            NuevaVentaTerrestre.UsuarioCreador = User.Identity?.Name ?? "Comercial";

            _context.TarifasClientes.Add(NuevaVentaTerrestre);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostEliminarTarifaClienteAsync(int idTarifa)
        {
            var tarifa = await _context.TarifasClientes.FindAsync(idTarifa);
            if (tarifa != null)
            {
                tarifa.EsActiva = false; // Soft delete para historial financiero
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }

        // ==================== HANDLERS: COSTOS DE PROVEEDORES ====================

        public async Task<IActionResult> OnPostCrearCostoMaritimoAsync()
        {
            NuevoCostoMaritimo.EsActiva = true;
            _context.TarifasMaritimas.Add(NuevoCostoMaritimo);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostEliminarCostoMaritimoAsync(int idTarifa)
        {
            var tarifa = await _context.TarifasMaritimas.FindAsync(idTarifa);
            if (tarifa != null)
            {
                _context.TarifasMaritimas.Remove(tarifa);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostCrearCostoTerrestreAsync()
        {
            NuevoCostoTerrestre.FechaCreacion = DateTime.Now;
            NuevoCostoTerrestre.UsuarioCreador = User.Identity?.Name ?? "Comercial";

            _context.Tarifasterrestres.Add(NuevoCostoTerrestre);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostEliminarCostoTerrestreAsync(int idTarifa)
        {
            var tarifa = await _context.Tarifasterrestres.FindAsync(idTarifa);
            if (tarifa != null)
            {
                _context.Tarifasterrestres.Remove(tarifa);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }

        // ==================== HANDLERS: COSTOS DOCUMENTALES (TARIFAS MAESTRAS) ====================
        [BindProperty] public TarifasMaestra NuevaTarifaDocumental { get; set; } = new();

        public async Task<IActionResult> OnPostCrearTarifaDocumentalAsync()
        {
            NuevaTarifaDocumental.Categoria = "Documental";
            NuevaTarifaDocumental.EsActiva = true;
            _context.TarifasMaestras.Add(NuevaTarifaDocumental);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostEliminarTarifaDocumentalAsync(int idTarifa)
        {
            var tarifa = await _context.TarifasMaestras.FindAsync(idTarifa);
            if (tarifa != null)
            {
                tarifa.EsActiva = false; // Soft delete
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }

        // ==================== HANDLERS: MAESTROS (PROVEEDORES Y CONDUCTORES) ====================

        public async Task<IActionResult> OnPostCrearProveedorAsync()
        {
            NuevoProveedor.Activo = 1;
            _context.Proveedores.Add(NuevoProveedor);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostCrearConductorAsync()
        {
            NuevoConductor.Activo = true;
            _context.Conductores.Add(NuevoConductor);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostCrearCostoDocumentalAsync()
        {
            NuevoCostoDocumental.EsActiva = true;
            _context.TarifasDocumentales.Add(NuevoCostoDocumental);
            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Tarifa de Agencia Aduanal registrada correctamente.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarCostoDocumentalAsync(int idTarifa)
        {
            var tarifa = await _context.TarifasDocumentales.FindAsync(idTarifa);
            if (tarifa != null)
            {
                tarifa.EsActiva = false; // Borrado lógico (lo archivamos)
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Costo de Agencia archivado.";
            }
            return RedirectToPage();
        }
    }
}