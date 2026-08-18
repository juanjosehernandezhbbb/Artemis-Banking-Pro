using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Common.Models;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Loans.GetLoans
{
    public class GetLoansQueryHandler : IRequestHandler<GetLoansQuery, PagedResult<LoanListItemDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;

        public GetLoansQueryHandler(IUnitOfWork unitOfWork, IIdentityService identityService)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
        }

        public async Task<PagedResult<LoanListItemDto>> Handle(GetLoansQuery request, CancellationToken cancellationToken)
        {
            var prestamoRepo = _unitOfWork.Repository<Prestamo>();
            var cuotaRepo = _unitOfWork.Repository<CuotaAmortizacion>();

            string? clientId = null;
            if (!string.IsNullOrWhiteSpace(request.Identification))
            {
                clientId = await _identityService.GetUserIdByCedulaAsync(request.Identification);
            }

            var status = request.Status?.ToLower() ?? "activos";

            List<Prestamo> loans;
            if (clientId != null)
            {
                loans = (await prestamoRepo.FindAsync(p => p.ClienteId == clientId)).ToList();
            }
            else
            {
                loans = (await prestamoRepo.FindAsync(_ => true)).ToList();
            }

            if (status != "todos")
            {
                var expectedState = status == "completados" ? EstadoPrestamo.Completado : EstadoPrestamo.Activo;
                loans = loans.Where(p => p.Estado == expectedState).ToList();
            }

            if (status == "todos")
            {
                loans = loans.OrderByDescending(p => p.FechaCreacion).ToList();
            }
            else if (!string.IsNullOrWhiteSpace(request.Identification))
            {
                loans = loans
                    .OrderBy(p => p.Estado)
                    .ThenByDescending(p => p.FechaCreacion)
                    .ToList();
            }
            else
            {
                loans = loans.OrderByDescending(p => p.FechaCreacion).ToList();
            }

            var totalRecords = loans.Count;
            var pagedLoans = loans.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

            var loanIds = pagedLoans.Select(p => p.Id).ToList();
            var allCuotas = (await cuotaRepo.FindAsync(c => loanIds.Contains(c.PrestamoId))).ToList();

            var clientNames = new Dictionary<string, string>();
            foreach (var client in pagedLoans.Select(p => p.ClienteId).Distinct())
            {
                var detail = await _identityService.GetUserDetailByIdAsync(client);
                clientNames[client] = detail != null ? $"{detail.FirstName} {detail.LastName}" : "Cliente";
            }

            var items = new List<LoanListItemDto>();
            foreach (var loan in pagedLoans)
            {
                var cuotas = allCuotas.Where(c => c.PrestamoId == loan.Id).ToList();
                var paidInstallments = cuotas.Count(c => c.FechaPago != null);
                var pendingAmount = cuotas.Where(c => c.FechaPago == null).Sum(c => c.ValorCuota);
                var isLate = cuotas.Any(c => c.FechaPago == null && c.Atrasada);

                items.Add(new LoanListItemDto(
                    loan.Id.ToString(),
                    loan.NumeroPrestamo,
                    loan.ClienteId,
                    clientNames.GetValueOrDefault(loan.ClienteId),
                    loan.CapitalAprobado,
                    loan.PlazoMeses,
                    paidInstallments,
                    pendingAmount,
                    loan.TasaInteresAnual,
                    loan.PlazoMeses,
                    loan.Estado.ToString(),
                    isLate ? "En mora" : "Al día",
                    loan.FechaCreacion
                ));
            }

            var totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize);

            return new PagedResult<LoanListItemDto>(request.Page, request.PageSize, totalRecords, totalPages, items);
        }
    }
}