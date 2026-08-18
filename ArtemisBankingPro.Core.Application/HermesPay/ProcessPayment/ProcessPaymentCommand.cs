using ArtemisBankingPro.Core.Application.Common.Attributes;
using MediatR;

namespace ArtemisBankingPro.Core.Application.HermesPay.ProcessPayment
{
    public record ProcessPaymentCommand(
        int CommerceId,
        [property: SensitiveData] string CardNumber,
        string MonthExpirationCard,
        string YearExpirationCard,
        [property: SensitiveData] string Cvc,
        decimal TransactionAmount
    ) : IRequest<ProcessPaymentResult>;

    public record ProcessPaymentResult(bool Approved, string? Message = null);
}