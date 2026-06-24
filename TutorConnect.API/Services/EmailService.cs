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

        public async Task SendBookingConfirmationAsync(
            string toEmail,
            string recipientName,
            string otherPartyName,
            string role,              // "student" or "tutor"
            string sessionDate,
            string sessionTime,
            string moduleCode,
            string meetLink)
        {
            var settings = _config.GetSection("EmailSettings");
            var senderEmail = settings["SenderEmail"]!;
            var senderName  = settings["SenderName"]!;
            var smtpHost    = settings["SmtpHost"]!;
            var smtpPort    = int.Parse(settings["SmtpPort"]!);
            var password    = settings["Password"]!;

            var partyLabel = role == "student" ? "Tutor" : "Student";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = $"Online Session Confirmed — {sessionDate} at {sessionTime}";

            message.Body = new TextPart("html")
            {
                Text = $@"
                <!DOCTYPE html>
                <html>
                <body style='font-family: Arial, sans-serif; background: #f4f4f4; padding: 32px;'>
                  <div style='max-width: 520px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.08);'>
                    <div style='background: #0d9488; padding: 24px 32px;'>
                      <h1 style='color: white; margin: 0; font-size: 22px;'>Smiths Tutoring</h1>
                      <p style='color: rgba(255,255,255,0.85); margin: 6px 0 0; font-size: 14px;'>Online Session Confirmed</p>
                    </div>
                    <div style='padding: 32px;'>
                      <h2 style='margin: 0 0 8px; color: #111;'>Hi {recipientName},</h2>
                      <p style='color: #555; margin: 0 0 24px;'>Your online session has been booked successfully.</p>

                      <div style='background: #f0fdfc; border: 1px solid #0d9488; border-radius: 8px; padding: 20px; margin-bottom: 24px;'>
                        <table style='width:100%; border-collapse:collapse;'>
                          <tr><td style='color:#555; padding:4px 0; font-size:13px;'>Module</td><td style='font-weight:600; color:#111; font-size:13px;'>{moduleCode}</td></tr>
                          <tr><td style='color:#555; padding:4px 0; font-size:13px;'>Date</td><td style='font-weight:600; color:#111; font-size:13px;'>{sessionDate}</td></tr>
                          <tr><td style='color:#555; padding:4px 0; font-size:13px;'>Time</td><td style='font-weight:600; color:#111; font-size:13px;'>{sessionTime}</td></tr>
                          <tr><td style='color:#555; padding:4px 0; font-size:13px;'>{partyLabel}</td><td style='font-weight:600; color:#111; font-size:13px;'>{otherPartyName}</td></tr>
                        </table>
                      </div>

                      {(string.IsNullOrEmpty(meetLink) ? @"
                      <p style='color:#888; font-size:13px; margin:0;'>A Google Meet link will be shared with you closer to the session time.</p>" : $@"
                      <p style='color:#555; margin: 0 0 12px;'>Join your session using the Google Meet link below:</p>
                      <a href='{meetLink}' style='display:inline-block; background:#0d9488; color:white; text-decoration:none; padding:12px 24px; border-radius:8px; font-weight:600; font-size:15px;'>
                        Join Google Meet
                      </a>
                      <p style='color:#aaa; font-size:12px; margin-top:16px;'>Or copy this link: <span style='color:#0d9488;'>{meetLink}</span></p>")}
                    </div>
                    <div style='background:#f9fafb; padding:16px 32px; border-top:1px solid #e5e7eb;'>
                      <p style='color:#aaa; font-size:12px; margin:0;'>This is an automated email from TutorConnect. Do not reply.</p>
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

        public async Task SendInPersonConfirmationAsync(
            string toEmail,
            string recipientName,
            string otherPartyName,
            string role,
            string sessionDate,
            string sessionTime,
            string moduleCode,
            string location,
            string sessionType)
        {
            var settings   = _config.GetSection("EmailSettings");
            var senderEmail = settings["SenderEmail"]!;
            var senderName  = settings["SenderName"]!;
            var smtpHost    = settings["SmtpHost"]!;
            var smtpPort    = int.Parse(settings["SmtpPort"]!);
            var password    = settings["Password"]!;
            var partyLabel  = role == "student" ? "Tutor" : "Student";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = $"In-Person Session Confirmed — {sessionDate} at {sessionTime}";

            message.Body = new TextPart("html")
            {
                Text = $@"<!DOCTYPE html>
<html><body style='font-family:Arial,sans-serif;background:#f4f4f4;padding:32px;'>
<div style='max-width:520px;margin:0 auto;background:white;border-radius:12px;overflow:hidden;box-shadow:0 4px 16px rgba(0,0,0,0.08);'>
  <div style='background:#0d9488;padding:24px 32px;'>
    <h1 style='color:white;margin:0;font-size:22px;'>Smiths Tutoring</h1>
    <p style='color:rgba(255,255,255,0.85);margin:6px 0 0;font-size:14px;'>In-Person Session Confirmed</p>
  </div>
  <div style='padding:32px;'>
    <h2 style='margin:0 0 8px;color:#111;'>Hi {recipientName},</h2>
    <p style='color:#555;margin:0 0 24px;'>Your in-person session has been booked successfully.</p>
    <div style='background:#f0fdfc;border:1px solid #0d9488;border-radius:8px;padding:20px;margin-bottom:24px;'>
      <table style='width:100%;border-collapse:collapse;'>
        <tr><td style='color:#555;padding:4px 0;font-size:13px;'>Module</td><td style='font-weight:600;color:#111;font-size:13px;'>{moduleCode}</td></tr>
        <tr><td style='color:#555;padding:4px 0;font-size:13px;'>Date</td><td style='font-weight:600;color:#111;font-size:13px;'>{sessionDate}</td></tr>
        <tr><td style='color:#555;padding:4px 0;font-size:13px;'>Time</td><td style='font-weight:600;color:#111;font-size:13px;'>{sessionTime}</td></tr>
        <tr><td style='color:#555;padding:4px 0;font-size:13px;'>{partyLabel}</td><td style='font-weight:600;color:#111;font-size:13px;'>{otherPartyName}</td></tr>
        <tr><td style='color:#555;padding:4px 0;font-size:13px;'>Session</td><td style='font-weight:600;color:#111;font-size:13px;'>{sessionType}</td></tr>
        <tr><td style='color:#555;padding:4px 0;font-size:13px;'>Location</td><td style='font-weight:600;color:#0d9488;font-size:13px;'>{location}</td></tr>
      </table>
    </div>
    <p style='color:#555;font-size:13px;margin:0;'>Please arrive on time at the location above. If you need to reschedule, contact your {partyLabel.ToLower()} directly.</p>
  </div>
  <div style='background:#f9fafb;padding:16px 32px;border-top:1px solid #e5e7eb;'>
    <p style='color:#aaa;font-size:12px;margin:0;'>This is an automated email from TutorConnect. Do not reply.</p>
  </div>
</div></body></html>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(senderEmail, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendCancellationEmailAsync(
            string toEmail,
            string recipientName,
            string studentName,
            string sessionDate,
            string sessionTime,
            string moduleCode,
            bool isGroup)
        {
            var settings    = _config.GetSection("EmailSettings");
            var senderEmail  = settings["SenderEmail"]!;
            var senderName   = settings["SenderName"]!;
            var smtpHost     = settings["SmtpHost"]!;
            var smtpPort     = int.Parse(settings["SmtpPort"]!);
            var password     = settings["Password"]!;

            var subject = isGroup
                ? $"Student Cancellation — {sessionDate} at {sessionTime}"
                : $"Session Cancelled — {sessionDate} at {sessionTime}";

            var bodyMessage = isGroup
                ? $"<strong>{studentName}</strong> will no longer be attending the group session."
                : $"Your session on <strong>{sessionDate} at {sessionTime}</strong> has been cancelled by the student.";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = $@"<!DOCTYPE html>
<html><body style='font-family:Arial,sans-serif;background:#f4f4f4;padding:32px;'>
<div style='max-width:520px;margin:0 auto;background:white;border-radius:12px;overflow:hidden;box-shadow:0 4px 16px rgba(0,0,0,0.08);'>
  <div style='background:#ef4444;padding:24px 32px;'>
    <h1 style='color:white;margin:0;font-size:22px;'>Smiths Tutoring</h1>
    <p style='color:rgba(255,255,255,0.85);margin:6px 0 0;font-size:14px;'>Session {(isGroup ? "Update" : "Cancelled")}</p>
  </div>
  <div style='padding:32px;'>
    <h2 style='margin:0 0 8px;color:#111;'>Hi {recipientName},</h2>
    <p style='color:#555;margin:0 0 24px;'>{bodyMessage}</p>
    <div style='background:#fff5f5;border:1px solid #fca5a5;border-radius:8px;padding:20px;margin-bottom:24px;'>
      <table style='width:100%;border-collapse:collapse;'>
        <tr><td style='color:#555;padding:4px 0;font-size:13px;'>Module</td><td style='font-weight:600;color:#111;font-size:13px;'>{moduleCode}</td></tr>
        <tr><td style='color:#555;padding:4px 0;font-size:13px;'>Date</td><td style='font-weight:600;color:#111;font-size:13px;'>{sessionDate}</td></tr>
        <tr><td style='color:#555;padding:4px 0;font-size:13px;'>Time</td><td style='font-weight:600;color:#111;font-size:13px;'>{sessionTime}</td></tr>
        <tr><td style='color:#555;padding:4px 0;font-size:13px;'>Student</td><td style='font-weight:600;color:#111;font-size:13px;'>{studentName}</td></tr>
      </table>
    </div>
  </div>
  <div style='background:#f9fafb;padding:16px 32px;border-top:1px solid #e5e7eb;'>
    <p style='color:#aaa;font-size:12px;margin:0;'>This is an automated email from TutorConnect. Do not reply.</p>
  </div>
</div></body></html>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(senderEmail, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
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
