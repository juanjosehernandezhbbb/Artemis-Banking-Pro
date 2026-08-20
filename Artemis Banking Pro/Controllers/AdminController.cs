using Artemis_Banking_Pro.Models.ViewModels;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Persistance.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.WebApp.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            // 1. Total de transacciones históricas
            var totalTransactions = await _context.Transacciones.CountAsync();

            // 2. Transacciones del día
            var transactionsToday = await _context.Transacciones
                .CountAsync(t => t.Fecha.Date == today);

            // 3 & 4. Pagos históricos y del día (Pagos a tarjetas o préstamos)
            var totalPayments = await _context.Transacciones
                .CountAsync(t => (t.PrestamoId != null || t.TarjetaCreditoId != null) &&
                                 (t.Tipo.ToString().Contains("Pago") || t.TipoOperacion.ToString().Contains("Pago")));

            var paymentsToday = await _context.Transacciones
                .CountAsync(t => t.Fecha.Date == today &&
                                 (t.PrestamoId != null || t.TarjetaCreditoId != null) &&
                                 (t.Tipo.ToString().Contains("Pago") || t.TipoOperacion.ToString().Contains("Pago")));

            // 5 & 6. Clientes activos e inactivos (Usuarios con rol Cliente)
            var clienteRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Cliente");
            var clienteRoleIds = clienteRole?.Id;

            var clienteUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == clienteRoleIds)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var activeClientsList = await _context.Users
                .Include(u => u.Prestamos)
                    .ThenInclude(p => p.Cuotas)
                .Include(u => u.TarjetasCredito)
                .Where(u => clienteUserIds.Contains(u.Id) && u.Activo)
                .ToListAsync();

            var activeClientsCount = activeClientsList.Count;
            var inactiveClientsCount = await _context.Users
                .CountAsync(u => clienteUserIds.Contains(u.Id) && !u.Activo);

            // 8, 9, 10. Préstamos vigentes, Tarjetas activas, Cuentas de ahorro activas
            var activeLoansCount = await _context.Prestamos
                .CountAsync(p => (int)p.Estado != (int)EstadoPrestamo.Completado);

            var activeCreditCardsCount = await _context.TarjetasCredito
                .CountAsync(tc => (int)tc.Estado != (int)EstadoTarjeta.Cancelada);

            var activeSavingsAccountsCount = await _context.CuentasAhorro
                .CountAsync(ca => (int)ca.Estado != (int)EstadoCuenta.Cancelada);

            // 7. Total de productos financieros (Suma de cuentas activas + préstamos vigentes + tarjetas activas)
            var totalFinancialProducts = activeSavingsAccountsCount + activeLoansCount + activeCreditCardsCount;

            // 11. Monto promedio de deuda por cliente activo
            decimal totalDebtActiveClients = 0m;
            foreach (var client in activeClientsList)
            {
                var loanDebt = client.Prestamos?
                    .Where(p => (int)p.Estado != (int)EstadoPrestamo.Completado)
                    .Sum(p => p.Cuotas?.Where(c => c.EstadoPago != EstadoCuota.Pagada).Sum(c => c.SaldoPendiente) ?? 0m) ?? 0m;

                var ccDebt = client.TarjetasCredito?
                    .Where(tc => (int)tc.Estado != (int)EstadoTarjeta.Cancelada)
                    .Sum(tc => tc.MontoAdeudado) ?? 0m;

                totalDebtActiveClients += (loanDebt + ccDebt);
            }

            decimal averageClientDebt = activeClientsCount > 0 ? totalDebtActiveClients / activeClientsCount : 0m;

            var model = new AdminDashboardViewModel
            {
                TotalTransactionsHistorical = totalTransactions,
                TransactionsToday = transactionsToday,
                TotalPaymentsHistorical = totalPayments,
                PaymentsToday = paymentsToday,
                ActiveClients = activeClientsCount,
                InactiveClients = inactiveClientsCount,
                TotalFinancialProducts = totalFinancialProducts,
                ActiveLoans = activeLoansCount,
                ActiveCreditCards = activeCreditCardsCount,
                ActiveSavingsAccounts = activeSavingsAccountsCount,
                AverageClientDebt = averageClientDebt
            };

            return View(model);
        }
    }
}