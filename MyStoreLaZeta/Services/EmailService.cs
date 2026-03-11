using System.Net;
using System.Net.Mail;

namespace MyStoreLaZeta.Services
{
    public class EmailService
    {
        public void SendEmail(string destino, string token)
        {
            //  TUS DATOS DE GMAIL (Necesitas la clave de aplicación de 16 letras) //hacerlo en app settings y leerlo desde ahí para no exponerlo en el código fuente
            string remitente = "molinajuarezaitana@gmail.com"; // CAMBIA ESTO POR TU GMAIL /admin
            string password = "cbmr lqdy lrfa xdlb"; // CAMBIA ESTO POR TU CLAVE DE 16 LETRAS //admin

            // El link que le llegará al usuario
            string link = $"http://localhost:5219/Account/ResetPassword?token={token}";

            //  correo que se le envia al usuario
            MailMessage correo = new MailMessage();
            correo.From = new MailAddress(remitente);
            correo.To.Add(destino);
            correo.Subject = "Recuperación de Contraseña - LaZeta";
            correo.Body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Recuperar Contraseña</h2>
                    <p>Hola,</p>
                    <p>Has pedido cambiar tu contraseña. Haz clic abajo para crear una nueva:</p>
                    <br>
                    <a href='{link}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Cambiar Contraseña</a>
                    <br><br>
                    <p style='color: gray; font-size: 12px;'>Si no fuiste tú, ignora este mensaje.</p>
                </div>";
            correo.IsBodyHtml = true;

            // ENVIARLO 
            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com";
            smtp.Port = 587;
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(remitente, password);

            smtp.Send(correo);
        }
    }
}