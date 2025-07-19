using Spotify.APIConsumer;
using Spotify.Modelos;
using Spotify.MVC.Interface;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using MimeKit;
using MailKit;

namespace Spotify.MVC.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com"; 
        private readonly int _smptPort = 587; 
        private readonly string _fromEmail = "beathousetucasa@gmail.com"; 
        private readonly string _fromPassword = "miih bbhy lwox drof"; 

        public async Task enviarEmailRecuperacionContraseña(string email)
        {
            try
            {
                Console.WriteLine("Enviando correo de recuperación...");
                var tempPassword = Guid.NewGuid().ToString("N").Substring(0, 5); 
                var mensaje = new MimeMessage();
                mensaje.From.Add(new MailboxAddress("BeatHouse", _fromEmail)); 
                mensaje.To.Add(new MailboxAddress("", email)); 
                mensaje.Subject = "Recuperación de contraseña"; 

                // Cuerpo del mensaje
                mensaje.Body = new TextPart("plain")
                {
                    Text = $"¡Hola!\n\nRecibimos una solicitud para recuperar tu contraseña en **BeatHouse**.\n" +
                           $"No te preocupes, hemos generado una nueva contraseña temporal para ti:\n\n" +
                           $"**{tempPassword}**\n\n" +
                           $"Te recomendamos que la cambies lo antes posible para garantizar la seguridad de tu cuenta.\n\n" +
                           "Si no solicitaste este cambio, por favor ignora este mensaje. Si tienes alguna duda, nuestro equipo " +
                           "de soporte está aquí para ayudarte.\n\n" +
                           "¡Gracias por ser parte de BeatHouse! 🎶"
                };
                using (var cliente = new SmtpClient())
                {
                    await cliente.ConnectAsync(_smtpServer, _smptPort, SecureSocketOptions.StartTls); 
                    await cliente.AuthenticateAsync(_fromEmail, _fromPassword);
                    await cliente.SendAsync(mensaje); 
                    await cliente.DisconnectAsync(true);
                }

                var usuario = CRUD<Usuario>.GetAll().FirstOrDefault(u => u.Email == email); 
                if (usuario != null)
                {
                    usuario.Contraseña = tempPassword;
                    CRUD<Usuario>.Update(usuario.Id, usuario);
                    Console.WriteLine($"contraseña actualizada: {tempPassword}");

                }
                Console.WriteLine("Correo enviado con éxito.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar el correo electrónico: {ex.Message}");
            }
        }
    }
}
