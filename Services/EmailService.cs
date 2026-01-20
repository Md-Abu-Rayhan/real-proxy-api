using System.Net;
using System.Net.Mail;

namespace real_proxy_api.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var fromEmail = smtpSettings["FromEmail"] ?? throw new InvalidOperationException("FromEmail not configured");
            var password = smtpSettings["Password"] ?? throw new InvalidOperationException("SMTP Password not configured");
            var host = smtpSettings["Host"] ?? "smtp.gmail.com";
            var port = int.Parse(smtpSettings["Port"] ?? "587");

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = "Password Reset OTP",
                Body = $@"
                    <html>
                    <body>
                        <h2>Password Reset Request</h2>
                        <p>Your OTP code is: <strong>{otpCode}</strong></p>
                        <p>This code will expire in 3 minutes.</p>
                        <p>If you didn't request this, please ignore this email.</p>
                    </body>
                    </html>
                ",
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            using var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(fromEmail, password)
            };

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
