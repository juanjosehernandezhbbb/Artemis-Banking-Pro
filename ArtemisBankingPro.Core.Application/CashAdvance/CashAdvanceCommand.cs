using MediatR;

namespace ArtemisBankingPro.Core.Application.CashAdvance
{
    public record CashAdvanceCommand(
        string ClienteId,
        int TarjetaCreditoId,
        int CuentaAhorroId,
        AmountRequest Monto
    ) : IRequest<CashAdvanceResult>;

    public record AmountRequest(decimal Value);

    public record CashAdvanceResult(bool Success, string Message = "");
}