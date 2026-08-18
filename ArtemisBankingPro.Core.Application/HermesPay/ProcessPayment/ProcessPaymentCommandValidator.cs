using FluentValidation;

namespace ArtemisBankingPro.Core.Application.HermesPay.ProcessPayment
{
    public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
    {
        public ProcessPaymentCommandValidator()
        {
            RuleFor(x => x.CommerceId).GreaterThan(0);
            RuleFor(x => x.CardNumber).NotEmpty().WithMessage("El número de tarjeta es requerido.")
                .Matches(@"^\d{16}$").WithMessage("El número de tarjeta debe contener exactamente 16 dígitos.");
            RuleFor(x => x.MonthExpirationCard).NotEmpty().WithMessage("El mes de expiración es requerido.")
                .Matches(@"^(0[1-9]|1[0-2])$").WithMessage("El mes de expiración debe tener un valor válido entre 01 y 12.");
            RuleFor(x => x.YearExpirationCard).NotEmpty().WithMessage("El año de expiración es requerido.")
                .Matches(@"^\d{4}$").WithMessage("El año de expiración debe tener formato YYYY.");
            RuleFor(x => x.Cvc).NotEmpty().WithMessage("El CVC es requerido.")
                .Matches(@"^\d{3}$").WithMessage("El CVC debe contener exactamente 3 dígitos.");
            RuleFor(x => x.TransactionAmount).NotEmpty().WithMessage("El monto de la transacción es requerido.")
                .GreaterThan(0).WithMessage("El monto de la transacción debe ser mayor que cero.");
        }
    }
}