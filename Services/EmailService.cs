using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace AtilsonCargoSpa.Services
{
    public class EmailService
    {
        public async Task EnviarCorreoAsync(string destinatario, string asunto, string mensajeHtml)
        {
            // CONFIGURACIÓN PARA GMAIL / GOOGLE WORKSPACE
            string smtpHost = "smtp.gmail.com";
            int smtpPort = 587; // Puerto TLS de Google

            // Reemplaza esto con tu correo corporativo real
            string correoRemitente = "danielgajardoatil@gmail.com";

            // Reemplaza esto con la Contraseña de Aplicación de 16 letras (SIN ESPACIOS)
            string password = "wqjnhgtnftleiloh";

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(correoRemitente, password);
                client.EnableSsl = true; // Fundamental para Google

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(correoRemitente, "Operaciones Atilson"),
                    Subject = asunto,
                    Body = mensajeHtml,
                    IsBodyHtml = true
                };

                // Añadimos al cliente o proveedor
                mailMessage.To.Add(destinatario);

                // IMPORTANTE: Te recomiendo agregar una copia oculta a ti mismo para que veas si el correo salió
                // mailMessage.Bcc.Add("tu_correo@atilson.com");

                // Enviamos el correo de forma asíncrona
                await client.SendMailAsync(mailMessage);
            }
        }
    }
}