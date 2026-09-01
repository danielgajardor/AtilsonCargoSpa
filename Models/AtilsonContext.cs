using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AtilsonCargoSpa.Models;

public partial class AtilsonContext : DbContext
{
    public AtilsonContext()
    {
    }

    public AtilsonContext(DbContextOptions<AtilsonContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Ciudade> Ciudades { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<Conductore> Conductores { get; set; }

    public virtual DbSet<Deposito> Depositos { get; set; }

    public virtual DbSet<ExtracostosOperacion> ExtracostosOperacions { get; set; }

    public virtual DbSet<Finanzasoperacion> Finanzasoperacions { get; set; }


    public virtual DbSet<Nafe> Naves { get; set; }

    public virtual DbSet<Naviera> Navieras { get; set; }

    public virtual DbSet<Operacione> Operaciones { get; set; }

    public virtual DbSet<OperacionesDocumentale> OperacionesDocumentales { get; set; }
     
    public virtual DbSet<OperacionesTerrestre> OperacionesTerrestres { get; set; }
    public virtual DbSet<TarifasAlmacenamiento> TarifasAlmacenamientos { get; set; }
    public virtual DbSet<OperacionesAlmacenamiento> OperacionesAlmacenamientos { get; set; }

    public virtual DbSet<Origenescarga> Origenescargas { get; set; }

    public virtual DbSet<Paise> Paises { get; set; }

    public virtual DbSet<Parametro> Parametros { get; set; }

    public virtual DbSet<Proveedore> Proveedores { get; set; }

    public virtual DbSet<Puerto> Puertos { get; set; }

    public virtual DbSet<Subparametro> Subparametros { get; set; }

    public virtual DbSet<TarifasMaritima> TarifasMaritimas { get; set; }

    public virtual DbSet<Tarifasterrestre> Tarifasterrestres { get; set; }

    public virtual DbSet<TarifasDocumentale> TarifasDocumentales { get; set; }

    public virtual DbSet<Unidadestecnica> Unidadestecnicas { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Cotizacion> Cotizaciones { get; set; }

    public virtual DbSet<TarifasMaestra> TarifasMaestras { get; set; }
    public virtual DbSet<TarifasCliente> TarifasClientes { get; set; } = default!;

    public virtual DbSet<Viaje> Viajes { get; set; }
    public virtual DbSet<Planta> Plantas { get; set; }

    public virtual DbSet<TarifaGate> TarifasGate { get; set; }
    public virtual DbSet<AgenciasAduana> AgenciasAduanas { get; set; } = null!;

    public virtual DbSet<TransaccionesFinanciera> TransaccionesFinancieras { get; set; }

    

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost\\ATILSON;Database=AtilsonLogistica;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Planta>(entity =>
        {
            entity.ToTable("Plantas");

            entity.Property(e => e.Nombre)
                .IsRequired()
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.Property(e => e.Direccion)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.Property(e => e.UrlGoogleMaps)
                .IsUnicode(false);

            entity.Property(e => e.UsuarioCreador)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValueSql("('Sistema')");

            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())");

            entity.Property(e => e.Activo)
                .HasDefaultValueSql("((1))");

            // Configuración de la llave foránea con Ciudade
            entity.HasOne(d => d.Ciudad)
                .WithMany(p => p.Plantas)
                .HasForeignKey(d => d.IdCiudad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Plantas_Ciudades");


        });

        modelBuilder.Entity<Ciudade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ciudades__3214EC07AF7057E1");

            entity.ToTable("ciudades");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");

            entity.HasOne(d => d.IdPaisNavigation).WithMany(p => p.Ciudades)
                .HasForeignKey(d => d.IdPais)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ciudades__IdPais__47DBAE45");
        });

        modelBuilder.Entity<TarifasCliente>(entity => { entity.ToTable("TarifasClientes"); });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__clientes__3214EC0728490F58");

            entity.ToTable("clientes");

            entity.HasIndex(e => e.Rut, "UX_Clientes_Rut")
                .IsUnique()
                .HasFilter("([Rut] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue((byte)1);
            entity.Property(e => e.Contacto)
                .HasMaxLength(100)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.Direccion)
                .HasMaxLength(255)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IdCiudad).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.NombreCliente)
                .HasMaxLength(100)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.RazonSocial).HasMaxLength(100);
            entity.Property(e => e.Rut)
                .HasMaxLength(12)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");

            entity.HasOne(d => d.IdCiudadNavigation).WithMany(p => p.Clientes)
                .HasForeignKey(d => d.IdCiudad)
                .HasConstraintName("FK__clientes__IdCiud__60A75C0F");
        });

        modelBuilder.Entity<Conductore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Conducto__3214EC0757D69557");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Patente).HasMaxLength(20);
            entity.Property(e => e.Rut).HasMaxLength(20);
            entity.Property(e => e.Telefono).HasMaxLength(50);

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.Conductores)
                .HasForeignKey(d => d.IdProveedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Conductor__IdPro__44952D46");
        });

        modelBuilder.Entity<Deposito>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__deposito__3214EC078E0B28F6");

            entity.ToTable("depositos");

            entity.Property(e => e.Activo).HasDefaultValue((byte)1);
            entity.Property(e => e.Direccion)
                .HasMaxLength(255)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IdCiudad).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IdNaviera).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.NombreDeposito).HasMaxLength(100);
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");

            entity.HasOne(d => d.IdCiudadNavigation).WithMany(p => p.Depositos)
                .HasForeignKey(d => d.IdCiudad)
                .HasConstraintName("FK__depositos__IdCiu__59FA5E80");

            entity.HasOne(d => d.IdNavieraNavigation).WithMany(p => p.Depositos)
                .HasForeignKey(d => d.IdNaviera)
                .HasConstraintName("FK__depositos__IdNav__571DF1D5");
        });

        modelBuilder.Entity<ExtracostosOperacion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Extracos__3214EC07CCBB9A84");

            entity.ToTable("ExtracostosOperacion");

            entity.Property(e => e.Evidencia)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Motivo)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.TipoCosto)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.IdOperacionNavigation).WithMany(p => p.ExtracostosOperacions)
                .HasForeignKey(d => d.IdOperacion)
                .HasConstraintName("FK_ExtraCostos_Operacion");
        });

        modelBuilder.Entity<Finanzasoperacion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__finanzas__3214EC073DC1CCD8");

            entity.ToTable("finanzasoperacion");

            entity.Property(e => e.CostoAgenciaNeto)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CostoMaritimoNeto)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CostoTerrestreNeto)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CostoTerrestreManual)          // 👈 NUEVO
                .HasDefaultValueSql("(NULL)");                     // 👈 NUEVO
            entity.Property(e => e.DiasLibresDestino).HasDefaultValueSql("(NULL)");

            entity.Property(e => e.DiasLibresDestino).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.DiasLibresOrigen).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IdCondicionFlete).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.VentaDocumental)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.VentaMaritimo)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.VentaTerrestre)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdOperacionNavigation).WithMany(p => p.Finanzasoperacions)
                .HasForeignKey(d => d.IdOperacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__finanzaso__IdOpe__2BFE89A6");
        });

        modelBuilder.Entity<Nafe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__naves__3214EC0764435126");

            entity.ToTable("naves");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");

            entity.HasOne(d => d.IdNavieraNavigation).WithMany(p => p.Naves)
                .HasForeignKey(d => d.IdNaviera)
                .HasConstraintName("FK_Naves_Navieras");
        });

        modelBuilder.Entity<Naviera>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__navieras__3214EC07D56AFB46");

            entity.ToTable("navieras");

            entity.Property(e => e.Activo).HasDefaultValue((byte)1);
            entity.Property(e => e.ColorRepresentativo)
                .HasMaxLength(20)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LinkTracking)
                .HasMaxLength(500)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.NombreNaviera).HasMaxLength(45);
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
        });

        modelBuilder.Entity<Operacione>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__operacio__3214EC07A9A23533");

            entity.ToTable("operaciones");

            entity.HasIndex(e => e.NumeroBooking, "UQ__operacio__3A320A4FA9C754E2").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue((byte)1);
            entity.Property(e => e.Atmosfera)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CodigoAtilson)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Comentarios).IsUnicode(false);
            entity.Property(e => e.Commodity)
                .HasMaxLength(100)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.CondicionReefer)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CutOffMatriz)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("datetime");
            entity.Property(e => e.EstadoLar)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EstadoWorkflow)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("EN PROCESO");
            entity.Property(e => e.EtaPod).HasColumnType("datetime");
            entity.Property(e => e.EtdPol).HasColumnType("datetime");
            entity.Property(e => e.EvidenciaContenedor)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.EvidenciaLar)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.ExtraLateArrival).HasColumnType("datetime");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaStacking)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("datetime");
            entity.Property(e => e.IdDeposito).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IdEstadoOperacion).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IdOrigen).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IdPuertoDestino).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IdPuertoOrigen).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IdTipoMovimiento).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IdTipoServicio).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IdViaje).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.Kilos).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.LateArrival).HasColumnType("datetime");
            entity.Property(e => e.MarcaAc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Nave)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NumeroBooking).HasMaxLength(45);
            entity.Property(e => e.NumeroContenedor).HasMaxLength(50);
            entity.Property(e => e.SelloNaviera).HasMaxLength(50);
            entity.Property(e => e.TerminalPortuario)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TipoContenedor).HasMaxLength(50);
            entity.Property(e => e.TipoViaje)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Transbordo)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.Vgm).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.Operaciones)
                .HasForeignKey(d => d.IdCliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__operacion__IdCli__07C12930");

            entity.HasOne(d => d.IdDepositoNavigation).WithMany(p => p.Operaciones)
                .HasForeignKey(d => d.IdDeposito)
                .HasConstraintName("FK__operacion__IdDep__0C85DE4D");

            entity.HasOne(d => d.IdNaveNavigation).WithMany(p => p.Operaciones)
                .HasForeignKey(d => d.IdNave)
                .HasConstraintName("FK_Operaciones_Naves");

            entity.HasOne(d => d.IdNavieraNavigation).WithMany(p => p.Operaciones)
                .HasForeignKey(d => d.IdNaviera)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__operacion__IdNav__08B54D69");

            entity.HasOne(d => d.IdOrigenNavigation).WithMany(p => p.Operaciones)
                .HasForeignKey(d => d.IdOrigen)
                .HasConstraintName("FK__operacion__IdOri__0A9D95DB");

            entity.HasOne(d => d.IdPuertoDestinoNavigation).WithMany(p => p.OperacioneIdPuertoDestinoNavigations)
                .HasForeignKey(d => d.IdPuertoDestino)
                .HasConstraintName("FK__operacion__IdPue__10566F31");

            entity.HasOne(d => d.IdPuertoOrigenNavigation).WithMany(p => p.OperacioneIdPuertoOrigenNavigations)
                .HasForeignKey(d => d.IdPuertoOrigen)
                .HasConstraintName("FK__operacion__IdPue__0E6E26BF");

            entity.HasOne(d => d.IdViajeNavigation).WithMany(p => p.Operaciones)
                .HasForeignKey(d => d.IdViaje)
                .HasConstraintName("FK__operacion__IdVia__123EB7A3");
        });

        modelBuilder.Entity<OperacionesDocumentale>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Operacio__3214EC07FE42183F");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.AgenciaAduana)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.CertificadoFito).HasDefaultValue(false);
            entity.Property(e => e.CertificadoOrigen).HasDefaultValue(false);
            entity.Property(e => e.CertificadoSeguro).HasDefaultValue(false);
            entity.Property(e => e.Consignatario)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.DusDin)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EstadoDocumental)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("PENDIENTE");
            entity.Property(e => e.EvidenciaMatriz)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.Mandato)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NotificarA)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ValorAduana).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdOperacionNavigation).WithMany(p => p.OperacionesDocumentales)
                .HasForeignKey(d => d.IdOperacion)
                .HasConstraintName("FK_OpeDocumental_Operaciones");
        });

        modelBuilder.Entity<OperacionesTerrestre>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Operacio__3214EC077F2DB409");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CorreoTransporte)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.DepositoDevolucion)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.DepositoRetiro)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.EmpresaTransporte)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FechaCarga).HasColumnType("datetime");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.LinkTracking)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.LlegadaPlanta).HasColumnType("datetime");
            entity.Property(e => e.LlegadaPuerto).HasColumnType("datetime");
            entity.Property(e => e.NombreConductor)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Patente)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PlantaCarga)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Rampla)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ReferenciaCliente)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RutConductor)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RutTransporte)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SalidaPlanta).HasColumnType("datetime");
            entity.Property(e => e.SalidaPuerto).HasColumnType("datetime");
            entity.Property(e => e.TelefonoConductor)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TipoUnidadTransporte)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ZonaEmbarque)
                .HasMaxLength(150)
                .IsUnicode(false);

            entity.HasOne(d => d.IdOperacionNavigation).WithMany(p => p.OperacionesTerrestres)
                .HasForeignKey(d => d.IdOperacion)
                .HasConstraintName("FK_OpeTerrestre_Operaciones");
        });

        modelBuilder.Entity<Origenescarga>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__origenes__3214EC0756EB3858");

            entity.ToTable("origenescarga");

            entity.Property(e => e.Activo).HasDefaultValue((byte)1);
            entity.Property(e => e.Direccion)
                .HasMaxLength(255)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IdCiudad).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.NombrePlanta).HasMaxLength(100);
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");

            entity.HasOne(d => d.IdCiudadNavigation).WithMany(p => p.Origenescargas)
                .HasForeignKey(d => d.IdCiudad)
                .HasConstraintName("FK__origenesc__IdCiu__02084FDA");
        });

        modelBuilder.Entity<Paise>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__paises__3214EC076036297E");

            entity.ToTable("paises");

            entity.Property(e => e.CodigoAlfa)
                .HasMaxLength(10)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
        });

        modelBuilder.Entity<Parametro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__parametr__3214EC073FAD1871");

            entity.ToTable("parametros");

            entity.Property(e => e.Categoria).HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
        });

        modelBuilder.Entity<Proveedore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__proveedo__3214EC0757E7544B");

            entity.ToTable("proveedores");

            entity.HasIndex(e => e.Rut, "UX_Proveedores_Rut")
                .IsUnique()
                .HasFilter("([Rut] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue((byte)1);
            entity.Property(e => e.Contacto)
                .HasMaxLength(100)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.CorreoOperativo)
                .HasMaxLength(100)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.NombreProveedor).HasMaxLength(100);
            entity.Property(e => e.Rut)
                .HasMaxLength(12)
                .HasDefaultValueSql("(NULL)");
        });

        modelBuilder.Entity<Puerto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__puertos__3214EC072A9DED2B");

            entity.ToTable("puertos");

            entity.Property(e => e.Activo).HasDefaultValue((byte)1);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IdCiudad).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IdTipoPuerto).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.NombrePuerto).HasMaxLength(100);
            entity.Property(e => e.TerminalPortuario)
                .HasMaxLength(100)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");

            entity.HasOne(d => d.IdCiudadNavigation).WithMany(p => p.Puertos)
                .HasForeignKey(d => d.IdCiudad)
                .HasConstraintName("FK__puertos__IdCiuda__4BAC3F29");
        });

        modelBuilder.Entity<Subparametro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__subparam__3214EC0727405E99");

            entity.ToTable("subparametros");

            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.Valor).HasMaxLength(100);

            entity.HasOne(d => d.Parametro).WithMany(p => p.Subparametros)
                .HasForeignKey(d => d.ParametroId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subparametros_Parametros");
        });

        modelBuilder.Entity<TarifasDocumentale>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_TarifasDocumentales");

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");

            // 👈 Nueva relación con AgenciasAduana
            entity.HasOne(d => d.IdAgenciaAduanaNavigation)
                  .WithMany(p => p.TarifasDocumentales)
                  .HasForeignKey(d => d.IdAgenciaAduana)
                  .HasConstraintName("FK_TarifasDocumentales_AgenciasAduanas");
        });

        modelBuilder.Entity<TarifasMaritima>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TarifasM__3214EC0701BED886");

            entity.Property(e => e.DiasLibresDestino).HasMaxLength(50);
            entity.Property(e => e.DiasLibresOrigen).HasMaxLength(50);
            entity.Property(e => e.Equipamiento).HasMaxLength(50);
            entity.Property(e => e.PaisDestino).HasMaxLength(50);
            entity.Property(e => e.Pod).HasMaxLength(100);
            entity.Property(e => e.Pol).HasMaxLength(100);
            entity.Property(e => e.RutaRespaldo).HasMaxLength(255);
            entity.Property(e => e.TarifaUsd).HasColumnType("decimal(18, 2)");

            // --- AQUI ESTA LA MAGIA: LA RELACION CON NAVIERAS QUE FALTABA ---
            entity.HasOne(d => d.IdNavieraNavigation)
                .WithMany(p => p.TarifasMaritimas)
                .HasForeignKey(d => d.IdNaviera)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TarifasMaritimas_Navieras");
        });

        modelBuilder.Entity<Tarifasterrestre>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tarifast__3214EC07739F61B4");

            entity.ToTable("tarifasterrestres");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaFinVigencia).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.FechaInicioVigencia).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IdCiudadDestino).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IdCiudadOrigen).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IdProveedor).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IdTipoCarga).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.RutaRespaldo).HasMaxLength(255);
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.ValorNeto)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdCiudadDestinoNavigation).WithMany(p => p.TarifasterrestreIdCiudadDestinoNavigations)
                .HasForeignKey(d => d.IdCiudadDestino)
                .HasConstraintName("FK__tarifaste__IdCiu__74AE54BC");

            entity.HasOne(d => d.IdCiudadOrigenNavigation).WithMany(p => p.TarifasterrestreIdCiudadOrigenNavigations)
                .HasForeignKey(d => d.IdCiudadOrigen)
                .HasConstraintName("FK__tarifaste__IdCiu__72C60C4A");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.Tarifasterrestres)
                .HasForeignKey(d => d.IdProveedor)
                .HasConstraintName("FK__tarifaste__IdPro__70DDC3D8");
            entity.HasOne(d => d.IdCiudadPlantaNavigation)
          .WithMany()
          .HasForeignKey(d => d.IdCiudadPlanta)
          .HasConstraintName("FK_tarifasterrestres_ciudades_planta");
        });

        modelBuilder.Entity<Unidadestecnica>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__unidades__3214EC0731AB95CE");

            entity.ToTable("unidadestecnicas");

            entity.HasIndex(e => e.NroContenedor, "UX_Unidades_Contenedor")
                .IsUnique()
                .HasFilter("([NroContenedor] IS NOT NULL)");

            entity.Property(e => e.AtmosferaControlada).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.Commodity).HasMaxLength(100);
            entity.Property(e => e.CondicionReefer)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.EvidenciaContenedor)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Humedad).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.MarcaAc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NivelCo2)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("NivelCO2");
            entity.Property(e => e.NivelO2).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.NroContenedor)
                .HasMaxLength(15)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.PesoCarga)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.SelloNaviera)
                .HasMaxLength(45)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.Tara)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Temperatura)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TipoAtmosfera).HasMaxLength(20);
            entity.Property(e => e.TipoContenedor).HasMaxLength(50);
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.Ventilacion).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.VgmTotal)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.IdOperacionNavigation).WithMany(p => p.Unidadestecnicas)
                .HasForeignKey(d => d.IdOperacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__unidadest__IdOpe__1F98B2C1");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuarios__3214EC071E1CD0AA");

            entity.HasIndex(e => e.Correo, "UQ__Usuarios__60695A19B71CAD64").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue((byte)1);
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NombreCompleto)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Rol)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Viaje>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__viajes__3214EC073D3734E7");

            entity.ToTable("viajes");

            entity.Property(e => e.EtaArribo).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.EtdZarpe).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NumeroViaje)
                .HasMaxLength(20)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");
            entity.Property(e => e.UsuarioModificador)
                .HasMaxLength(50)
                .HasDefaultValue("Sistema");

            entity.HasOne(d => d.IdNaveNavigation).WithMany(p => p.Viajes)
                .HasForeignKey(d => d.IdNave)
                .HasConstraintName("FK_Viajes_Naves");
        });

        OnModelCreatingPartial(modelBuilder);

        modelBuilder.Entity<AgenciasAduana>(entity =>
        {
            entity.ToTable("AgenciasAduana");

            entity.Property(e => e.Contacto).HasMaxLength(100);
            entity.Property(e => e.CorreoContacto).HasMaxLength(100);
            entity.Property(e => e.FechaCreacion)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.NombreAgencia).HasMaxLength(100);
            entity.Property(e => e.Rut).HasMaxLength(20);
            entity.Property(e => e.UsuarioCreador)
                .HasMaxLength(50)
                .HasDefaultValueSql("('Sistema')");
            entity.Property(e => e.UsuarioModificador).HasMaxLength(50);

            // Si tu SQL tiene Activo como int
            entity.Property(e => e.Activo).HasDefaultValueSql("((1))");
        });

        modelBuilder.Entity<OperacionesDocumentale>(entity =>
        {
            // ... Seguramente aquí ya tienes configuraciones antiguas, no las borres.
            // Solo agrega esta nueva relación al final del bloque de OperacionesDocumentale:

            entity.HasOne(d => d.IdAgenciaAduanaNavigation)
                .WithMany(p => p.OperacionesDocumentales)
                .HasForeignKey(d => d.IdAgenciaAduana)
                .HasConstraintName("FK_OpeDoc_Agencias");
        });
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}