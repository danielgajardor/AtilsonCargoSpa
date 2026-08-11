using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtilsonCargoSpa.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly AtilsonContext _context;

        public DashboardModel(AtilsonContext context)
        {
            _context = context;
        }

        // KPIs Generales
        public int TotalBookings { get; set; }
        public int TotalClientes { get; set; }
        public int CantidadCriticos { get; set; }
        public List<Operacione> UltimasOperaciones { get; set; } = new();

        // ----------------------------------------------------
        // NUEVAS PROPIEDADES PARA GRÁFICOS INTERACTIVOS
        // ----------------------------------------------------

        // 1. Mix de Servicios
        public int CountIntegral { get; set; }
        public int CountMaritimo { get; set; }
        public int CountTerrestre { get; set; }
        public int CountDocumental { get; set; }

        // 2. Control Marítimo (LAR)
        public int CountMaritimoATiempo { get; set; }
        public int CountMaritimoConLAR { get; set; }

        // 3. Documental (Matrices)
        public int CountMatricesConfirmadas { get; set; }
        public int CountMatricesPendientes { get; set; }

        // 4. Estado de Bookings (Workflow)
        public int CountPendienteNaviera { get; set; }
        public int CountConfirmadoCliente { get; set; }
        public int CountCanceladas { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                // Query base para ignorar los eliminados (Activo == 1 o null)
                var queryActivas = _context.Operaciones.Where(o => o.Activo == 1 || o.Activo == null);

                TotalBookings = await queryActivas.CountAsync();
                TotalClientes = await _context.Clientes.CountAsync();

                DateTime limite = DateTime.Now.AddDays(2);
                CantidadCriticos = await queryActivas
                    .CountAsync(o => o.FechaStacking >= DateTime.Now && o.FechaStacking <= limite);

                // ==========================================
                // CÁLCULO DE MÉTRICAS PARA GRÁFICOS
                // ==========================================

                // 1. Tipos de Servicio (Mix Comercial)
                CountIntegral = await queryActivas.CountAsync(o => o.IdTipoServicio == 1 || o.IdTipoServicio == 8);
                CountMaritimo = await queryActivas.CountAsync(o => o.IdTipoServicio == 5 || o.IdTipoServicio == 12);
                CountTerrestre = await queryActivas.CountAsync(o => o.IdTipoServicio == 6 || o.IdTipoServicio == 13);
                CountDocumental = await queryActivas.CountAsync(o => o.IdTipoServicio == 7 || o.IdTipoServicio == 14);

                // 2. Control LAR (Filtramos solo operaciones que tienen tramo marítimo)
                var queryMaritimas = queryActivas.Where(o => new[] { 1, 2, 3, 5, 8, 9, 10, 12 }.Contains(o.IdTipoServicio ?? 0));
                int totalMaritimas = await queryMaritimas.CountAsync();
                CountMaritimoConLAR = await queryMaritimas.CountAsync(o => o.EstadoLar != null && o.EstadoLar.Contains("LAR"));
                CountMaritimoATiempo = totalMaritimas - CountMaritimoConLAR;

                // 3. Desempeño Documental (Matrices B/L)
                // Se filtra por los tipos de servicio que requieren gestión marítima o documental
                var queryConMatriz = queryActivas.Where(o => new[] { 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 14 }.Contains(o.IdTipoServicio ?? 0));

                CountMatricesConfirmadas = await queryConMatriz
                    .CountAsync(o => o.OperacionesDocumentales.Any(d => d.MatrizPresentada == true));

                CountMatricesPendientes = await queryConMatriz.CountAsync() - CountMatricesConfirmadas;

                // 4. Estatus de Bookings / Workflow
                CountPendienteNaviera = await queryActivas.CountAsync(o => o.EstadoWorkflow != null && o.EstadoWorkflow.Contains("Solicitado"));
                CountConfirmadoCliente = await queryActivas.CountAsync(o => o.EstadoWorkflow != null && o.EstadoWorkflow.Contains("Confirmado"));
                CountCanceladas = await _context.Operaciones.CountAsync(o => o.EstadoWorkflow != null && o.EstadoWorkflow.Contains("Cancelado"));

                // ==========================================
                // OBTENCIÓN DE ÚLTIMAS OPERACIONES (TABLA)
                // ==========================================
                UltimasOperaciones = await queryActivas
                    .Include(o => o.IdClienteNavigation)
                    .Include(o => o.IdPuertoDestinoNavigation)
                    .OrderByDescending(o => o.Id)
                    .Take(5)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // En caso de que haya una tabla vacía o error de base de datos
                UltimasOperaciones = new List<Operacione>();
                Console.WriteLine(ex.Message);
            }
        }
    }
}