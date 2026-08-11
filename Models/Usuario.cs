using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Rol { get; set; } = null!;



    public byte? Activo { get; set; }

    // NUEVO CAMPO: Conecta al usuario con su empresa (Cliente)
    public int? IdCliente { get; set; }
}