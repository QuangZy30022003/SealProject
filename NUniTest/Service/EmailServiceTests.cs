using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Service.Servicefolder;
using System.Net.Mail;

namespace NUniTest.Service
{
    [TestFixture]
    public class EmailServiceTests
    {
        private Mock<IConfiguration> _configMock;
        private EmailService _service;

        [SetUp]
        public void Setup()
        {
            _configMock = new Mock<IConfiguration>();

            // Setup configuration values
            _configMock.Setup(c => c["Email:SmtpServer"]).Returns("smtp.gmail.com");
            _configMock.Setup(c => c["Email:Port"]).Returns("587");
            _configMock.Setup(c => c["Email:Username"]).Returns("test@gmail.com");
            _configMock.Setup(c => c["Email:Password"]).Returns("password123");
            _configMock.Setup(c => c["Email:From"]).Returns("noreply@seal.com");

            _service = new EmailService(_configMock.Object);
        }

     

        [Test]
        public void SendEmailAsync_WhenEmailIsNull_Throws()
        {
            // Arrange
            string toEmail = null;
            var subject = "Subject";
            var body = "Body";

            // Act & Assert
            Func<Task> act = async () => await _service.SendEmailAsync(toEmail, subject, body);
            act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public void SendEmailAsync_WhenEmailIsEmpty_Throws()
        {
            // Arrange
            var toEmail = "";
            var subject = "Subject";
            var body = "Body";

            // Act & Assert
            Func<Task> act = async () => await _service.SendEmailAsync(toEmail, subject, body);
            act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public void SendEmailAsync_WhenSubjectIsNull_Throws()
        {
            // Arrange
            var toEmail = "test@example.com";
            string subject = null;
            var body = "Body";

            // Act & Assert
            Func<Task> act = async () => await _service.SendEmailAsync(toEmail, subject, body);
            act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public void SendEmailAsync_WhenBodyIsNull_Throws()
        {
            // Arrange
            var toEmail = "test@example.com";
            var subject = "Subject";
            string body = null;

            // Act & Assert
            Func<Task> act = async () => await _service.SendEmailAsync(toEmail, subject, body);
            act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public void SendEmailAsync_WhenSmtpServerNotConfigured_Throws()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Email:SmtpServer"]).Returns((string)null);
            var service = new EmailService(configMock.Object);

            // Act & Assert
            Func<Task> act = async () => await service.SendEmailAsync("test@example.com", "Subject", "Body");
            act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public void SendEmailAsync_WhenPortNotConfigured_Throws()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Email:SmtpServer"]).Returns("smtp.gmail.com");
            configMock.Setup(c => c["Email:Port"]).Returns((string)null);
            var service = new EmailService(configMock.Object);

            // Act & Assert
            Func<Task> act = async () => await service.SendEmailAsync("test@example.com", "Subject", "Body");
            act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public void SendEmailAsync_WhenUsernameNotConfigured_Throws()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Email:SmtpServer"]).Returns("smtp.gmail.com");
            configMock.Setup(c => c["Email:Port"]).Returns("587");
            configMock.Setup(c => c["Email:Username"]).Returns((string)null);
            var service = new EmailService(configMock.Object);

            // Act & Assert
            Func<Task> act = async () => await service.SendEmailAsync("test@example.com", "Subject", "Body");
            act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public void SendEmailAsync_WhenPasswordNotConfigured_Throws()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Email:SmtpServer"]).Returns("smtp.gmail.com");
            configMock.Setup(c => c["Email:Port"]).Returns("587");
            configMock.Setup(c => c["Email:Username"]).Returns("test@gmail.com");
            configMock.Setup(c => c["Email:Password"]).Returns((string)null);
            var service = new EmailService(configMock.Object);

            // Act & Assert
            Func<Task> act = async () => await service.SendEmailAsync("test@example.com", "Subject", "Body");
            act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public void SendEmailAsync_WhenFromEmailNotConfigured_Throws()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Email:SmtpServer"]).Returns("smtp.gmail.com");
            configMock.Setup(c => c["Email:Port"]).Returns("587");
            configMock.Setup(c => c["Email:Username"]).Returns("test@gmail.com");
            configMock.Setup(c => c["Email:Password"]).Returns("password123");
            configMock.Setup(c => c["Email:From"]).Returns((string)null);
            var service = new EmailService(configMock.Object);

            // Act & Assert
            Func<Task> act = async () => await service.SendEmailAsync("test@example.com", "Subject", "Body");
            act.Should().ThrowAsync<Exception>();
        }

      
       
    }
}
