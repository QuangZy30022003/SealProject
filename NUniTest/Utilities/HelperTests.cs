using NUnit.Framework;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NUniTest.Utilities
{
    [TestFixture]
    public class HelperTests
    {
        [Test]
        public void EmailValidation_ValidEmails_ShouldReturnTrue()
        {
            // Arrange
            var validEmails = new[]
            {
                "test@example.com",
                "user.name@domain.co.uk",
                "firstname+lastname@company.org",
                "email@123.123.123.123", // IP address
                "1234567890@example.com",
                "email@example-one.com",
                "_______@example.com",
                "email@example.name"
            };

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

            // Act & Assert
            foreach (var email in validEmails)
            {
                emailRegex.IsMatch(email).Should().BeTrue($"Email {email} should be valid");
            }
        }

        [Test]
        public void EmailValidation_InvalidEmails_ShouldReturnFalse()
        {
            // Arrange
            var invalidEmails = new[]
            {
                "plainaddress",
                "@missingdomain.com",
                "missing-at-sign.net",
                "missing@.com",
                "missing@domain",
                "spaces @domain.com",
                "email@",
                "@domain.com",
                "email..double.dot@domain.com",
                "email@domain..com"
            };

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

            // Act & Assert
            foreach (var email in invalidEmails)
            {
                emailRegex.IsMatch(email).Should().BeFalse($"Email {email} should be invalid");
            }
        }

        [Test]
        public void PasswordStrength_StrongPasswords_ShouldMeetCriteria()
        {
            // Arrange
            var strongPasswords = new[]
            {
                "MyStr0ngP@ssw0rd!",
                "C0mpl3x!P@ssw0rd",
                "S3cur3P@ssw0rd123",
                "Adm1n!P@ssw0rd2024"
            };

            // Password should have at least 8 characters, 1 uppercase, 1 lowercase, 1 digit, 1 special char
            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$");

            // Act & Assert
            foreach (var password in strongPasswords)
            {
                passwordRegex.IsMatch(password).Should().BeTrue($"Password {password} should be strong");
            }
        }

        [Test]
        public void PasswordStrength_WeakPasswords_ShouldNotMeetCriteria()
        {
            // Arrange
            var weakPasswords = new[]
            {
                "password", // no uppercase, no digit, no special char
                "PASSWORD", // no lowercase, no digit, no special char
                "Password", // no digit, no special char
                "Pass123", // no special char, too short
                "12345678", // no letters, no special char
                "P@ss", // too short
                ""
            };

            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$");

            // Act & Assert
            foreach (var password in weakPasswords)
            {
                passwordRegex.IsMatch(password).Should().BeFalse($"Password {password} should be weak");
            }
        }

        [Test]
        public void DateTimeComparison_FutureDates_ShouldBeAfterNow()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var futureDate = now.AddDays(1);
            var farFutureDate = now.AddYears(1);

            // Act & Assert
            futureDate.Should().BeAfter(now);
            farFutureDate.Should().BeAfter(now);
            farFutureDate.Should().BeAfter(futureDate);
        }

        [Test]
        public void DateTimeComparison_PastDates_ShouldBeBeforeNow()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var pastDate = now.AddDays(-1);
            var farPastDate = now.AddYears(-1);

            // Act & Assert
            pastDate.Should().BeBefore(now);
            farPastDate.Should().BeBefore(now);
            farPastDate.Should().BeBefore(pastDate);
        }

        [Test]
        public void StringManipulation_TrimAndFormat_ShouldWorkCorrectly()
        {
            // Arrange
            var input = "  Hello World  ";
            var expected = "Hello World";

            // Act
            var result = input.Trim();

            // Assert
            result.Should().Be(expected);
            result.Length.Should().Be(11);
            result.Should().NotStartWith(" ");
            result.Should().NotEndWith(" ");
        }

        [Test]
        public void StringManipulation_CaseConversion_ShouldWorkCorrectly()
        {
            // Arrange
            var input = "Hello World";

            // Act & Assert
            input.ToUpper().Should().Be("HELLO WORLD");
            input.ToLower().Should().Be("hello world");
            input.ToUpperInvariant().Should().Be("HELLO WORLD");
            input.ToLowerInvariant().Should().Be("hello world");
        }

        [Test]
        public void CollectionOperations_FilterAndSort_ShouldWorkCorrectly()
        {
            // Arrange
            var numbers = new List<int> { 5, 2, 8, 1, 9, 3 };

            // Act
            var evenNumbers = numbers.Where(n => n % 2 == 0).ToList();
            var sortedNumbers = numbers.OrderBy(n => n).ToList();
            var descendingNumbers = numbers.OrderByDescending(n => n).ToList();

            // Assert
            evenNumbers.Should().BeEquivalentTo(new[] { 2, 8 });
            sortedNumbers.Should().BeEquivalentTo(new[] { 1, 2, 3, 5, 8, 9 });
            descendingNumbers.Should().BeEquivalentTo(new[] { 9, 8, 5, 3, 2, 1 });
        }

        [Test]
        public void CollectionOperations_Aggregation_ShouldWorkCorrectly()
        {
            // Arrange
            var numbers = new List<int> { 1, 2, 3, 4, 5 };

            // Act & Assert
            numbers.Sum().Should().Be(15);
            numbers.Average().Should().Be(3.0);
            numbers.Min().Should().Be(1);
            numbers.Max().Should().Be(5);
            numbers.Count().Should().Be(5);
        }

        [Test]
        public void GuidGeneration_ShouldCreateUniqueValues()
        {
            // Arrange & Act
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var guid3 = Guid.NewGuid();

            // Assert
            guid1.Should().NotBe(guid2);
            guid1.Should().NotBe(guid3);
            guid2.Should().NotBe(guid3);
            guid1.Should().NotBe(Guid.Empty);
            guid2.Should().NotBe(Guid.Empty);
            guid3.Should().NotBe(Guid.Empty);
        }

        [Test]
        public void NumberValidation_PositiveNumbers_ShouldBeValid()
        {
            // Arrange
            var positiveNumbers = new[] { 1, 5, 100, 1000 };

            // Act & Assert
            foreach (var number in positiveNumbers)
            {
                number.Should().BePositive();
                (number > 0).Should().BeTrue();
            }
        }

        [Test]
        public void NumberValidation_NegativeNumbers_ShouldBeInvalid()
        {
            // Arrange
            var negativeNumbers = new[] { -1, -5, -100, -1000 };

            // Act & Assert
            foreach (var number in negativeNumbers)
            {
                number.Should().BeNegative();
                (number < 0).Should().BeTrue();
            }
        }

        [Test]
        public void RangeValidation_NumbersInRange_ShouldBeValid()
        {
            // Arrange
            var min = 1;
            var max = 100;
            var validNumbers = new[] { 1, 50, 100, 25, 75 };

            // Act & Assert
            foreach (var number in validNumbers)
            {
                number.Should().BeInRange(min, max);
                (number >= min && number <= max).Should().BeTrue();
            }
        }

        [Test]
        public void RangeValidation_NumbersOutOfRange_ShouldBeInvalid()
        {
            // Arrange
            var min = 1;
            var max = 100;
            var invalidNumbers = new[] { 0, -1, 101, 200, -50 };

            // Act & Assert
            foreach (var number in invalidNumbers)
            {
                number.Should().NotBeInRange(min, max);
                (number < min || number > max).Should().BeTrue();
            }
        }
    }
}