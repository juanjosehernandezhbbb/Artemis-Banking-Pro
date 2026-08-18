using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Loans.GetLoanById
{
    public class GetLoanByIdQueryHandler : IRequestHandler<GetLoanByIdQuery, LoanDetailDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;
        private readonly ILoanCalculatorService _loanCalculator;

        public GetLoanByIdQueryHandler(IUnitOfWork unitOfWork, IIdentityService identityService, ILoanCalculatorService loanCalculator)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
            _loanCalculator = loanCalculator;
        }

        public async Task<LoanDetailDto> Handle(GetLoanByIdQuery request, CancellationToken cancellationToken)
        {
            var prestamo = await _unitOfWork.Repository<Prestamo>().GetByIdAsync(request.Id);
            if (prestamo == null)
                throw new NotFoundException("Préstamo", request.Id);

            var cuotas = (await _unitOfWork.Repository<CuotaAmortizacion>()
                .FindAsync(c => c.PrestamoId == prestamo.Id))
                .OrderBy(c => c.NumeroCuota)
                .ToList();

            var client = await _identityService.GetUserDetailByIdAsync(prestamo.ClienteId);
            var clientFullName = client != null ? $"{client.FirstName} {client.LastName}" : "Cliente";

            var (monthlyInstallment, _) = _loanCalculator.CalculateFrenchAmortization(
                prestamo.CapitalAprobado, prestamo.TasaInteresAnual, prestamo.PlazoMeses);

            var pendingAmount = cuotas.Where(c => c.FechaPago == null).Sum(c => c.ValorCuota);
            var isLate = cuotas.Any(c => c.FechaPago == null && c.Atrasada);

            var amortization = new List<AmortizationItemDto>();
            foreach (var cuota in cuotas)
            {
                amortization.Add(new AmortizationItemDto(
                    cuota.NumeroCuota,
                    cuota.FechaVencimiento,
                    cuota.ValorCuota,
                    cuota.MontoInteres,
                    cuota.MontoCapital,
                    cuota.FechaPago == null ? cuota.ValorCuota : 0m,
                    cuota.EstadoPago.ToString(),
                    cuota.Atrasada
                ));
            }

            return new LoanDetailDto(
                prestamo.Id.ToString(),
                prestamo.NumeroPrestamo,
                prestamo.ClienteId,
                clientFullName,
                prestamo.CapitalAprobado,
                prestamo.TasaInteresAnual,
                prestamo.PlazoMeses,
                monthlyInstallment,
                pendingAmount,
                prestamo.Estado.ToString(),
                isLate ? "En mora" : "Al día",
                prestamo.FechaCreacion,
                amortization
            );
        }
    }
}