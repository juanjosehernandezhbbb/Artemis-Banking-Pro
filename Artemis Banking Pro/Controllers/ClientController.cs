using ArtemisBankingPro.Core.Application.CashAdvance;
using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.TransferBetweenAccounts;
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
    [Authorize(Roles = "Cliente")]
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IMediator _mediator;

        public ClientController(
            ApplicationDbContext context,
            IEmailService emailService,
            IMediator mediator)
        {
            _context = context;
            _emailService = emailService;
            _mediator = mediator;
        }

        // ==========================================
        // 1. DASHBOARD DEL CLIENTE (Fase 1)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var cuentas = await _context.CuentasAhorro
                .Where(c => c.ClienteId == userId && (int)c.Estado == 0)
                .ToListAsync();

            cuentas = cuentas
                .OrderBy(c => (int)c.TipoCuenta)
                .ThenByDescending(c => c.Balance)
                .ToList();

            var prestamos = await _context.Prestamos
                .Include(p => p.Cuotas)
                .Where(p => p.ClienteId == userId && (int)p.Estado == 0)
                .ToListAsync();

            var tarjetas = await _context.TarjetasCredito
                .Where(t => t.ClienteId == userId && (int)t.Estado == 0)
                .ToListAsync();

            var viewModel = new ClientDashboardViewModel
            {
                CuentasAhorro = cuentas,
                Prestamos = prestamos,
                TarjetasCredito = tarjetas
            };

            return View("~/Views/Client/Index.cshtml", viewModel);
        }

        // ==========================================
        // 2. MÓDULO DE BENEFICIARIOS (Fase 2)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Beneficiaries()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var beneficiarios = await _context.Beneficiarios
                .Include(b => b.CuentaAhorro)
                .ThenInclude(c => c.Cliente)
                .Where(b => b.ClienteId == userId)
                .Select(b => new BeneficiaryListItemViewModel
                {
                    Id = b.Id,
                    Nombre = b.CuentaAhorro.Cliente.Nombre,
                    Apellido = b.CuentaAhorro.Cliente.Apellido,
                    NumeroCuenta = b.CuentaAhorro.NumeroCuenta
                })
                .ToListAsync();

            return View("~/Views/Client/Beneficiaries.cshtml", beneficiarios);
        }

        [HttpGet]
        public IActionResult AddBeneficiary()
        {
            return View("~/Views/Client/AddBeneficiary.cshtml", new AddBeneficiaryViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> AddBeneficiary(AddBeneficiaryViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Client/AddBeneficiary.cshtml", model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cuentaDestino = await _context.CuentasAhorro
                .FirstOrDefaultAsync(c => c.NumeroCuenta == model.NumeroCuenta);

            if (cuentaDestino == null)
            {
                ModelState.AddModelError(string.Empty, "El número de cuenta ingresado no corresponde a una cuenta válida.");
                return View("~/Views/Client/AddBeneficiary.cshtml", model);
            }

            if ((int)cuentaDestino.Estado != 0)
            {
                ModelState.AddModelError(string.Empty, "No puede agregar una cuenta cancelada como beneficiario.");
                return View("~/Views/Client/AddBeneficiary.cshtml", model);
            }

            if (cuentaDestino.ClienteId == userId)
            {
                ModelState.AddModelError(string.Empty, "No puede agregar una cuenta propia como beneficiario. Utilice la opción Transferencia para mover fondos entre sus cuentas.");
                return View("~/Views/Client/AddBeneficiary.cshtml", model);
            }

            var yaRegistrada = await _context.Beneficiarios
                .AnyAsync(b => b.ClienteId == userId && b.CuentaAhorroId == cuentaDestino.Id);

            if (yaRegistrada)
            {
                ModelState.AddModelError(string.Empty, "Esta cuenta ya se encuentra registrada como beneficiario.");
                return View("~/Views/Client/AddBeneficiary.cshtml", model);
            }

            var nuevoBeneficiario = new Beneficiario
            {
                ClienteId = userId ?? string.Empty,
                CuentaAhorroId = cuentaDestino.Id,
                FechaCreacion = DateTime.UtcNow
            };

            await _context.Beneficiarios.AddAsync(nuevoBeneficiario);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Beneficiario agregado correctamente.";
            return RedirectToAction(nameof(Beneficiaries));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBeneficiary(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var beneficiario = await _context.Beneficiarios
                .FirstOrDefaultAsync(b => b.Id == id && b.ClienteId == userId);

            if (beneficiario != null)
            {
                _context.Beneficiarios.Remove(beneficiario);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Beneficiario eliminado correctamente.";
            }

            return RedirectToAction(nameof(Beneficiaries));
        }

        // ==========================================
        // 3. TRANSACCIÓN EXPRESS (Fase 3)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExpressTransaction()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cuentas = await _context.CuentasAhorro
                .Where(c => c.ClienteId == userId && (int)c.Estado == 0)
                .ToListAsync();

            var viewModel = new ExpressTransactionViewModel
            {
                ActiveAccounts = cuentas.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.NumeroCuenta} - Balance: RD$ {c.Balance:N2} ({(c.TipoCuenta == 0 ? "Principal" : "Secundaria")})"
                }).ToList()
            };

            return View("~/Views/Client/ExpressTransaction.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ExpressTransaction(ExpressTransactionViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cuentasCliente = await _context.CuentasAhorro
                .Where(c => c.ClienteId == userId && (int)c.Estado == 0)
                .ToListAsync();

            model.ActiveAccounts = cuentasCliente.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.NumeroCuenta} - Balance: RD$ {c.Balance:N2}"
            }).ToList();

            if (!ModelState.IsValid)
                return View("~/Views/Client/ExpressTransaction.cshtml", model);

            var cuentaOrigen = cuentasCliente.FirstOrDefault(c => c.Id == model.SourceAccountId);
            if (cuentaOrigen == null)
            {
                ModelState.AddModelError(string.Empty, "La cuenta de origen seleccionada no es válida.");
                return View("~/Views/Client/ExpressTransaction.cshtml", model);
            }

            if (cuentaOrigen.Balance < model.Amount)
            {
                ModelState.AddModelError(string.Empty, "El monto ingresado excede el saldo disponible de la cuenta seleccionada.");
                return View("~/Views/Client/ExpressTransaction.cshtml", model);
            }

            var cuentaDestino = await _context.CuentasAhorro
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.NumeroCuenta == model.TargetAccountNumber && (int)c.Estado == 0);

            if (cuentaDestino == null)
            {
                ModelState.AddModelError(string.Empty, "El número de cuenta ingresado no corresponde a una cuenta válida.");
                return View("~/Views/Client/ExpressTransaction.cshtml", model);
            }

            if (cuentaOrigen.Id == cuentaDestino.Id)
            {
                ModelState.AddModelError(string.Empty, "La cuenta destino no puede ser la misma cuenta de origen.");
                return View("~/Views/Client/ExpressTransaction.cshtml", model);
            }

            var confirmationModel = new ExpressConfirmationViewModel
            {
                TargetAccountNumber = cuentaDestino.NumeroCuenta,
                TargetClientName = $"{cuentaDestino.Cliente.Nombre} {cuentaDestino.Cliente.Apellido}",
                Amount = model.Amount,
                SourceAccountId = cuentaOrigen.Id,
                SourceAccountNumber = cuentaOrigen.NumeroCuenta
            };

            return View("~/Views/Client/ExpressConfirmation.cshtml", confirmationModel);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmExpressTransaction(ExpressConfirmationViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cuentaOrigen = await _context.CuentasAhorro
                .FirstOrDefaultAsync(c => c.Id == model.SourceAccountId && c.ClienteId == userId && (int)c.Estado == 0);

            var cuentaDestino = await _context.CuentasAhorro
                .FirstOrDefaultAsync(c => c.NumeroCuenta == model.TargetAccountNumber && (int)c.Estado == 0);

            if (cuentaOrigen == null || cuentaDestino == null || cuentaOrigen.Balance < model.Amount)
            {
                TempData["Error"] = "Ocurrió un error con las cuentas o los fondos disponibles.";
                return RedirectToAction(nameof(Index));
            }

            cuentaOrigen.Balance -= model.Amount;
            cuentaDestino.Balance += model.Amount;

            _context.CuentasAhorro.Update(cuentaOrigen);
            _context.CuentasAhorro.Update(cuentaDestino);

            var txDebito = new Transaccion
            {
                CuentaAhorroId = cuentaOrigen.Id,
                Tipo = 0,
                TipoOperacion = TipoOperacion.TransaccionExpress,
                Monto = model.Amount,
                Origen = cuentaOrigen.NumeroCuenta,
                Destino = cuentaDestino.NumeroCuenta,
                Estado = EstadoTransaccion.Aprobada,
                Fecha = DateTime.UtcNow,
                RealizadoPorUserId = userId
            };

            var txCredito = new Transaccion
            {
                CuentaAhorroId = cuentaDestino.Id,
                Tipo = (TipoTransaccion)1,
                TipoOperacion = TipoOperacion.TransaccionExpress,
                Monto = model.Amount,
                Origen = cuentaOrigen.NumeroCuenta,
                Destino = cuentaDestino.NumeroCuenta,
                Estado = EstadoTransaccion.Aprobada,
                Fecha = DateTime.UtcNow,
                RealizadoPorUserId = userId
            };

            await _context.Transacciones.AddAsync(txDebito);
            await _context.Transacciones.AddAsync(txCredito);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Transacción realizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 4. PAGO A TARJETA DE CRÉDITO
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> PayCreditCard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tarjetas = await _context.TarjetasCredito
                .Where(t => t.ClienteId == userId && (int)t.Estado == 0)
                .ToListAsync();

            var cuentas = await _context.CuentasAhorro
                .Where(c => c.ClienteId == userId && (int)c.Estado == 0)
                .ToListAsync();

            var viewModel = new PayCreditCardViewModel
            {
                ActiveCards = tarjetas.Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = $"**** **** **** {(t.NumeroTarjeta.Length >= 4 ? t.NumeroTarjeta.Substring(t.NumeroTarjeta.Length - 4) : t.NumeroTarjeta)} - Deuda: RD$ {t.MontoAdeudado:N2}"
                }).ToList(),
                ActiveAccounts = cuentas.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.NumeroCuenta} - Balance: RD$ {c.Balance:N2}"
                }).ToList()
            };

            return View("~/Views/Client/PayCreditCard.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> PayCreditCard(PayCreditCardViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tarjetas = await _context.TarjetasCredito
                .Where(t => t.ClienteId == userId && (int)t.Estado == 0)
                .ToListAsync();

            var cuentas = await _context.CuentasAhorro
                .Where(c => c.ClienteId == userId && (int)c.Estado == 0)
                .ToListAsync();

            model.ActiveCards = tarjetas.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = $"**** **** **** {(t.NumeroTarjeta.Length >= 4 ? t.NumeroTarjeta.Substring(t.NumeroTarjeta.Length - 4) : t.NumeroTarjeta)} - Deuda: RD$ {t.MontoAdeudado:N2}"
            }).ToList();

            model.ActiveAccounts = cuentas.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.NumeroCuenta} - Balance: RD$ {c.Balance:N2}"
            }).ToList();

            if (!ModelState.IsValid)
                return View("~/Views/Client/PayCreditCard.cshtml", model);

            var tarjeta = tarjetas.FirstOrDefault(t => t.Id == model.CreditCardId);
            var cuentaOrigen = cuentas.FirstOrDefault(c => c.Id == model.SourceAccountId);

            if (tarjeta == null || cuentaOrigen == null)
            {
                ModelState.AddModelError(string.Empty, "La tarjeta o cuenta seleccionada no es válida.");
                return View("~/Views/Client/PayCreditCard.cshtml", model);
            }

            if (tarjeta.MontoAdeudado <= 0)
            {
                ModelState.AddModelError(string.Empty, "La tarjeta seleccionada no tiene deuda pendiente.");
                return View("~/Views/Client/PayCreditCard.cshtml", model);
            }

            decimal montoEfectivo = model.Amount > tarjeta.MontoAdeudado ? tarjeta.MontoAdeudado : model.Amount;

            if (cuentaOrigen.Balance < montoEfectivo)
            {
                ModelState.AddModelError(string.Empty, "No dispone del monto requerido en la cuenta seleccionada.");
                return View("~/Views/Client/PayCreditCard.cshtml", model);
            }

            cuentaOrigen.Balance -= montoEfectivo;
            tarjeta.MontoAdeudado -= montoEfectivo;

            _context.CuentasAhorro.Update(cuentaOrigen);
            _context.TarjetasCredito.Update(tarjeta);

            string last4Tarjeta = tarjeta.NumeroTarjeta.Length >= 4 ? tarjeta.NumeroTarjeta.Substring(tarjeta.NumeroTarjeta.Length - 4) : tarjeta.NumeroTarjeta;

            var transaccion = new Transaccion
            {
                CuentaAhorroId = cuentaOrigen.Id,
                Tipo = 0,
                TipoOperacion = TipoOperacion.PagoTarjetaCredito,
                Monto = montoEfectivo,
                Origen = cuentaOrigen.NumeroCuenta,
                Destino = $"****{last4Tarjeta}",
                Estado = EstadoTransaccion.Aprobada,
                Fecha = DateTime.UtcNow,
                RealizadoPorUserId = userId
            };

            await _context.Transacciones.AddAsync(transaccion);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Pago a tarjeta de crédito realizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 5. PAGO A PRÉSTAMO
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> PayLoan()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var prestamos = await _context.Prestamos
                .Include(p => p.Cuotas)
                .Where(p => p.ClienteId == userId && (int)p.Estado == 0)
                .ToListAsync();

            var cuentas = await _context.CuentasAhorro
                .Where(c => c.ClienteId == userId && (int)c.Estado == 0)
                .ToListAsync();

            var viewModel = new PayLoanViewModel
            {
                ActiveLoans = prestamos.Select(p => {
                    decimal deudaTotal = p.Cuotas.Where(c => (int)c.EstadoPago != 2).Sum(c => c.SaldoPendiente);
                    return new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = $"Préstamo #{p.NumeroPrestamo} - Deuda Total: RD$ {deudaTotal:N2}"
                    };
                }).ToList(),
                ActiveAccounts = cuentas.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.NumeroCuenta} - Balance: RD$ {c.Balance:N2}"
                }).ToList()
            };

            return View("~/Views/Client/PayLoan.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> PayLoan(PayLoanViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var prestamos = await _context.Prestamos
                .Include(p => p.Cuotas)
                .Where(p => p.ClienteId == userId && (int)p.Estado == 0)
                .ToListAsync();

            var cuentas = await _context.CuentasAhorro
                .Where(c => c.ClienteId == userId && (int)c.Estado == 0)
                .ToListAsync();

            model.ActiveLoans = prestamos.Select(p => {
                decimal deudaTotal = p.Cuotas.Where(c => (int)c.EstadoPago != 2).Sum(c => c.SaldoPendiente);
                return new SelectListItem { Value = p.Id.ToString(), Text = $"Préstamo #{p.NumeroPrestamo} - Deuda Total: RD$ {deudaTotal:N2}" };
            }).ToList();

            model.ActiveAccounts = cuentas.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = $"{c.NumeroCuenta} - Balance: RD$ {c.Balance:N2}" }).ToList();

            if (!ModelState.IsValid)
                return View("~/Views/Client/PayLoan.cshtml", model);

            var prestamo = prestamos.FirstOrDefault(p => p.Id == model.LoanId);
            var cuentaOrigen = cuentas.FirstOrDefault(c => c.Id == model.SourceAccountId);

            if (prestamo == null || cuentaOrigen == null)
            {
                ModelState.AddModelError(string.Empty, "El préstamo o cuenta seleccionada no es válida.");
                return View("~/Views/Client/PayLoan.cshtml", model);
            }

            var cuotasPendientes = prestamo.Cuotas
                .Where(c => (int)c.EstadoPago != 2)
                .OrderBy(c => c.FechaVencimiento)
                .ToList();

            decimal deudaTotalReal = cuotasPendientes.Sum(c => c.SaldoPendiente);

            if (deudaTotalReal <= 0)
            {
                ModelState.AddModelError(string.Empty, "El préstamo seleccionado no tiene cuotas pendientes de pago.");
                return View("~/Views/Client/PayLoan.cshtml", model);
            }

            decimal montoEfectivo = model.Amount > deudaTotalReal ? deudaTotalReal : model.Amount;

            if (cuentaOrigen.Balance < montoEfectivo)
            {
                ModelState.AddModelError(string.Empty, "No dispone del monto requerido en la cuenta seleccionada.");
                return View("~/Views/Client/PayLoan.cshtml", model);
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

            var transaccion = new Transaccion
            {
                CuentaAhorroId = cuentaOrigen.Id,
                Tipo = 0,
                TipoOperacion = TipoOperacion.PagoPrestamo,
                Monto = montoEfectivo,
                Origen = cuentaOrigen.NumeroCuenta,
                Destino = prestamo.NumeroPrestamo,
                Estado = EstadoTransaccion.Aprobada,
                Fecha = DateTime.UtcNow,
                RealizadoPorUserId = userId
            };

            await _context.Transacciones.AddAsync(transaccion);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Pago al préstamo realizado y aplicado a las cuotas correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 6. TRANSFERENCIA A BENEFICIARIOS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> TransferToBeneficiary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var beneficiarios = await _context.Beneficiarios
                .Include(b => b.CuentaAhorro)
                    .ThenInclude(c => c.Cliente)
                .Where(b => b.ClienteId == userId)
                .ToListAsync();

            var cuentas = await _context.CuentasAhorro
                .Where(c => c.ClienteId == userId && (int)c.Estado == 0)
                .ToListAsync();

            var viewModel = new TransferToBeneficiaryViewModel
            {
                HasBeneficiaries = beneficiarios.Any(),
                Beneficiaries = beneficiarios.Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = $"{b.CuentaAhorro.Cliente.Nombre} {b.CuentaAhorro.Cliente.Apellido} - Cuenta: {b.CuentaAhorro.NumeroCuenta}"
                }).ToList(),
                ActiveAccounts = cuentas.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.NumeroCuenta} - Balance: RD$ {c.Balance:N2}"
                }).ToList()
            };

            return View("~/Views/Client/TransferToBeneficiary.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> TransferToBeneficiary(TransferToBeneficiaryViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var beneficiarios = await _context.Beneficiarios
                .Include(b => b.CuentaAhorro)
                    .ThenInclude(c => c.Cliente)
                .Where(b => b.ClienteId == userId)
                .ToListAsync();

            var cuentas = await _context.CuentasAhorro
                .Where(c => c.ClienteId == userId && (int)c.Estado == 0)
                .ToListAsync();

            model.HasBeneficiaries = beneficiarios.Any();
            model.Beneficiaries = beneficiarios.Select(b => new SelectListItem { Value = b.Id.ToString(), Text = $"{b.CuentaAhorro.Cliente.Nombre} {b.CuentaAhorro.Cliente.Apellido} - Cuenta: {b.CuentaAhorro.NumeroCuenta}" }).ToList();
            model.ActiveAccounts = cuentas.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = $"{c.NumeroCuenta} - Balance: RD$ {c.Balance:N2}" }).ToList();

            if (!model.HasBeneficiaries)
            {
                ModelState.AddModelError(string.Empty, "No tiene beneficiarios registrados.");
                return View("~/Views/Client/TransferToBeneficiary.cshtml", model);
            }

            if (!ModelState.IsValid)
                return View("~/Views/Client/TransferToBeneficiary.cshtml", model);

            var beneficiario = beneficiarios.FirstOrDefault(b => b.Id == model.BeneficiaryId);
            var cuentaOrigen = cuentas.FirstOrDefault(c => c.Id == model.SourceAccountId);

            if (beneficiario == null || cuentaOrigen == null)
            {
                ModelState.AddModelError(string.Empty, "El beneficiario o la cuenta de origen seleccionada no es válida.");
                return View("~/Views/Client/TransferToBeneficiary.cshtml", model);
            }

            var cuentaDestino = beneficiario.CuentaAhorro;

            if (cuentaDestino == null || (int)cuentaDestino.Estado != 0)
            {
                ModelState.AddModelError(string.Empty, "La cuenta del beneficiario no se encuentra disponible.");
                return View("~/Views/Client/TransferToBeneficiary.cshtml", model);
            }

            if (cuentaOrigen.Balance < model.Amount)
            {
                var transaccionRechazada = new Transaccion
                {
                    CuentaAhorroId = cuentaOrigen.Id,
                    Tipo = (TipoTransaccion)0,
                    TipoOperacion = TipoOperacion.TransaccionBeneficiario,
                    Monto = model.Amount,
                    Origen = cuentaOrigen.NumeroCuenta,
                    Destino = cuentaDestino.NumeroCuenta,
                    Estado = EstadoTransaccion.Rechazada,
                    Fecha = DateTime.UtcNow,
                    RealizadoPorUserId = userId
                };

                await _context.Transacciones.AddAsync(transaccionRechazada);
                await _context.SaveChangesAsync();

                ModelState.AddModelError(string.Empty, "No dispone de fondos suficientes para realizar esta transacción.");
                return View("~/Views/Client/TransferToBeneficiary.cshtml", model);
            }

            var confirmModel = new ConfirmTransferViewModel
            {
                BeneficiaryId = beneficiario.Id,
                SourceAccountId = cuentaOrigen.Id,
                Amount = model.Amount,
                BeneficiaryName = $"{cuentaDestino.Cliente.Nombre} {cuentaDestino.Cliente.Apellido}",
                BeneficiaryAccountNumber = cuentaDestino.NumeroCuenta,
                SourceAccountNumber = cuentaOrigen.NumeroCuenta
            };

            return View("~/Views/Client/ConfirmTransfer.cshtml", confirmModel);
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteTransfer(ConfirmTransferViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cuentaOrigen = await _context.CuentasAhorro.FirstOrDefaultAsync(c => c.Id == model.SourceAccountId && c.ClienteId == userId);

            var beneficiario = await _context.Beneficiarios
                .Include(b => b.CuentaAhorro)
                    .ThenInclude(c => c.Cliente)
                .FirstOrDefaultAsync(b => b.Id == model.BeneficiaryId && b.ClienteId == userId);

            var cuentaDestino = beneficiario?.CuentaAhorro;

            if (cuentaOrigen == null || cuentaDestino == null || (int)cuentaDestino.Estado != 0 || cuentaOrigen.Balance < model.Amount)
            {
                TempData["Error"] = "Ocurrió un error al procesar la transferencia o los fondos son insuficientes.";
                return RedirectToAction(nameof(Index));
            }

            cuentaOrigen.Balance -= model.Amount;
            cuentaDestino.Balance += model.Amount;

            _context.CuentasAhorro.Update(cuentaOrigen);
            _context.CuentasAhorro.Update(cuentaDestino);

            string last4Dest = cuentaDestino.NumeroCuenta.Length >= 4 ? cuentaDestino.NumeroCuenta.Substring(cuentaDestino.NumeroCuenta.Length - 4) : cuentaDestino.NumeroCuenta;
            string last4Orig = cuentaOrigen.NumeroCuenta.Length >= 4 ? cuentaOrigen.NumeroCuenta.Substring(cuentaOrigen.NumeroCuenta.Length - 4) : cuentaOrigen.NumeroCuenta;

            var txDebito = new Transaccion
            {
                CuentaAhorroId = cuentaOrigen.Id,
                Tipo = (TipoTransaccion)0,
                TipoOperacion = TipoOperacion.TransaccionBeneficiario,
                Monto = model.Amount,
                Origen = cuentaOrigen.NumeroCuenta,
                Destino = cuentaDestino.NumeroCuenta,
                Estado = EstadoTransaccion.Aprobada,
                Fecha = DateTime.UtcNow,
                RealizadoPorUserId = userId
            };

            var txCredito = new Transaccion
            {
                CuentaAhorroId = cuentaDestino.Id,
                Tipo = (TipoTransaccion)1,
                TipoOperacion = TipoOperacion.TransaccionBeneficiario,
                Monto = model.Amount,
                Origen = cuentaOrigen.NumeroCuenta,
                Destino = cuentaDestino.NumeroCuenta,
                Estado = EstadoTransaccion.Aprobada,
                Fecha = DateTime.UtcNow,
                RealizadoPorUserId = userId
            };

            await _context.Transacciones.AddAsync(txDebito);
            await _context.Transacciones.AddAsync(txCredito);
            await _context.SaveChangesAsync();

            try
            {
                var remitenteUser = await _context.Users.FindAsync(userId);
                var receptorUser = await _context.Users.FindAsync(cuentaDestino.ClienteId);
                var now = DateTime.UtcNow;

                if (remitenteUser != null && !string.IsNullOrEmpty(remitenteUser.Email))
                {
                    await _emailService.SendEmailAsync(
                        remitenteUser.Email,
                        $"Transacción realizada a la cuenta ...{last4Dest}",
                        $"Estimado/a {remitenteUser.Nombre}, se ha realizado una transferencia exitosa por RD$ {model.Amount:N2} el {now:dd/MM/yyyy} a las {now:HH:mm:ss} hacia la cuenta terminada en {last4Dest}."
                    );
                }

                if (receptorUser != null && !string.IsNullOrEmpty(receptorUser.Email))
                {
                    await _emailService.SendEmailAsync(
                        receptorUser.Email,
                        $"Transacción enviada desde la cuenta ...{last4Orig}",
                        $"Estimado/a {receptorUser.Nombre}, ha recibido una transferencia por RD$ {model.Amount:N2} el {now:dd/MM/yyyy} a las {now:HH:mm:ss} desde la cuenta terminada en {last4Orig}."
                    );
                }
            }
            catch (Exception)
            {
                TempData["Warning"] = "La transferencia se realizó con éxito, pero hubo un inconveniente al enviar las notificaciones por correo.";
            }

            TempData["Success"] = "Transferencia a beneficiario realizada con éxito.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> BeneficiaryTransaction()
        {
            return await TransferToBeneficiary();
        }

        [HttpPost]
        public async Task<IActionResult> BeneficiaryTransaction(TransferToBeneficiaryViewModel model)
        {
            return await TransferToBeneficiary(model);
        }

        // ==========================================
        // 7. MÓDULO DE AVANCE DE EFECTIVO
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> CashAdvance()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tarjetas = await _context.TarjetasCredito
                .Where(t => t.ClienteId == clientId && t.Estado == EstadoTarjeta.Activa && t.FechaExpiracion > DateTime.UtcNow)
                .ToListAsync();

            var cuentas = await _context.CuentasAhorro
                .Where(c => c.ClienteId == clientId && c.Estado == EstadoCuenta.Activa)
                .ToListAsync();

            ViewBag.Tarjetas = tarjetas;
            ViewBag.Cuentas = cuentas;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CashAdvance(int tarjetaCreditoId, int cuentaAhorroId, decimal monto)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var command = new CashAdvanceCommand(clientId, tarjetaCreditoId, cuentaAhorroId, new AmountRequest(monto));
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(CashAdvance));
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Index", "Client");
        }

        // ==========================================
        // MÓDULO DE TRANSFERENCIA ENTRE CUENTAS PROPIAS
        // ==========================================
        [HttpGet("Client/Transfer")]
        public async Task<IActionResult> TransferBetweenAccounts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cuentas = await _context.CuentasAhorro
                .Where(c => c.ClienteId == userId && c.Estado == EstadoCuenta.Activa)
                .ToListAsync();

            if (cuentas.Count < 2)
            {
                TempData["Error"] = "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.";
                return RedirectToAction("Index", "Client");
            }

            ViewBag.Cuentas = cuentas;
            return View();
        }

        [HttpPost("Client/Transfer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferBetweenAccounts(int cuentaOrigenId, int cuentaDestinoId, decimal monto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (cuentaOrigenId == cuentaDestinoId)
            {
                TempData["Error"] = "La cuenta de origen y la cuenta de destino no pueden ser la misma.";
                return RedirectToAction(nameof(TransferBetweenAccounts));
            }

            if (monto <= 0)
            {
                TempData["Error"] = "El monto a transferir debe ser mayor que cero.";
                return RedirectToAction(nameof(TransferBetweenAccounts));
            }

            var cuentaOrigen = await _context.CuentasAhorro.FirstOrDefaultAsync(c => c.Id == cuentaOrigenId && c.ClienteId == userId);
            var cuentaDestino = await _context.CuentasAhorro.FirstOrDefaultAsync(c => c.Id == cuentaDestinoId && c.ClienteId == userId);

            if (cuentaOrigen == null || cuentaDestino == null)
            {
                TempData["Error"] = "Las cuentas seleccionadas no son válidas.";
                return RedirectToAction(nameof(TransferBetweenAccounts));
            }

            if (cuentaOrigen.Balance < monto)
            {
                TempData["Error"] = "No dispone del monto requerido en la cuenta seleccionada.";
                return RedirectToAction(nameof(TransferBetweenAccounts));
            }

            ViewBag.CuentaOrigenNumero = cuentaOrigen.NumeroCuenta;
            ViewBag.CuentaDestinoNumero = cuentaDestino.NumeroCuenta;
            ViewBag.CuentaOrigenId = cuentaOrigenId;
            ViewBag.CuentaDestinoId = cuentaDestinoId;
            ViewBag.Monto = monto;

            return View("ConfirmTransferBetweenAccounts");
        }

        [HttpPost("Client/ExecuteTransferBetweenAccounts")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteTransferBetweenAccounts(int cuentaOrigenId, int cuentaDestinoId, decimal monto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var command = new TransferBetweenAccountsCommand(userId, cuentaOrigenId, cuentaDestinoId, monto);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(TransferBetweenAccounts));
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Index", "Client");
        }

        // ==========================================
        // DETALLE DE TRANSACCIONES DE CUENTA
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> AccountTransactions(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cuenta = await _context.CuentasAhorro
                .Include(c => c.Transacciones)
                .FirstOrDefaultAsync(c => c.Id == id && c.ClienteId == userId);

            if (cuenta == null)
            {
                TempData["Error"] = "La cuenta seleccionada no existe o no le pertenece.";
                return RedirectToAction(nameof(Index));
            }

            return View(cuenta);
        }

        // ==========================================
        // DETALLE DE CONSUMOS DE TARJETA DE CRÉDITO
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> CreditCardConsumptions(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tarjeta = await _context.TarjetasCredito
                .Include(t => t.Consumos)
                .FirstOrDefaultAsync(t => t.Id == id && t.ClienteId == userId);

            if (tarjeta == null)
            {
                TempData["Error"] = "La tarjeta seleccionada no existe o no le pertenece.";
                return RedirectToAction(nameof(Index));
            }

            return View(tarjeta);
        }

        // ==========================================
        // TABLA DE AMORTIZACIÓN Y DETALLE DE PRÉSTAMO
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> LoanDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var prestamo = await _context.Prestamos
                .Include(p => p.Cuotas)
                .FirstOrDefaultAsync(p => p.Id == id && p.ClienteId == userId);

            if (prestamo == null)
            {
                TempData["Error"] = "El préstamo seleccionado no existe o no le pertenece.";
                return RedirectToAction(nameof(Index));
            }

            return View(prestamo);
        }
    }
}