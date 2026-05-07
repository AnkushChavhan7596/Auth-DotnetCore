using AuthDotnetCoreJwt.Models;
using AuthDotnetCoreJwt.Models.Dto.Auth;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AuthDotnetCoreJwt.Services
{

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // ✅ Basic validation
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("Recipient email is required");

            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Email subject is required");

            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Email body is required");

            try
            {
                var message = new MimeMessage();

                // From
                message.From.Add(new MailboxAddress(
                    _settings.SenderName,
                    _settings.SenderEmail));

                // To
                message.To.Add(MailboxAddress.Parse(to));

                // Subject
                message.Subject = subject;

                // Body (HTML + fallback text)
                var builder = new BodyBuilder
                {
                    HtmlBody = body,
                    TextBody = "This email requires HTML support to view properly."
                };

                message.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();

                // Optional: better security handling
                smtp.CheckCertificateRevocation = true;

                // Connect
                await smtp.ConnectAsync(
                    _settings.Host,
                    _settings.Port,
                    SecureSocketOptions.StartTls
                );

                // Authenticate
                await smtp.AuthenticateAsync(
                    _settings.Username,
                    _settings.Password
                );

                // Send email
                await smtp.SendAsync(message);

                // Disconnect
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}", to);
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogError(ex, "SMTP command error while sending email to {Email}", to);
                throw new Exception("Failed to send email (SMTP command error)");
            }
            catch (SmtpProtocolException ex)
            {
                _logger.LogError(ex, "SMTP protocol error while sending email to {Email}", to);
                throw new Exception("Failed to send email (SMTP protocol error)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending email to {Email}", to);
                throw;
            }
        }
    }
}