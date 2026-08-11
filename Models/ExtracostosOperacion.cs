using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class ExtracostosOperacion
{
    public int Id { get; set; }

    public int IdOperacion { get; set; }

    public string TipoCosto { get; set; } = null!;

    public string? Motivo { get; set; }

    public decimal Monto { get; set; }

    public string Moneda { get; set; } = null!;

    public string Evidencia { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreador { get; set; } = null!;

    public virtual Operacione IdOperacionNavigation { get; set; } = null!;

    public decimal CostoReal { get; set; } // Reemplaza a 'Monto'
    public decimal ValorVentaPropuesto { get; set; }
    public bool CobroAceptadoCliente { get; set; }

    public string? Responsable { get; set; }
}
