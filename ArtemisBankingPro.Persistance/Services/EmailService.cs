using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Persistance.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public EmailService(
            ILogger<EmailService> logger,
            IHostEnvironment environment,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _logger = logger;
            _environment = environment;
            _configuration = configuration;
            _context = context;
        }

        // =======================================================
        // MÉTODO REAL DE ENVÍO DE CORREOS
        // =======================================================
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpHost = _configuration["SmtpSettings:Host"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["SmtpSettings:Port"] ?? "587");
            var smtpUser = _configuration["SmtpSettings:User"];
            var smtpPass = _configuration["SmtpSettings:Password"];

            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
            {
                throw new Exception("¡ERROR!: Las credenciales de SMTP están nulas o vacías. El sistema no está leyendo el appsettings.json.");
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUser, "Artemis Banking Pro"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Correo REAL enviado exitosamente a {Email}", toEmail);
        }

        // =======================================================
        // MÉTODOS DE ENVÍO DE CORREO ESPECÍFICOS
        // =======================================================

        public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == toEmail);
            string userId = user?.Id ?? string.Empty;

            // Lee la URL base de forma dinámica desde el appsettings.json
            var baseUrl = _configuration["AppBaseUrl"] ?? "https://localhost:7179";
            string resetLink = $"{baseUrl}/Account/ResetPassword?userId={userId}&token={System.Net.WebUtility.UrlEncode(token)}";

            string subject = "Restablecer contraseña - Artemis Banking Pro";
            string body = $"Hola <b>{userName}</b>,<br><br>" +
                          $"Has solicitado restablecer tu contraseña en el sistema. Haz clic en el siguiente botón para continuar:<br><br>" +
                          $"<div style='text-align: center; margin: 20px 0;'>" +
                          $"  <a href='{resetLink}' style='background-color: #0d6efd; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Restablecer Contraseña</a>" +
                          $"</div><br>" +
                          $"Si el botón no funciona, copia y pega el siguiente enlace en tu navegador:<br>" +
                          $"<a href='{resetLink}'>{resetLink}</a><br><br>" +
                          $"Si no solicitaste este cambio, puedes ignorar este mensaje.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendActivationEmailAsync(string toEmail, string userName, string token)
        {
            // Lee la URL base de forma dinámica desde el appsettings.json
            var baseUrl = _configuration["AppBaseUrl"] ?? "https://localhost:7179";
            string activationLink = $"{baseUrl}/Account/ConfirmAccount?token={System.Net.WebUtility.UrlEncode(token)}";

            string subject = "Activación de cuenta - Artemis Banking Pro";
            string body = $"Hola <b>{userName}</b>,<br><br>" +
                          $"Su cuenta ha sido creada exitosamente en el sistema. Haga clic en el siguiente botón para activarla:<br><br>" +
                          $"<div style='text-align: center; margin: 20px 0;'>" +
                          $"  <a href='{activationLink}' style='background-color: #198754; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Activar Cuenta</a>" +
                          $"</div><br>" +
                          $"Si el botón no funciona, copia y pega el siguiente enlace en tu navegador:<br>" +
                          $"<a href='{activationLink}'>{activationLink}</a><br><br>" +
                          $"Si no solicitaste esta cuenta, puedes ignorar este mensaje.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendLoanApprovalEmailAsync(string toEmail, string clientName, decimal capitalAmount, string loanNumber, int termInMonths, decimal monthlyInstallment)
        {
            string subject = $"Préstamo Aprobado #{loanNumber}";
            string body = $"Estimado/a <b>{clientName}</b>,<br><br>Su préstamo por un capital de <b>RD$ {capitalAmount:N2}</b> ha sido aprobado exitosamente y desembolsado en su cuenta principal.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendLoanRateChangeEmailAsync(string toEmail, string clientName, string loanNumber, decimal annualInterestRate)
        {
            string subject = $"Actualización de Tasa - Préstamo #{loanNumber}";
            string body = $"Estimado/a <b>{clientName}</b>,<br><br>Le informamos que la tasa de interés anual de su préstamo ha sido actualizada a <b>{annualInterestRate}%</b>.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendCardAssignmentEmailAsync(string toEmail, string clientName, string lastFourDigits)
        {
            string subject = $"Nueva Tarjeta de Crédito Asignada (...{lastFourDigits})";
            string body = $"Estimado/a <b>{clientName}</b>,<br><br>Se le ha asignado y activado una nueva tarjeta de crédito terminada en <b>{lastFourDigits}</b>.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendCardLimitChangeEmailAsync(string toEmail, string clientName, string lastFourDigits, decimal newLimit)
        {
            string subject = $"Modificación de Límite - Tarjeta ...{lastFourDigits}";
            string body = $"Estimado/a <b>{clientName}</b>,<br><br>El límite de su tarjeta de crédito terminada en <b>{lastFourDigits}</b> ha sido modificado a <b>RD$ {newLimit:N2}</b>.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendCardConsumptionEmailAsync(string toEmail, string clientName, decimal amount, string lastFourDigits, string commerceName, DateTime transactionDate)
        {
            string subject = $"Consumo Registrado - Tarjeta ...{lastFourDigits}";
            string body = $"Estimado/a <b>{clientName}</b>,<br><br>Se registró un consumo por <b>RD$ {amount:N2}</b> en el comercio <b>{commerceName}</b> el día {transactionDate:dd/MM/yyyy HH:mm}.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPaymentReceivedEmailAsync(string toEmail, string commerceName, decimal amount, string lastFourDigits, DateTime transactionDate)
        {
            string subject = $"Pago Recibido - Tarjeta ...{lastFourDigits}";
            string body = $"Estimado/a cliente,<br><br>Se ha aplicado un pago por <b>RD$ {amount:N2}</b> a su tarjeta de crédito terminada en <b>{lastFourDigits}</b> exitosamente.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendCashAdvanceEmailAsync(string toEmail, string clientName, decimal amount, decimal interest, decimal totalCharged, string accountLastFour, DateTime transactionDate)
        {
            string subject = $"Avance de efectivo desde la tarjeta [****]";
            string body = $"Hola <b>{clientName}</b>,<br><br>" +
                          $"Se ha realizado un avance de efectivo desde su tarjeta.<br><br>" +
                          $"<b>Monto depositado:</b> RD${amount:N2}<br>" +
                          $"<b>Interés aplicado:</b> RD${interest:N2}<br>" +
                          $"<b>Total cargado a la tarjeta:</b> RD${totalCharged:N2}<br>" +
                          $"<b>Cuenta destino terminada en:</b> {accountLastFour}<br>" +
                          $"<b>Fecha y hora:</b> {transactionDate:dd/MM/yyyy HH:mm:ss}<br><br>" +
                          $"Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendTransferBetweenAccountsEmailAsync(string toEmail, string clientName, decimal amount, string originLastFour, string targetLastFour, DateTime transactionDate)
        {
            string subject = "Transferencia entre cuentas realizada";
            string body = $"Hola <b>{clientName}</b>,<br><br>" +
                          $"Se ha realizado una transferencia entre sus cuentas de ahorro.<br><br>" +
                          $"<b>Cuenta origen terminada en:</b> {originLastFour}<br>" +
                          $"<b>Cuenta destino terminada en:</b> {targetLastFour}<br>" +
                          $"<b>Monto transferido:</b> RD${amount:N2}<br>" +
                          $"<b>Fecha y hora:</b> {transactionDate:dd/MM/yyyy HH:mm:ss}<br><br>" +
                          $"Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendDepositEmailAsync(string toEmail, string clientName, decimal amount, string accountLastFour, DateTime transactionDate)
        {
            string subject = $"Depósito realizado a su cuenta [{accountLastFour}]";
            string body = $"Hola <b>{clientName}</b>,<br><br>" +
                          $"Se ha realizado un depósito a su cuenta terminada en {accountLastFour}.<br><br>" +
                          $"<b>Monto depositado:</b> RD${amount:N2}<br>" +
                          $"<b>Fecha y hora:</b> {transactionDate:dd/MM/yyyy HH:mm:ss}<br><br>" +
                          $"Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendWithdrawalEmailAsync(string toEmail, string clientName, decimal amount, string accountLastFour, DateTime transactionDate)
        {
            string subject = $"Retiro realizado desde su cuenta [{accountLastFour}]";
            string body = $"Hola <b>{clientName}</b>,<br><br>" +
                          $"Se ha realizado un retiro desde su cuenta terminada en {accountLastFour}.<br><br>" +
                          $"<b>Monto retirado:</b> RD${amount:N2}<br>" +
                          $"<b>Fecha y hora:</b> {transactionDate:dd/MM/yyyy HH:mm:ss}<br><br>" +
                          $"Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendCreditCardPaymentEmailAsync(string toEmail, string clientName, decimal amount, string accountLastFour, string cardLastFour, DateTime transactionDate)
        {
            string subject = $"Pago realizado a la tarjeta [{cardLastFour}]";
            string body = $"Hola <b>{clientName}</b>,<br><br>" +
                          $"Se ha realizado un pago a su tarjeta de crédito terminada en {cardLastFour}.<br><br>" +
                          $"<b>Monto pagado:</b> RD${amount:N2}<br>" +
                          $"<b>Cuenta origen terminada en:</b> {accountLastFour}<br>" +
                          $"<b>Fecha y hora:</b> {transactionDate:dd/MM/yyyy HH:mm:ss}<br><br>" +
                          $"Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendAccountDebitedForCardPaymentEmailAsync(string toEmail, string clientName, decimal amount, string accountLastFour, string cardLastFour, DateTime transactionDate)
        {
            string subject = $"Débito en su cuenta [{accountLastFour}] por pago de tarjeta";
            string body = $"Hola <b>{clientName}</b>,<br><br>" +
                          $"Se ha debitado dinero de su cuenta de ahorro terminada en {accountLastFour} para realizar un pago a la tarjeta terminada en {cardLastFour}.<br><br>" +
                          $"<b>Monto debitado:</b> RD${amount:N2}<br>" +
                          $"<b>Fecha y hora:</b> {transactionDate:dd/MM/yyyy HH:mm:ss}<br><br>" +
                          $"Si usted no autorizó esta operación, comuníquese con la entidad bancaria.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendLoanPaymentNotificationEmailAsync(string toEmail, string clientName, decimal amount, string loanNumber, string accountLastFour, DateTime transactionDate)
        {
            string subject = $"Pago realizado al préstamo [{loanNumber}]";
            string body = $"Hola <b>{clientName}</b>,<br><br>" +
                          $"Se ha realizado un pago a su préstamo {loanNumber}.<br><br>" +
                          $"<b>Monto pagado:</b> RD${amount:N2}<br>" +
                          $"<b>Cuenta origen terminada en:</b> {accountLastFour}<br>" +
                          $"<b>Fecha y hora:</b> {transactionDate:dd/MM/yyyy HH:mm:ss}<br><br>" +
                          $"Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendAccountDebitedForLoanPaymentEmailAsync(string toEmail, string clientName, decimal amount, string loanNumber, string accountLastFour, DateTime transactionDate)
        {
            string subject = $"Débito en su cuenta [{accountLastFour}] por pago de préstamo";
            string body = $"Hola <b>{clientName}</b>,<br><br>" +
                          $"Se ha debitado dinero de su cuenta de ahorro terminada en {accountLastFour} para realizar un pago al préstamo {loanNumber}.<br><br>" +
                          $"<b>Monto debitado:</b> RD${amount:N2}<br>" +
                          $"<b>Fecha y hora:</b> {transactionDate:dd/MM/yyyy HH:mm:ss}<br><br>" +
                          $"Si usted no autorizó esta operación, comuníquese con la entidad bancaria.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendThirdPartyTransferSourceEmailAsync(string toEmail, string clientName, decimal amount, string sourceLastFour, string targetLastFour, DateTime transactionDate)
        {
            string subject = $"Transacción realizada a la cuenta [{targetLastFour}]";
            string body = $"Hola <b>{clientName}</b>,<br><br>" +
                          $"Se ha realizado una transferencia de RD${amount:N2} desde su cuenta terminada en {sourceLastFour} hacia la cuenta terminada en {targetLastFour}.<br><br>" +
                          $"<b>Fecha y hora:</b> {transactionDate:dd/MM/yyyy HH:mm:ss}<br><br>" +
                          $"Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendThirdPartyTransferDestinationEmailAsync(string toEmail, string clientName, decimal amount, string sourceLastFour, string targetLastFour, DateTime transactionDate)
        {
            string subject = $"Transacción enviada desde la cuenta [{sourceLastFour}]";
            string body = $"Hola <b>{clientName}</b>,<br><br>" +
                          $"Ha recibido una transferencia por RD${amount:N2} en su cuenta terminada en {targetLastFour} desde la cuenta terminada en {sourceLastFour}.<br><br>" +
                          $"<b>Fecha y hora:</b> {transactionDate:dd/MM/yyyy HH:mm:ss}<br><br>" +
                          $"Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}