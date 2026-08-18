using ArtemisBankingPro.Core.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IHostEnvironment _environment;

        public EmailService(ILogger<EmailService> logger, IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public Task SendPasswordResetEmailAsync(string toEmail, string userName, string token)
        {
            if (_environment.IsDevelopment())
                _logger.LogInformation("SIMULACIÓN SMTP (solo dev): correo de recuperación enviado a {Email}", toEmail);
            else
                _logger.LogInformation("SIMULACIÓN SMTP: correo de recuperación enviado a {Email}", toEmail);

            return Task.CompletedTask;
        }

        public Task SendActivationEmailAsync(string toEmail, string userName, string token)
        {
            if (_environment.IsDevelopment())
                _logger.LogInformation("SIMULACIÓN SMTP (solo dev): correo de activación enviado a {Email}", toEmail);
            else
                _logger.LogInformation("SIMULACIÓN SMTP: correo de activación enviado a {Email}", toEmail);

            return Task.CompletedTask;
        }

        public Task SendLoanApprovalEmailAsync(string toEmail, string clientName, decimal capitalAmount, string loanNumber, int termInMonths, decimal monthlyInstallment)
        {
            if (_environment.IsDevelopment())
                _logger.LogInformation("SIMULACIÓN SMTP (solo dev): préstamo aprobado a {Email}. Préstamo {LoanNumber}, monto RD$ {Capital:N2}, plazo {Term} meses, cuota RD$ {Installment:N2}",
                    toEmail, loanNumber, capitalAmount, termInMonths, monthlyInstallment);
            else
                _logger.LogInformation("SIMULACIÓN SMTP: correo de aprobación de préstamo enviado a {Email}", toEmail);

            return Task.CompletedTask;
        }

        public Task SendLoanRateChangeEmailAsync(string toEmail, string clientName, string loanNumber, decimal annualInterestRate)
        {
            if (_environment.IsDevelopment())
                _logger.LogInformation("SIMULACIÓN SMTP (solo dev): tasa del préstamo {LoanNumber} modificada para {Email}. Nueva tasa: {Rate}%", loanNumber, toEmail, annualInterestRate);
            else
                _logger.LogInformation("SIMULACIÓN SMTP: correo de cambio de tasa enviado a {Email}", toEmail);

            return Task.CompletedTask;
        }

        public Task SendCardAssignmentEmailAsync(string toEmail, string clientName, string lastFourDigits)
        {
            if (_environment.IsDevelopment())
                _logger.LogInformation("SIMULACIÓN SMTP (solo dev): tarjeta terminada en {LastFour} asignada a {Email}", lastFourDigits, toEmail);
            else
                _logger.LogInformation("SIMULACIÓN SMTP: correo de asignación de tarjeta enviado a {Email}", toEmail);

            return Task.CompletedTask;
        }

        public Task SendCardLimitChangeEmailAsync(string toEmail, string clientName, string lastFourDigits, decimal newLimit)
        {
            if (_environment.IsDevelopment())
                _logger.LogInformation("SIMULACIÓN SMTP (solo dev): límite de tarjeta {LastFour} modificado para {Email}. Nuevo límite: RD$ {Limit:N2}", lastFourDigits, toEmail, newLimit);
            else
                _logger.LogInformation("SIMULACIÓN SMTP: correo de cambio de límite enviado a {Email}", toEmail);

            return Task.CompletedTask;
        }

        public Task SendCardConsumptionEmailAsync(string toEmail, string clientName, decimal amount, string lastFourDigits, string commerceName, DateTime transactionDate)
        {
            if (_environment.IsDevelopment())
                _logger.LogInformation("SIMULACIÓN SMTP (solo dev): consumo con tarjeta {LastFour} en {Commerce} por RD$ {Amount:N2} a las {Date:HH:mm} - cliente {Email}",
                    lastFourDigits, commerceName, amount, transactionDate, toEmail);
            else
                _logger.LogInformation("SIMULACIÓN SMTP: correo de consumo de tarjeta enviado a {Email}", toEmail);

            return Task.CompletedTask;
        }

        public Task SendPaymentReceivedEmailAsync(string toEmail, string commerceName, decimal amount, string lastFourDigits, DateTime transactionDate)
        {
            if (_environment.IsDevelopment())
                _logger.LogInformation("SIMULACIÓN SMTP (solo dev): pago recibido por {Commerce} con tarjeta {LastFour} por RD$ {Amount:N2} a las {Date:HH:mm} - {Email}",
                    commerceName, lastFourDigits, amount, transactionDate, toEmail);
            else
                _logger.LogInformation("SIMULACIÓN SMTP: correo de pago recibido enviado a {Email}", toEmail);

            return Task.CompletedTask;
        }
    }
}