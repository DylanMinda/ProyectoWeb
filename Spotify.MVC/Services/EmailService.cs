using Spotify.APIConsumer;
using Spotify.Modelos;
using Spotify.MVC.Interface;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using MimeKit;

namespace Spotify.MVC.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com"; 
        private readonly int _smptPort = 587; 
        private readonly string _fromEmail = "soyeljoni123@gmail.com"; 
        private readonly string _fromPassword = "isst qlfk eetu anzt";

        public async Task enviarEmailBienvenida(string email)
        {
            try
            {
                var mensaje = new MimeMessage(); 
                mensaje.From.Add(new MailboxAddress("Vivelab", _fromEmail)); 
                mensaje.To.Add(new MailboxAddress("", email)); 
                mensaje.Subject = "Te damos la bienvenida, su ingreso a sido exitoso"; 

                mensaje.Body = new TextPart("plain") 
                {
                    Text = $"Hola,\n\n¡Bienvenido! Tu ingreso ha sido exitoso"
                };

                using (var cliente = new SmtpClient())
                {
                    await cliente.ConnectAsync(_smtpServer, _smptPort, SecureSocketOptions.StartTls); 
                    await cliente.AuthenticateAsync(_fromEmail, _fromPassword); 
                    await cliente.SendAsync(mensaje); 
                    await cliente.DisconnectAsync(true); 
                }

            }
            catch (Exception ex)
            {
                // Manejar excepciones de envío de correo electrónico
                Console.WriteLine($"Error al enviar el correo electrónico: {ex.Message}");
            }
        }

        public async Task enviarEmailRecuperacionPassword(string email)
        {
            try
            {
                var tempPassword = Guid.NewGuid().ToString("N").Substring(0, 5); 
                var mensaje = new MimeMessage();
                mensaje.From.Add(new MailboxAddress("Vivelab", _fromEmail)); 
                mensaje.To.Add(new MailboxAddress("", email)); 
                mensaje.Subject = "Recuperación de contraseña"; 

                // Cuerpo del mensaje
                mensaje.Body = new TextPart("plain")
                {
                    Text = $"Hola,\n\nSe ha recibido una solicitud para recuperar tu contraseña. " +
                    $"Toma una contraseña temporal:\n\n{tempPassword}\n\n" +
                    $"Te recomendamos que la cambies lo antes posible."
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar el correo electrónico: {ex.Message}");
            }
        }
    }
}
