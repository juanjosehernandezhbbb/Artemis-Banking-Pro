using ArtemisBankingPro.Core.Application.Cashier.Deposit;
using ArtemisBankingPro.Core.Application.Cashier.PayCreditCard;
using ArtemisBankingPro.Core.Application.Cashier.Withdrawal;
using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Persistance.Context;
using ArtemisBankingPro.WebApp.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.WebApp.Controllers
{
    [Authorize(Roles = "Cajero")]
    public class CashierController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMediator _mediator;
        private readonly IEmailService _emailService;

        public CashierController(
            ApplicationDbContext context,
            IDateTimeProvider dateTimeProvider,
            IMediator mediator,
            IEmailService emailService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _mediator = mediator;
            _emailService = emailService;
        }

        // ==========================================
        // HOME DEL CAJERO (DASHBOARD)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = _dateTimeProvider.UtcNow.Date;

            var transaccionesHoy = await _context.Transacciones
                .Where(t => t.RealizadoPorUserId == cashierId && t.Fecha.Date == today)
                .ToListAsync();

            int depositos = transaccionesHoy.Count(t => t.TipoOperacion == TipoOperacion.Deposito);
            int retiros = transaccionesHoy.Count(t => t.TipoOperacion == TipoOperacion.Retiro);
            int pagosTarjetas = transaccionesHoy.Count(t => t.TipoOperacion == TipoOperacion.PagoTarjetaCredito);
            int pagosPrestamos = transaccionesHoy.Count(t => t.TipoOperacion == TipoOperacion.PagoPrestamo);

            int pagosTotal = pagosTarjetas + pagosPrestamos;
            int totalTransacciones = transaccionesHoy.Count;

            var viewModel = new CashierDashboardViewModel
            {
                TransaccionesHoy = totalTransacciones,
                PagosHoy = pagosTotal,
                DepositosHoy = depositos,
                RetirosHoy = retiros
            };

            return View("~/Views/Cashier/Index.cshtml", viewModel);
        }

        // ==========================================
        // MÓDULO DE DEPÓSITO
        // ==========================================
        [HttpGet]
        public IActionResult Deposit()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(string accountNumber, decimal monto)
        {
            if (monto <= 0)
            {
                ModelState.AddModelError(string.Empty, "El monto a depositar debe ser mayor que cero.");
                return View();
            }

            var cuenta = await _context.CuentasAhorro
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.NumeroCuenta == accountNumber && c.Estado == EstadoCuenta.Activa);

            if (cuenta == null)
            {
                ModelState.AddModelError(string.Empty, "El número de cuenta ingresado no corresponde a una cuenta válida.");
                return View();
            }

            ViewBag.Titular = $"{cuenta.Cliente.Nombre} {cuenta.Cliente.Apellido}";
            ViewBag.NumeroCuenta = cuenta.NumeroCuenta;
            ViewBag.AccountId = cuenta.Id;
            ViewBag.Monto = monto;

            return View("ConfirmDeposit");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteDeposit(int accountId, decimal monto)
        {
            var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var command = new ExecuteDepositCommand(cashierId, accountId, monto);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Deposit));
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // MÓDULO DE RETIRO
        // ==========================================
        [HttpGet]
        public IActionResult Withdrawal()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdrawal(string accountNumber, decimal monto)
        {
            if (monto <= 0)
            {
                ModelState.AddModelError(string.Empty, "El monto a retirar debe ser mayor que cero.");
                return View();
            }

            var cuenta = await _context.CuentasAhorro
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.NumeroCuenta == accountNumber && c.Estado == EstadoCuenta.Activa);

            if (cuenta == null)
            {
                ModelState.AddModelError(string.Empty, "El número de cuenta ingresado no corresponde a una cuenta válida.");
                return View();
            }

            if (cuenta.Balance < monto)
            {
                ModelState.AddModelError(string.Empty, "El monto ingresado excede el saldo disponible de la cuenta.");
                return View();
            }

            ViewBag.Titular = $"{cuenta.Cliente.Nombre} {cuenta.Cliente.Apellido}";
            ViewBag.NumeroCuenta = cuenta.NumeroCuenta;
            ViewBag.AccountId = cuenta.Id;
            ViewBag.Monto = monto;

            return View("ConfirmWithdrawal");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteWithdrawal(int accountId, decimal monto)
        {
            var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var command = new ExecuteWithdrawalCommand(cashierId, accountId, monto);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Withdrawal));
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // MÓDULO DE PAGO A TARJETA DE CRÉDITO
        // ==========================================
        [HttpGet]
        public IActionResult PayCreditCard()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayCreditCard(string sourceAccountNumber, string cardNumber, decimal monto)
        {
            if (monto <= 0)
            {
                ModelState.AddModelError(string.Empty, "El monto a pagar debe ser mayor que cero.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length != 16)
            {
                ModelState.AddModelError(string.Empty, "El número de tarjeta debe contener 16 dígitos.");
                return View();
            }

            var cuenta = await _context.CuentasAhorro
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.NumeroCuenta == sourceAccountNumber && c.Estado == EstadoCuenta.Activa);

            if (cuenta == null)
            {
                ModelState.AddModelError(string.Empty, "El número de cuenta ingresado no corresponde a una cuenta válida.");
                return View();
            }

            var tarjeta = await _context.TarjetasCredito
                .Include(t => t.Cliente)
                .FirstOrDefaultAsync(t => t.NumeroTarjeta == cardNumber && t.Estado == EstadoTarjeta.Activa);

            if (tarjeta == null)
            {
                ModelState.AddModelError(string.Empty, "El número de tarjeta ingresado no corresponde a una tarjeta válida.");
                return View();
            }

            if (tarjeta.MontoAdeudado <= 0)
            {
                ModelState.AddModelError(string.Empty, "La tarjeta seleccionada no tiene deuda pendiente.");
                return View();
            }

            decimal montoEfectivo = monto > tarjeta.MontoAdeudado ? tarjeta.MontoAdeudado : monto;

            if (cuenta.Balance < montoEfectivo)
            {
                ModelState.AddModelError(string.Empty, "El monto ingresado excede el saldo disponible de la cuenta.");
                return View();
            }

            ViewBag.TitularCuenta = $"{cuenta.Cliente.Nombre} {cuenta.Cliente.Apellido}";
            ViewBag.SourceAccountNumber = cuenta.NumeroCuenta;
            ViewBag.TitularTarjeta = $"{tarjeta.Cliente.Nombre} {tarjeta.Cliente.Apellido}";
            ViewBag.CardNumberFull = tarjeta.NumeroTarjeta;
            ViewBag.CardLastFour = tarjeta.NumeroTarjeta.Length >= 4 ? tarjeta.NumeroTarjeta[^4..] : tarjeta.NumeroTarjeta;
            ViewBag.MontoIngresado = monto;
            ViewBag.MontoEfectivo = montoEfectivo;

            return View("ConfirmPayCreditCard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecutePayCreditCard(string sourceAccountNumber, string cardNumber, decimal monto)
        {
            var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var command = new CashierPayCreditCardCommand(cashierId, sourceAccountNumber, cardNumber, monto);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(PayCreditCard));
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // MÓDULO DE PAGO A PRÉSTAMO
        // ==========================================
        [HttpGet]
        public IActionResult PayLoan()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayLoan(string sourceAccountNumber, string loanNumber, decimal monto)
        {
            if (monto <= 0)
            {
                ModelState.AddModelError(string.Empty, "El monto a pagar debe ser mayor que cero.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(loanNumber) || loanNumber.Length != 9)
            {
                ModelState.AddModelError(string.Empty, "El número del préstamo debe contener 9 dígitos.");
                return View();
            }

            var cuenta = await _context.CuentasAhorro
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.NumeroCuenta == sourceAccountNumber && (int)c.Estado == 0);

            if (cuenta == null)
            {
                ModelState.AddModelError(string.Empty, "El número de cuenta ingresado no corresponde a una cuenta válida.");
                return View();
            }

            var prestamo = await _context.Prestamos
                .Include(p => p.Cliente)
                .Include(p => p.Cuotas)
                .FirstOrDefaultAsync(p => p.NumeroPrestamo == loanNumber && (int)p.Estado == 0);

            if (prestamo == null)
            {
                ModelState.AddModelError(string.Empty, "El número de préstamo ingresado no corresponde a un préstamo válido.");
                return View();
            }

            var cuotasPendientes = prestamo.Cuotas
                .Where(c => (int)c.EstadoPago != 2)
                .OrderBy(c => c.FechaVencimiento)
                .ToList();

            if (!cuotasPendientes.Any())
            {
                ModelState.AddModelError(string.Empty, "El préstamo seleccionado no tiene cuotas pendientes de pago.");
                return View();
            }

            decimal deudaTotalReal = cuotasPendientes.Sum(c => c.SaldoPendiente);
            decimal montoEfectivo = monto > deudaTotalReal ? deudaTotalReal : monto;

            if (cuenta.Balance < montoEfectivo)
            {
                ModelState.AddModelError(string.Empty, "El monto ingresado excede el saldo disponible de la cuenta.");
                return View();
            }

            ViewBag.TitularCuenta = $"{cuenta.Cliente.Nombre} {cuenta.Cliente.Apellido}";
            ViewBag.SourceAccountNumber = cuenta.NumeroCuenta;
            ViewBag.TitularPrestamo = $"{prestamo.Cliente.Nombre} {prestamo.Cliente.Apellido}";
            ViewBag.LoanNumber = prestamo.NumeroPrestamo;
            ViewBag.MontoIngresado = monto;
            ViewBag.MontoEfectivo = montoEfectivo;

            return View("ConfirmPayLoan");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecutePayLoan(string sourceAccountNumber, string loanNumber, decimal monto)
        {
            var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var now = _dateTimeProvider.UtcNow;

            var cuentaOrigen = await _context.CuentasAhorro
                .FirstOrDefaultAsync(c => c.NumeroCuenta == sourceAccountNumber && (int)c.Estado == 0);

            var prestamo = await _context.Prestamos
                .Include(p => p.Cuotas)
                .FirstOrDefaultAsync(p => p.NumeroPrestamo == loanNumber && (int)p.Estado == 0);

            if (cuentaOrigen == null || prestamo == null)
            {
                TempData["Error"] = "Ocurrió un error con la cuenta o el préstamo seleccionado.";
                return RedirectToAction(nameof(PayLoan));
            }

            var cuotasPendientes = prestamo.Cuotas
                .Where(c => (int)c.EstadoPago != 2)
                .OrderBy(c => c.FechaVencimiento)
                .ToList();

            decimal deudaTotalReal = cuotasPendientes.Sum(c => c.SaldoPendiente);
            decimal montoEfectivo = monto > deudaTotalReal ? deudaTotalReal : monto;
            string accountLastFour = cuentaOrigen.NumeroCuenta.Length >= 4 ? cuentaOrigen.NumeroCuenta[^4..] : cuentaOrigen.NumeroCuenta;

            if (cuentaOrigen.Balance < montoEfectivo)
            {
                var txRechazada = new Transaccion
                {
                    CuentaAhorroId = cuentaOrigen.Id,
                    Tipo = 0,
                    TipoOperacion = TipoOperacion.PagoPrestamo,
                    Monto = montoEfectivo,
                    Origen = cuentaOrigen.NumeroCuenta,
                    Destino = prestamo.NumeroPrestamo,
                    Estado = EstadoTransaccion.Rechazada,
                    Fecha = now,
                    RealizadoPorUserId = cashierId
                };

                await _context.Transacciones.AddAsync(txRechazada);
                await _context.SaveChangesAsync();

                TempData["Error"] = "No dispone del monto requerido en la cuenta seleccionada.";
                return RedirectToAction(nameof(PayLoan));
            }

            cuentaOrigen.Balance -= montoEfectivo;
            _context.CuentasAhorro.Update(cuentaOrigen);

            decimal montoRestante = montoEfectivo;
            foreach (var cuota in cuotasPendientes)
            {
                if (montoRestante <= 0) break;

                if (montoRestante >= cuota.SaldoPendiente)
                {
                    montoRestante -= cuota.SaldoPendiente;
                    cuota.SaldoPendiente = 0;
                    cuota.EstadoPago = (EstadoCuota)2;
                }
                else
                {
                    cuota.SaldoPendiente -= montoRestante;
                    cuota.EstadoPago = (EstadoCuota)1;
                    montoRestante = 0;
                }
            }

            if (prestamo.Cuotas.All(c => (int)c.EstadoPago == 2))
            {
                prestamo.Estado = (EstadoPrestamo)1;
            }
            _context.Prestamos.Update(prestamo);

            var txAprobada = new Transaccion
            {
                CuentaAhorroId = cuentaOrigen.Id,
                Tipo = 0,
                TipoOperacion = TipoOperacion.PagoPrestamo,
                Monto = montoEfectivo,
                Origen = cuentaOrigen.NumeroCuenta,
                Destino = prestamo.NumeroPrestamo,
                Estado = EstadoTransaccion.Aprobada,
                Fecha = now,
                RealizadoPorUserId = cashierId
            };

            await _context.Transacciones.AddAsync(txAprobada);
            await _context.SaveChangesAsync();

            try
            {
                var prestamoOwner = await _context.Users.FindAsync(prestamo.ClienteId);
                if (prestamoOwner != null && !string.IsNullOrEmpty(prestamoOwner.Email))
                {
                    await _emailService.SendLoanPaymentNotificationEmailAsync(
                        prestamoOwner.Email,
                        prestamoOwner.Nombre,
                        montoEfectivo,
                        prestamo.NumeroPrestamo,
                        accountLastFour,
                        now
                    );
                }

                if (cuentaOrigen.ClienteId != prestamo.ClienteId)
                {
                    var cuentaOwner = await _context.Users.FindAsync(cuentaOrigen.ClienteId);
                    if (cuentaOwner != null && !string.IsNullOrEmpty(cuentaOwner.Email))
                    {
                        await _emailService.SendAccountDebitedForLoanPaymentEmailAsync(
                            cuentaOwner.Email,
                            cuentaOwner.Nombre,
                            montoEfectivo,
                            prestamo.NumeroPrestamo,
                            accountLastFour,
                            now
                        );
                    }
                }
            }
            catch (Exception)
            {
                TempData["Warning"] = "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Pago al préstamo realizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // MÓDULO DE TRANSACCIONES A CUENTAS DE TERCEROS
        // ==========================================
        [HttpGet]
        public IActionResult ThirdPartyTransfer()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThirdPartyTransfer(string sourceAccountNumber, string targetAccountNumber, decimal monto)
        {
            if (monto <= 0)
            {
                ModelState.AddModelError(string.Empty, "El monto de la transacción debe ser mayor que cero.");
                return View();
            }

            if (sourceAccountNumber == targetAccountNumber)
            {
                ModelState.AddModelError(string.Empty, "La cuenta origen y la cuenta destino no pueden ser la misma.");
                return View();
            }

            var cuentaOrigen = await _context.CuentasAhorro
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.NumeroCuenta == sourceAccountNumber && (int)c.Estado == 0); // 0 = Activa

            if (cuentaOrigen == null)
            {
                ModelState.AddModelError(string.Empty, "El número de cuenta origen ingresado no corresponde a una cuenta válida.");
                return View();
            }

            var cuentaDestino = await _context.CuentasAhorro
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.NumeroCuenta == targetAccountNumber && (int)c.Estado == 0); // 0 = Activa

            if (cuentaDestino == null)
            {
                ModelState.AddModelError(string.Empty, "El número de cuenta destino ingresado no corresponde a una cuenta válida.");
                return View();
            }

            if (cuentaOrigen.Balance < monto)
            {
                ModelState.AddModelError(string.Empty, "El monto ingresado excede el saldo disponible de la cuenta.");
                return View();
            }

            // Pasar datos a la pantalla de confirmación
            ViewBag.TitularOrigen = $"{cuentaOrigen.Cliente.Nombre} {cuentaOrigen.Cliente.Apellido}";
            ViewBag.SourceAccountNumber = cuentaOrigen.NumeroCuenta;
            ViewBag.TitularDestino = $"{cuentaDestino.Cliente.Nombre} {cuentaDestino.Cliente.Apellido}";
            ViewBag.TargetAccountNumber = cuentaDestino.NumeroCuenta;
            ViewBag.Monto = monto;

            return View("ConfirmThirdPartyTransfer");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteThirdPartyTransfer(string sourceAccountNumber, string targetAccountNumber, decimal monto)
        {
            var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var now = _dateTimeProvider.UtcNow;

            using var transactionScope = await _context.Database.BeginTransactionAsync();
            try
            {
                var cuentaOrigen = await _context.CuentasAhorro
                    .Include(c => c.Cliente)
                    .FirstOrDefaultAsync(c => c.NumeroCuenta == sourceAccountNumber && (int)c.Estado == 0);

                var cuentaDestino = await _context.CuentasAhorro
                    .Include(c => c.Cliente)
                    .FirstOrDefaultAsync(c => c.NumeroCuenta == targetAccountNumber && (int)c.Estado == 0);

                if (cuentaOrigen == null || cuentaDestino == null || sourceAccountNumber == targetAccountNumber || monto <= 0)
                {
                    TempData["Error"] = "Los datos de la transacción no son válidos.";
                    return RedirectToAction(nameof(ThirdPartyTransfer));
                }

                string sourceLastFour = cuentaOrigen.NumeroCuenta.Length >= 4 ? cuentaOrigen.NumeroCuenta[^4..] : cuentaOrigen.NumeroCuenta;
                string targetLastFour = cuentaDestino.NumeroCuenta.Length >= 4 ? cuentaDestino.NumeroCuenta[^4..] : cuentaDestino.NumeroCuenta;

                if (cuentaOrigen.Balance < monto)
                {
                    var txRechazada = new Transaccion
                    {
                        CuentaAhorroId = cuentaOrigen.Id,
                        Tipo = 0, // Débito
                        TipoOperacion = TipoOperacion.TransaccionBeneficiario,
                        Monto = monto,
                        Origen = cuentaOrigen.NumeroCuenta,
                        Destino = cuentaDestino.NumeroCuenta,
                        Estado = EstadoTransaccion.Rechazada,
                        Fecha = now,
                        RealizadoPorUserId = cashierId
                    };
                    await _context.Transacciones.AddAsync(txRechazada);
                    await _context.SaveChangesAsync();
                    await transactionScope.CommitAsync();

                    TempData["Error"] = "El monto ingresado excede el saldo disponible de la cuenta.";
                    return RedirectToAction(nameof(ThirdPartyTransfer));
                }

                // Actualizar balances
                cuentaOrigen.Balance -= monto;
                cuentaDestino.Balance += monto;

                _context.CuentasAhorro.Update(cuentaOrigen);
                _context.CuentasAhorro.Update(cuentaDestino);

                // Registrar transacciones cruzadas
                var txDebito = new Transaccion
                {
                    CuentaAhorroId = cuentaOrigen.Id,
                    Tipo = 0, // Débito
                    TipoOperacion = TipoOperacion.TransaccionBeneficiario,
                    Monto = monto,
                    Origen = cuentaOrigen.NumeroCuenta,
                    Destino = cuentaDestino.NumeroCuenta,
                    Estado = EstadoTransaccion.Aprobada,
                    Fecha = now,
                    RealizadoPorUserId = cashierId
                };

                var txCredito = new Transaccion
                {
                    CuentaAhorroId = cuentaDestino.Id,
                    Tipo = (TipoTransaccion)1, // Crédito
                    TipoOperacion = TipoOperacion.TransaccionBeneficiario,
                    Monto = monto,
                    Origen = cuentaOrigen.NumeroCuenta,
                    Destino = cuentaDestino.NumeroCuenta,
                    Estado = EstadoTransaccion.Aprobada,
                    Fecha = now,
                    RealizadoPorUserId = cashierId
                };

                await _context.Transacciones.AddAsync(txDebito);
                await _context.Transacciones.AddAsync(txCredito);
                await _context.SaveChangesAsync();
                await transactionScope.CommitAsync();

                // Envío de notificaciones por correo (No bloqueante)
                try
                {
                    if (cuentaOrigen.Cliente != null && !string.IsNullOrEmpty(cuentaOrigen.Cliente.Email))
                    {
                        await _emailService.SendThirdPartyTransferSourceEmailAsync(
                            cuentaOrigen.Cliente.Email,
                            cuentaOrigen.Cliente.Nombre,
                            monto,
                            sourceLastFour,
                            targetLastFour,
                            now
                        );
                    }

                    if (cuentaDestino.Cliente != null && !string.IsNullOrEmpty(cuentaDestino.Cliente.Email))
                    {
                        await _emailService.SendThirdPartyTransferDestinationEmailAsync(
                            cuentaDestino.Cliente.Email,
                            cuentaDestino.Cliente.Nombre,
                            monto,
                            sourceLastFour,
                            targetLastFour,
                            now
                        );
                    }
                }
                catch (Exception)
                {
                    TempData["Warning"] = "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = "Transacción a cuenta de terceros realizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await transactionScope.RollbackAsync();
                TempData["Error"] = "Ocurrió un error al procesar la transacción.";
                return RedirectToAction(nameof(ThirdPartyTransfer));
            }
        }
    }
}