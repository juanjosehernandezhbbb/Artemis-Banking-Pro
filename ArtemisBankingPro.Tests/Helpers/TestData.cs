using System.Collections.Generic;
using System.Linq;
using ArtemisBankingPro.Core.Application.Common.Models;
using ArtemisBankingPro.Core.Application.Users.GetUserById;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Tests.Helpers
{
    public static class TestData
    {
        public static UserDetailDto ActiveClient(string id = "client-1", decimal balance = 1000m)
            => new(
                Id: id,
                UserName: "cliente1",
                Identification: "00123456789",
                FirstName: "Juan",
                LastName: "Perez",
                Email: "juan.perez@example.com",
                Role: DomainConstants.RolCliente,
                IsActive: true,
                CreatedAt: new System.DateTime(2025, 1, 1),
                MainAccount: new AccountSummaryDto("100000001", balance, true, "Activa"));

        public static UserDetailDto InactiveClient(string id = "client-1")
            => new(
                Id: id,
                UserName: "cliente1",
                Identification: "00123456789",
                FirstName: "Juan",
                LastName: "Perez",
                Email: "juan.perez@example.com",
                Role: DomainConstants.RolCliente,
                IsActive: false,
                CreatedAt: new System.DateTime(2025, 1, 1),
                MainAccount: null);

        public static CuentaAhorro PrincipalAccount(string clientId = "client-1", decimal balance = 1000m, int id = 1)
            => new()
            {
                Id = id,
                NumeroCuenta = "100000001",
                ClienteId = clientId,
                Balance = balance,
                TipoCuenta = TipoCuenta.Principal,
                Estado = EstadoCuenta.Activa,
                FechaCreacion = new System.DateTime(2025, 1, 1)
            };

        public static List<CuotaAmortizacion> AmortizationCuotas(
            int prestamoId,
            int count = 12,
            decimal valor = 8884.88m,
            bool future = true,
            bool paid = false)
        {
            var start = future ? System.DateTime.UtcNow.AddMonths(1) : System.DateTime.UtcNow.AddMonths(-count);
            return Enumerable.Range(1, count).Select(i => new CuotaAmortizacion
            {
                PrestamoId = prestamoId,
                NumeroCuota = i,
                FechaVencimiento = start.AddMonths(i - 1),
                FechaPago = paid ? System.DateTime.UtcNow.AddDays(-1) : (System.DateTime?)null,
                ValorCuota = valor,
                MontoInteres = 100m,
                MontoCapital = valor - 100m,
                SaldoPendiente = valor * (count - i + 1),
                EstadoPago = paid ? EstadoCuota.Pagada : EstadoCuota.Pendiente,
                Atrasada = false
            }).ToList();
        }
    }
}