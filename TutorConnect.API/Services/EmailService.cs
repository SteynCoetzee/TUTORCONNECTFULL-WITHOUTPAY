using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TutorConnect.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendResetCodeAsync(string toEmail, string resetCode)
        {
            var settings = _config.GetSection("EmailSettings");
            var senderEmail = settings["SenderEmail"]!;
            var senderName  = settings["SenderName"]!;
            var smtpHost    = settings["SmtpHost"]!;
            var smtpPort    = int.Parse(settings["SmtpPort"]!);
            var password    = settings["Password"]!;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Your TutorConnect Password Reset Code";

            message.Body = new TextPart("html")
            {
                Text = $@"
                <!DOCTYPE html>
                <html>
                <body style='font-family: Arial, sans-serif; background: #f4f4f4; padding: 32px;'>
                  <div style='max-width: 480px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.08);'>
                    <div style='background: #0d9488; padding: 24px 32px;'>
                      <h1 style='color: white; margin: 0; font-size: 22px;'>Smiths Tutoring</h1>
                    </div>
                    <div style='padding: 32px;'>
                      <h2 style='margin: 0 0 8px; color: #111;'>Password Reset</h2>
                      <p style='color: #555; margin: 0 0 24px;'>Use the code below to reset your password. It expires in <strong>15 minutes</strong>.</p>
                      <div style='background: #f0fdfc; border: 2px solid #0d9488; border-radius: 8px; padding: 20px; text-align: center; margin-bottom: 24px;'>
                        <span style='font-size: 36px; font-weight: 700; letter-spacing: 10px; color: #0d9488;'>{resetCode}</span>
                      </div>
                      <p style='color: #888; font-size: 13px; margin: 0;'>If you didn't request this, you can safely ignore this email.</p>
                    </div>
                  </div>
                </body>
                </html>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(senderEmail, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
