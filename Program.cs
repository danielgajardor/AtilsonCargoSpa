using AtilsonCargoSpa.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// 2. Registra el contexto de la base de datos
builder.Services.AddDbContext<AtilsonContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AtilsonContext")));

// Add services to the container.
// --- REGLAS DE ACCESO ESTRICTO (EL CANDADO ATILSON) ---
builder.Services.AddRazorPages(options =>
{
    // 1. Zona Exclusiva Clientes
    options.Conventions.AuthorizeFolder("/PortalCliente", "SoloClientes");

    // 2. Zona Exclusiva Internos (ATILSON) - Bloqueamos a los clientes
    options.Conventions.AuthorizeFolder("/Operaciones", "SoloInternos");
    options.Conventions.AuthorizeFolder("/Finanzas", "SoloInternos");
    options.Conventions.AuthorizeFolder("/Comercial", "SoloInternos");
});

builder.Services.AddAuthorization(options =>
{
    // Define quién es "Cliente"
    options.AddPolicy("SoloClientes", policy =>
        policy.RequireRole("Cliente"));

    // Define quiénes son "Internos" según tu tabla SQL exacta
    options.AddPolicy("SoloInternos", policy =>
        policy.RequireRole("Informático", "Ventas", "Finanzas", "Operaciones"));
});

// Añade el servicio de correos
builder.Services.AddScoped<AtilsonCargoSpa.Services.EmailService>();

// --- CONFIGURACIÓN DE SEGURIDAD ATILSON ---
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login"; // Si no están logueados, los manda aquí
        options.AccessDeniedPath = "/AccesoDenegado"; // Si entran donde no deben
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // La sesión dura 8 horas
    });

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // <-- Agregar esta (Identifica QUIÉN es)

app.UseAuthorization();

app.MapRazorPages();

app.Run();

