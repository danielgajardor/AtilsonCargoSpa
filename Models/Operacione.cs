using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; // <-- Agregado para usar [ForeignKey]

namespace AtilsonCargoSpa.Models;

public partial class Operacione
{
    public int Id { get; set; }

    public string NumeroBooking { get; set; } = null!;

    public int IdCliente { get; set; }

    public int IdNaviera { get; set; }

    public int? IdOrigen { get; set; }

    public int? IdDeposito { get; set; }

    public int? IdPuertoOrigen { get; set; }

    public int? IdPuertoDestino { get; set; }

    public int? IdViaje { get; set; }

    public int IdTipoCarga { get; set; }

    public string? Commodity { get; set; }

    public DateTime? FechaStacking { get; set; }

    public DateTime? CutOffMatriz { get; set; }

    public int? IdEstadoOperacion { get; set; }

    public int? IdTipoMovimiento { get; set; }

    public int? IdTipoServicio { get; set; }

    public byte? Activo { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public string? UsuarioCreador { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificador { get; set; }

    public DateTime? LateArrival { get; set; }

    public DateTime? ExtraLateArrival { get; set; }

    public string? Nave { get; set; }

    public string? Transbordo { get; set; }

    public string? Comentarios { get; set; }

    public double? Temperatura { get; set; }

    public string? Atmosfera { get; set; }

    public double? Co2 { get; set; }

    public double? O2 { get; set; }

    public double? Ventilacion { get; set; }

    public double? Humedad { get; set; }

    public bool IsDeleted { get; set; }

    public int? CondicionPago { get; set; }

    public string? NumeroContenedor { get; set; }

    public string? SelloNaviera { get; set; }

    public int? CantidadBultos { get; set; }

    public string? TipoContenedor { get; set; }

    public bool RequiereGenset { get; set; }

    public int? DiasLibresDemurrage { get; set; }

    public int? DiasLibresDetention { get; set; }

    public DateTime? EtdPol { get; set; }

    public DateTime? EtaPod { get; set; }

    public int? IdNave { get; set; }

    public string? CondicionReefer { get; set; }

    public string? MarcaAc { get; set; }

    public string? TipoViaje { get; set; }

    public string? CodigoAtilson { get; set; }

    public string? EstadoWorkflow { get; set; }

    public string? EvidenciaContenedor { get; set; }

    public string? EstadoLar { get; set; }

    public string? EvidenciaLar { get; set; }

    public bool ContenedorIngresado { get; set; }

    public string? TerminalPortuario { get; set; }

    public decimal? Kilos { get; set; }

    public decimal? Vgm { get; set; }

    public DateTime? ElarDate { get; set; }
    public string? NumeroSello { get; set; }
    public string? FotoContenedor { get; set; }
    public string? FotoSello { get; set; }

    public string? PaisOrigen { get; set; }

    public string? GestionaGate { get; set; }
    public string? PagoGate { get; set; }
    public string? TipoGate { get; set; }

    public virtual ICollection<ExtracostosOperacion> ExtracostosOperacions { get; set; } = new List<ExtracostosOperacion>();

    public virtual ICollection<Finanzasoperacion> Finanzasoperacions { get; set; } = new List<Finanzasoperacion>();

    // 👇 SOLUCIÓN: Data Annotations para las llaves foráneas 👇

    [ForeignKey("IdCliente")]
    public virtual Cliente IdClienteNavigation { get; set; } = null!;

    [ForeignKey("IdDeposito")]
    public virtual Deposito? IdDepositoNavigation { get; set; }

    [ForeignKey("IdNave")]
    public virtual Nafe? IdNaveNavigation { get; set; }

    [ForeignKey("IdNaviera")]
    public virtual Naviera IdNavieraNavigation { get; set; } = null!;

    [ForeignKey("IdOrigen")]
    public virtual Origenescarga? IdOrigenNavigation { get; set; }

    [ForeignKey("IdPuertoDestino")]
    public virtual Puerto? IdPuertoDestinoNavigation { get; set; }

    [ForeignKey("IdPuertoOrigen")]
    public virtual Puerto? IdPuertoOrigenNavigation { get; set; }

    [ForeignKey("IdViaje")]
    public virtual Viaje? IdViajeNavigation { get; set; }

    public string? NumeroInstructivo { get; set; }

    public virtual ICollection<OperacionesDocumentale> OperacionesDocumentales { get; set; } = new List<OperacionesDocumentale>();

    public virtual ICollection<OperacionesTerrestre> OperacionesTerrestres { get; set; } = new List<OperacionesTerrestre>();

    public virtual ICollection<Unidadestecnica> Unidadestecnicas { get; set; } = new List<Unidadestecnica>();

    public virtual ICollection<TransaccionesFinanciera> TransaccionesFinancieras { get; set; } = new List<TransaccionesFinanciera>();
    public virtual ICollection<OperacionesAlmacenamiento> OperacionesAlmacenamientos { get; set; } = new List<OperacionesAlmacenamiento>();

    public bool LockFinanzas { get; set; }
    public string? EstadoFinanzas { get; set; }
    public DateTime? FechaEnvioFinanzas { get; set; }

    public string? HistorialCorreos { get; set; }
    public bool? CorreoClienteEnviado { get; set; } = false;
    public int? VersionCorreo { get; set; } = 1;

    // Opcional: Para saber exactamente cuándo se mandó la última versión
    public DateTime? FechaUltimoEnvioCorreo { get; set; }
}