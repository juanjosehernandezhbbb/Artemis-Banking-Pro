using ArtemisBankingPro.Core.Application.Account.Login;
using ArtemisBankingPro.Core.Application.Commerces.CreateCommerce;
using ArtemisBankingPro.Core.Application.CreditCards.CreateCreditCard;
using ArtemisBankingPro.Core.Application.HermesPay.ProcessPayment;
using ArtemisBankingPro.Core.Application.Loans.CreateLoan;
using ArtemisBankingPro.Core.Application.SavingsAccounts.CreateSavingsAccount;
using ArtemisBankingPro.Core.Application.Users.CreateCommerceUser;
using ArtemisBankingPro.Core.Application.Users.CreateUser;
using System.Collections.Generic;
using Xunit;

namespace ArtemisBankingPro.Tests.UnitTests.Validators
{
    public class CreateUserCommandValidatorTests
    {
        private readonly CreateUserCommandValidator _validator = new();

        [Fact]
        public void ComandoValido_Pasa()
        {
            var command = new CreateUserCommand("Juan", "Perez", "00112345678", "juan@test.com", "juanp", "Passw0rd!", "Passw0rd!", "Cliente", 500m);
            var result = _validator.Validate(command);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void IdentificacionConLetras_Falla()
        {
            var command = new CreateUserCommand("Juan", "Perez", "0011A345678", "juan@test.com", "juanp", "Passw0rd!", "Passw0rd!", "Cliente", 0m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ContrasenasQueNoCoinciden_Falla()
        {
            var command = new CreateUserCommand("Juan", "Perez", "00112345678", "juan@test.com", "juanp", "Passw0rd!", "OtraPass!", "Cliente", 0m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("RolInvalido")]
        [InlineData("")]
        public void RolNoPermitido_Falla(string role)
        {
            var command = new CreateUserCommand("Juan", "Perez", "00112345678", "juan@test.com", "juanp", "Passw0rd!", "Passw0rd!", role, 0m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ClienteConMontoInicialNegativo_Falla()
        {
            var command = new CreateUserCommand("Juan", "Perez", "00112345678", "juan@test.com", "juanp", "Passw0rd!", "Passw0rd!", "Cliente", -100m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void RolAdministrador_AceptaSinMonto()
        {
            var command = new CreateUserCommand("Juan", "Perez", "00112345678", "juan@test.com", "juanp", "Passw0rd!", "Passw0rd!", "Administrador", 0m);
            var result = _validator.Validate(command);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void EmailInvalido_Falla()
        {
            var command = new CreateUserCommand("Juan", "Perez", "00112345678", "no-es-un-email", "juanp", "Passw0rd!", "Passw0rd!", "Cliente", 0m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }
    }

    public class CreateLoanCommandValidatorTests
    {
        private readonly CreateLoanCommandValidator _validator = new();

        [Fact]
        public void PlazoPermitido_Pasa()
        {
            var command = new CreateLoanCommand("client-1", 100000m, 12, 12m);
            var result = _validator.Validate(command);
            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(11)]
        [InlineData(13)]
        public void PlazoNoPermitido_Falla(int plazo)
        {
            var command = new CreateLoanCommand("client-1", 100000m, plazo, 12m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void CapitalCero_Falla()
        {
            var command = new CreateLoanCommand("client-1", 0m, 12, 12m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void TasaCero_Falla()
        {
            var command = new CreateLoanCommand("client-1", 100000m, 12, 0m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ClienteVacio_Falla()
        {
            var command = new CreateLoanCommand("", 100000m, 12, 12m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }
    }

    public class CreateCreditCardCommandValidatorTests
    {
        private readonly CreateCreditCardCommandValidator _validator = new();

        [Fact]
        public void ComandoValido_Pasa()
        {
            var command = new CreateCreditCardCommand("client-1", 15000m);
            var result = _validator.Validate(command);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void LimiteCero_Falla()
        {
            var command = new CreateCreditCardCommand("client-1", 0m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ClienteVacio_Falla()
        {
            var command = new CreateCreditCardCommand("", 15000m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }
    }

    public class CreateSavingsAccountCommandValidatorTests
    {
        private readonly CreateSavingsAccountCommandValidator _validator = new();

        [Fact]
        public void ComandoValido_Pasa()
        {
            var command = new CreateSavingsAccountCommand("client-1", 500m);
            var result = _validator.Validate(command);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void BalanceNegativo_Falla()
        {
            var command = new CreateSavingsAccountCommand("client-1", -50m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }
    }

    public class CreateCommerceCommandValidatorTests
    {
        private readonly CreateCommerceCommandValidator _validator = new();

        [Fact]
        public void ComandoValido_Pasa()
        {
            var command = new CreateCommerceCommand("Supermercado", null, "sup@test.com", "8095551234", "130123456");
            var result = _validator.Validate(command);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void NombreVacio_Falla()
        {
            var command = new CreateCommerceCommand("", null, "sup@test.com", "8095551234", "130123456");
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void EmailInvalido_Falla()
        {
            var command = new CreateCommerceCommand("Supermercado", null, "invalido", "8095551234", "130123456");
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void RncVacio_Falla()
        {
            var command = new CreateCommerceCommand("Supermercado", null, "sup@test.com", "8095551234", "");
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }
    }

    public class ProcessPaymentCommandValidatorTests
    {
        private readonly ProcessPaymentCommandValidator _validator = new();

        [Fact]
        public void ComandoValido_Pasa()
        {
            var command = new ProcessPaymentCommand(1, "1234567890123456", "12", "2029", "123", 2500m);
            var result = _validator.Validate(command);
            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("1234")]
        [InlineData("123456789012345")]
        [InlineData("12345678901234567")]
        [InlineData("ABCDEFGHIJKLMNOP")]
        [InlineData("")]
        public void NumeroDeTarjetaInvalido_Falla(string cardNumber)
        {
            var command = new ProcessPaymentCommand(1, cardNumber, "12", "2029", "123", 2500m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("00")]
        [InlineData("13")]
        [InlineData("99")]
        [InlineData("")]
        public void MesDeExpiracionInvalido_Falla(string month)
        {
            var command = new ProcessPaymentCommand(1, "1234567890123456", month, "2029", "123", 2500m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("29")]
        [InlineData("202")]
        [InlineData("20251")]
        [InlineData("")]
        public void AnioDeExpiracionInvalido_Falla(string year)
        {
            var command = new ProcessPaymentCommand(1, "1234567890123456", "12", year, "123", 2500m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("12")]
        [InlineData("1234")]
        [InlineData("ABCD")]
        [InlineData("")]
        public void CvcInvalido_Falla(string cvc)
        {
            var command = new ProcessPaymentCommand(1, "1234567890123456", "12", "2029", cvc, 2500m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void MontoCero_Falla()
        {
            var command = new ProcessPaymentCommand(1, "1234567890123456", "12", "2029", "123", 0m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void CommerceIdCero_Falla()
        {
            var command = new ProcessPaymentCommand(0, "1234567890123456", "12", "2029", "123", 2500m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }
    }

    public class LoginCommandValidatorTests
    {
        private readonly LoginCommandValidator _validator = new();

        [Fact]
        public void ComandoValido_Pasa()
        {
            var command = new LoginCommand("admin", "Password123!", new List<string> { "Administrador" });
            var result = _validator.Validate(command);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void UsuarioVacio_Falla()
        {
            var command = new LoginCommand("", "Password123!", new List<string> { "Administrador" });
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ContrasenaVacia_Falla()
        {
            var command = new LoginCommand("admin", "", new List<string> { "Administrador" });
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }
    }

    public class CreateCommerceUserCommandValidatorTests
    {
        private readonly CreateCommerceUserCommandValidator _validator = new();

        [Fact]
        public void ComandoValido_Pasa()
        {
            var command = new CreateCommerceUserCommand("Maria", "Gomez", "00112345679", "maria@test.com", "mariag", "Passw0rd!", "Passw0rd!", 1, 1000m);
            var result = _validator.Validate(command);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void IdentificacionConLetras_Falla()
        {
            var command = new CreateCommerceUserCommand("Maria", "Gomez", "00112A45679", "maria@test.com", "mariag", "Passw0rd!", "Passw0rd!", 1);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ContrasenasQueNoCoinciden_Falla()
        {
            var command = new CreateCommerceUserCommand("Maria", "Gomez", "00112345679", "maria@test.com", "mariag", "Passw0rd!", "Otra!", 1);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void CommerceIdCero_Falla()
        {
            var command = new CreateCommerceUserCommand("Maria", "Gomez", "00112345679", "maria@test.com", "mariag", "Passw0rd!", "Passw0rd!", 0);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void MontoInicialNegativo_Falla()
        {
            var command = new CreateCommerceUserCommand("Maria", "Gomez", "00112345679", "maria@test.com", "mariag", "Passw0rd!", "Passw0rd!", 1, -5m);
            var result = _validator.Validate(command);
            Assert.False(result.IsValid);
        }
    }
}