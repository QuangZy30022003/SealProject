    using Microsoft.Extensions.Configuration;
    using Service.Interface;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Mail;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    namespace Service.Servicefolder
    {
        public class EmailService : IEmailService
        {
            private readonly IConfiguration _configuration;

            public EmailService(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            public async Task SendEmailAsync(string toEmail, string subject, string body)
            {
                var smtpHost = _configuration["Email:SmtpServer"];
                var smtpPort = int.Parse(_configuration["Email:Port"]);
                var smtpUser = _configuration["Email:Username"];
                var smtpPass = _configuration["Email:Password"];
                var fromEmail = _configuration["Email:From"];

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, "Seal System"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                }
            }
        }
    }
