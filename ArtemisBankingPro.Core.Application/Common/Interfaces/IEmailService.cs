using System;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Common.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string userName, string token);
        Task SendActivationEmailAsync(string toEmail, string userName, string token);

        Task SendLoanApprovalEmailAsync(string toEmail, string clientName, decimal capitalAmount, string loanNumber, int termInMonths, decimal monthlyInstallment);
        Task SendLoanRateChangeEmailAsync(string toEmail, string clientName, string loanNumber, decimal annualInterestRate);

        Task SendCardAssignmentEmailAsync(string toEmail, string clientName, string lastFourDigits);
        Task SendCardLimitChangeEmailAsync(string toEmail, string clientName, string lastFourDigits, decimal newLimit);
        Task SendCardConsumptionEmailAsync(string toEmail, string clientName, decimal amount, string lastFourDigits, string commerceName, DateTime transactionDate);
        Task SendPaymentReceivedEmailAsync(string toEmail, string commerceName, decimal amount, string lastFourDigits, DateTime transactionDate);

        Task SendEmailAsync(string toEmail, string subject, string body);

        Task SendCashAdvanceEmailAsync(string toEmail, string clientName, decimal amount, decimal interest, decimal totalCharged, string accountLastFour, DateTime transactionDate);

        Task SendTransferBetweenAccountsEmailAsync(string toEmail, string clientName, decimal amount, string originLastFour, string targetLastFour, DateTime transactionDate);

        Task SendDepositEmailAsync(string toEmail, string clientName, decimal amount, string accountLastFour, DateTime transactionDate);

        Task SendWithdrawalEmailAsync(string toEmail, string clientName, decimal amount, string accountLastFour, DateTime transactionDate);

        Task SendCreditCardPaymentEmailAsync(string toEmail, string clientName, decimal amount, string accountLastFour, string cardLastFour, DateTime transactionDate);
        Task SendAccountDebitedForCardPaymentEmailAsync(string toEmail, string clientName, decimal amount, string accountLastFour, string cardLastFour, DateTime transactionDate);

        Task SendLoanPaymentNotificationEmailAsync(string toEmail, string clientName, decimal amount, string loanNumber, string accountLastFour, DateTime transactionDate);
        Task SendAccountDebitedForLoanPaymentEmailAsync(string toEmail, string clientName, decimal amount, string loanNumber, string accountLastFour, DateTime transactionDate);

        Task SendThirdPartyTransferSourceEmailAsync(string toEmail, string clientName, decimal amount, string sourceLastFour, string targetLastFour, DateTime transactionDate);
        Task SendThirdPartyTransferDestinationEmailAsync(string toEmail, string clientName, decimal amount, string sourceLastFour, string targetLastFour, DateTime transactionDate);
    }
}

