using ArtemisBankingPro.Core.Application.Loans.CreateLoan;
using ArtemisBankingPro.Core.Application.Loans.GetLoanById;
using ArtemisBankingPro.Core.Application.Loans.GetLoans;
using ArtemisBankingPro.Core.Application.Users.GetUsers; // Namespace corregido
using ArtemisBankingPro.Core.Application.Loans.UpdateLoanRate;
using ArtemisBankingPro.Persistance.Context;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArtemisBankingPro.WebApp.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class LoanController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ApplicationDbContext _context;

        public LoanController(IMediator mediator, ApplicationDbContext context)
        {
            _mediator = mediator;
            _context = context;
        }

        // GET: /Loan/Index
        public async Task<IActionResult> Index(int page = 1, string? status = "Activos", string? identification = null)
        {
            var query = new GetLoansQuery(Page: page, PageSize: 20, Status: status, Identification: identification);
            var result = await _mediator.Send(query);

            ViewBag.CurrentStatus = status ?? "Activos";
            ViewBag.CurrentIdentification = identification;

            return View("~/Views/Admin/Loans/Index.cshtml", result);
        }

        // GET: /Loan/SelectClient
        [HttpGet]
        public async Task<IActionResult> SelectClient(string? identification = null)
        {
            var usersResult = await _mediator.Send(new GetUsersQuery(Page: 1, PageSize: 500, Role: "Cliente"));
            var activeClients = usersResult.Data?.Where(u => u.IsActive).ToList() ?? new();

            var clientsWithActiveLoan = await _context.Prestamos
                .Where(p => (int)p.Estado != 2) // 2 = Completado
                .Select(p => p.ClienteId) // Corregido de ClientId a ClienteId
                .Distinct()
                .ToListAsync();

            var eligibleClients = activeClients
                .Where(c => !clientsWithActiveLoan.Contains(c.Id))
                .ToList();

            if (!string.IsNullOrEmpty(identification))
            {
                eligibleClients = eligibleClients
                    .Where(c => c.Identification != null && c.Identification.Contains(identification))
                    .ToList();
            }

            var allActiveClientIds = activeClients.Select(c => c.Id).ToList();
            var activeClientsEntities = await _context.Users
                .Include(u => u.Prestamos)
                    .ThenInclude(p => p.Cuotas)
                .Include(u => u.TarjetasCredito)
                .Where(u => allActiveClientIds.Contains(u.Id))
                .ToListAsync();

            decimal totalSystemDebt = 0m;
            foreach (var client in activeClientsEntities)
            {
                var loanDebt = client.Prestamos?
                    .Where(p => (int)p.Estado != 2)
                    .Sum(p => p.Cuotas?.Where(cu => (int)cu.EstadoPago != 2).Sum(cu => cu.SaldoPendiente) ?? 0m) ?? 0m;
                var ccDebt = client.TarjetasCredito?
                    .Where(tc => (int)tc.Estado != 2)
                    .Sum(tc => tc.MontoAdeudado) ?? 0m;
                totalSystemDebt += (loanDebt + ccDebt);
            }

            decimal averageDebt = activeClientsEntities.Count > 0 ? totalSystemDebt / activeClientsEntities.Count : 0m;
            ViewBag.AverageSystemDebt = averageDebt;
            ViewBag.SearchIdentification = identification;

            return View("~/Views/Admin/Loans/SelectClient.cshtml", eligibleClients);
        }

        // POST: /Loan/SelectClient
        [HttpPost]
        public IActionResult PostSelectClient(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                TempData["Error"] = "Debe seleccionar un cliente para continuar.";
                return RedirectToAction(nameof(SelectClient));
            }

            return RedirectToAction(nameof(Create), new { clientId });
        }

        // GET: /Loan/Create
        [HttpGet]
        public IActionResult Create(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                TempData["Error"] = "Debe seleccionar un cliente válido.";
                return RedirectToAction(nameof(SelectClient));
            }

            var model = new CreateLoanCommand(
                ClientId: clientId,
                CapitalAmount: 0m,
                TermInMonths: 12,
                AnnualInterestRate: 12.0m,
                ConfirmHighRisk: false,
                PerformedByUserId: string.Empty
            );

            return View("~/Views/Admin/Loans/Create.cshtml", model);
        }

        // POST: /Loan/Create
        [HttpPost]
        public async Task<IActionResult> Create(CreateLoanCommand command, bool confirmHighRisk = false)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            command = command with { PerformedByUserId = currentUserId ?? string.Empty, ConfirmHighRisk = confirmHighRisk };

            if (!ModelState.IsValid)
                return View("~/Views/Admin/Loans/Create.cshtml", command);

            try
            {
                // Validar si estamos en escenario de arranque en frío (sin préstamos ni deudas previas en el sistema)
                bool hasAnyActiveLoans = await _context.Prestamos.AnyAsync(p => (int)p.Estado == 0);
                bool hasAnyCardDebt = await _context.TarjetasCredito.AnyAsync(t => (int)t.Estado == 0 && t.MontoAdeudado > 0);
                bool isColdStart = !hasAnyActiveLoans && !hasAnyCardDebt;

                await _mediator.Send(command);

                // Asignar el mensaje dinámico según corresponda
                if (isColdStart)
                {
                    TempData["Success"] = "Préstamo asignado correctamente y desembolsado. Nota: Se clasificó como de alto riesgo, pero fue aprobado y completado automáticamente por ser el primer préstamo registrado en el sistema.";
                }
                else
                {
                    TempData["Success"] = "Préstamo asignado correctamente y desembolsado en la cuenta principal.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("riesgo") || ex.Message.Contains("alto riesgo"))
            {
                ViewBag.WarningMessage = ex.Message;
                return View("~/Views/Admin/Loans/HighRiskWarning.cshtml", command);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("~/Views/Admin/Loans/Create.cshtml", command);
            }
        }

        // GET: /Loan/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var loan = await _mediator.Send(new GetLoanByIdQuery(id));
            if (loan == null)
            {
                TempData["Error"] = "El préstamo seleccionado no existe.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Admin/Loans/Details.cshtml", loan);
        }

        // GET: /Loan/EditRate/5
        [HttpGet]
        public async Task<IActionResult> EditRate(int id)
        {
            var loan = await _mediator.Send(new GetLoanByIdQuery(id));
            if (loan == null)
            {
                TempData["Error"] = "El préstamo seleccionado no existe.";
                return RedirectToAction(nameof(Index));
            }

            var command = new UpdateLoanRateCommand(int.Parse(loan.Id), loan.AnnualInterestRate);
            ViewBag.LoanNumber = loan.LoanNumber;
            return View("~/Views/Admin/Loans/EditRate.cshtml", command);
        }

        // POST: /Loan/EditRate
        [HttpPost]
        public async Task<IActionResult> EditRate(UpdateLoanRateCommand command)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Admin/Loans/EditRate.cshtml", command);

            try
            {
                await _mediator.Send(command);
                TempData["Success"] = "Tasa de interés actualizada correctamente y cuotas futuras recalculadas.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("~/Views/Admin/Loans/EditRate.cshtml", command);
            }
        }
    }
}