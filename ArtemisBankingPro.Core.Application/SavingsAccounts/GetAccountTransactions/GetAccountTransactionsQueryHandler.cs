using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Common.Models;
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

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.GetAccountTransactions
{
    public class GetAccountTransactionsQueryHandler : IRequestHandler<GetAccountTransactionsQuery, AccountTransactionsDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;

        public GetAccountTransactionsQueryHandler(IUnitOfWork unitOfWork, IIdentityService identityService)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
        }

        public async Task<AccountTransactionsDto> Handle(GetAccountTransactionsQuery request, CancellationToken cancellationToken)
        {
            var cuentaRepo = _unitOfWork.Repository<CuentaAhorro>();
            var cuentas = await cuentaRepo.FindAsync(c => c.NumeroCuenta == request.AccountNumber);
            var cuenta = cuentas.FirstOrDefault();

            if (cuenta == null)
                throw new NotFoundException("Cuenta de ahorro", request.AccountNumber);

            var client = await _identityService.GetUserDetailByIdAsync(cuenta.ClienteId);
            var clientFullName = client != null ? $"{client.FirstName} {client.LastName}" : "Cliente";

            var transacciones = (await _unitOfWork.Repository<Transaccion>()
                .FindAsync(t => t.CuentaAhorroId == cuenta.Id))
                .OrderByDescending(t => t.Fecha)
                .ToList();

            var totalRecords = transacciones.Count;
            var paged = transacciones.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

            var items = paged.Select(t => new AccountTransactionItemDto(
                t.Id.ToString(),
                t.Fecha,
                t.Monto,
                t.Tipo == TipoTransaccion.Credito ? "CRÉDITO" : "DÉBITO",
                t.Origen,
                t.Destino,
                t.Estado == EstadoTransaccion.Aprobada ? "APROBADA" : "RECHAZADA"
            )).ToList();

            var totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize);
            var transactionsResult = new PagedResult<AccountTransactionItemDto>(
                request.Page, request.PageSize, totalRecords, totalPages, items);

            return new AccountTransactionsDto(
                cuenta.NumeroCuenta,
                clientFullName,
                cuenta.Balance,
                cuenta.TipoCuenta.ToString(),
                cuenta.Estado.ToString(),
                transactionsResult
            );
        }
    }
}