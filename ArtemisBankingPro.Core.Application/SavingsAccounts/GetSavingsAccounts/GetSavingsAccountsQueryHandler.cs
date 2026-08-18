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

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.GetSavingsAccounts
{
    public class GetSavingsAccountsQueryHandler : IRequestHandler<GetSavingsAccountsQuery, PagedResult<SavingsAccountListItemDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;

        public GetSavingsAccountsQueryHandler(IUnitOfWork unitOfWork, IIdentityService identityService)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
        }

        public async Task<PagedResult<SavingsAccountListItemDto>> Handle(GetSavingsAccountsQuery request, CancellationToken cancellationToken)
        {
            var cuentaRepo = _unitOfWork.Repository<CuentaAhorro>();

            string? clientId = null;
            if (!string.IsNullOrWhiteSpace(request.Identification))
            {
                clientId = await _identityService.GetUserIdByCedulaAsync(request.Identification);
            }

            var status = request.Status?.ToLower() ?? "activa";
            var type = request.Type?.ToLower() ?? "todas";

            List<CuentaAhorro> cuentas;
            if (clientId != null)
            {
                cuentas = (await cuentaRepo.FindAsync(c => c.ClienteId == clientId)).ToList();
            }
            else
            {
                cuentas = (await cuentaRepo.FindAsync(_ => true)).ToList();
            }

            if (status != "todas")
            {
                var expectedState = status == "cancelada" ? EstadoCuenta.Cancelada : EstadoCuenta.Activa;
                cuentas = cuentas.Where(c => c.Estado == expectedState).ToList();
            }

            if (type != "todas")
            {
                var expectedType = type == "principal" ? TipoCuenta.Principal : TipoCuenta.Secundaria;
                cuentas = cuentas.Where(c => c.TipoCuenta == expectedType).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.Identification) && status == "activa")
            {
                cuentas = cuentas
                    .OrderBy(c => c.Estado)
                    .ThenByDescending(c => c.FechaCreacion)
                    .ToList();
            }
            else
            {
                cuentas = cuentas.OrderByDescending(c => c.FechaCreacion).ToList();
            }

            var totalRecords = cuentas.Count;
            var pagedCuentas = cuentas.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

            var clientNames = new Dictionary<string, string>();
            var clientIdentifications = new Dictionary<string, string>();
            foreach (var client in pagedCuentas.Select(c => c.ClienteId).Distinct())
            {
                var detail = await _identityService.GetUserDetailByIdAsync(client);
                clientNames[client] = detail != null ? $"{detail.FirstName} {detail.LastName}" : "Cliente";
                clientIdentifications[client] = detail?.Identification ?? string.Empty;
            }

            var items = pagedCuentas.Select(c => new SavingsAccountListItemDto(
                c.Id.ToString(),
                c.NumeroCuenta,
                c.ClienteId,
                clientNames.GetValueOrDefault(c.ClienteId),
                clientIdentifications.GetValueOrDefault(c.ClienteId),
                c.Balance,
                c.TipoCuenta.ToString(),
                c.Estado.ToString(),
                c.FechaCreacion
            )).ToList();

            var totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize);

            return new PagedResult<SavingsAccountListItemDto>(request.Page, request.PageSize, totalRecords, totalPages, items);
        }
    }
}