using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AtilsonCargoSpa.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AtilsonContext _context;

        public LoginModel(AtilsonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Correo { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
            // Si el usuario ya inició sesión, redirigirlo a su pantalla correspondiente
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Cliente"))
                {
                    Response.Redirect("/PortalCliente/Index");
                }
                else
                {
                    Response.Redirect("/Operaciones/Index");
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Correo) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Por favor ingrese correo y contraseña.";
                return Page();
            }

            // Buscar en tu tabla real de Usuarios
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == Correo && u.Password == Password && u.Activo == 1);

            if (usuario != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                    new Claim(ClaimTypes.Email, usuario.Correo),
                    new Claim(ClaimTypes.Role, usuario.Rol ?? "Usuario"),
                    new Claim("IdCliente", usuario.IdCliente?.ToString() ?? "0") // 👈 ¡ESTA ES LA LÍNEA MÁGICA QUE FALTABA!
                };

                // --- MAGIA ATILSON: INYECCIÓN DEL ID DEL CLIENTE ---
                // Si el usuario tiene un IdCliente, se lo "tatuamos" en la sesión
                if (usuario.IdCliente.HasValue)
                {
                    claims.Add(new Claim("IdCliente", usuario.IdCliente.Value.ToString()));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                // --- EL DESVIADOR AUTOMÁTICO ---
                if (usuario.Rol == "Cliente")
                {
                    return RedirectToPage("/PortalCliente/Index"); // A su extranet privada
                }
                else
                {
                    return RedirectToPage("/Dashboard"); // A la matriz de Atilson
                }
            }

            ErrorMessage = "Credenciales incorrectas o cuenta inactiva.";
            return Page();
        }
    }
}