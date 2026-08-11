using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class TarifasMaritima
{
    public int Id { get; set; }
    public int IdNaviera { get; set; }
    public string Pol { get; set; } = null!;
    public string Pod { get; set; } = null!;
    public string? PaisDestino { get; set; }
    public string Equipamiento { get; set; } = null!;
    public decimal TarifaUsd { get; set; }
    public string? DiasLibresOrigen { get; set; }
    public string? DiasLibresDestino { get; set; }
    public string? Comentarios { get; set; }
    public string? RutaRespaldo { get; set; }

    // --- NUEVO HISTORIAL COMERCIAL ---
    public DateOnly FechaInicioVigencia { get; set; }
    public DateOnly? FechaFinVigencia { get; set; }
    public bool EsActiva { get; set; } = true;

    public virtual Naviera IdNavieraNavigation { get; set; } = null!;

    public decimal? CostoLateArrival { get; set; }
    public decimal? CostoCorrectorBL { get; set; }
    public decimal? DemurrageDiario { get; set; }
}