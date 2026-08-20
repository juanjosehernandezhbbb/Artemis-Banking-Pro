using ArtemisBankingPro.Core.Application.SavingsAccounts.CancelSavingsAccount;
using ArtemisBankingPro.Core.Application.SavingsAccounts.CreateSavingsAccount;
using ArtemisBankingPro.Core.Application.SavingsAccounts.GetSavingsAccounts;
using ArtemisBankingPro.Core.Application.Users.GetUsers;
using ArtemisBankingPro.Persistance.Context;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArtemisBankingPro.WebApp.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class SavingsAccountController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ApplicationDbContext _context;

        public SavingsAccountController(IMediator mediator, ApplicationDbContext context)
        {
            _mediator = mediator;
            _context = context;
        }

        // GET: /SavingsAccount/Index
        public async Task<IActionResult> Index(int page = 1, string? status = "Activas", string? type = "Todas", string? identification = null)
        {
            var query = new GetSavingsAccountsQuery(Page: page, PageSize: 20, Identification: identification, Status: status, Type: type);
            var result = await _mediator.Send(query);

            ViewBag.CurrentStatus = status ?? "Activas";
            ViewBag.CurrentType = type ?? "Todas";
            ViewBag.CurrentIdentification = identification;

            return View("~/Views/Admin/SavingsAccounts/Index.cshtml", result);
        }

        // GET: /SavingsAccount/SelectClient
        [HttpGet]
        public async Task<IActionResult> SelectClient(string? identification = null)
        {
            var usersResult = await _mediator.Send(new GetUsersQuery(Page: 1, PageSize: 500, Role: "Cliente"));
            var activeClients = usersResult.Data?.Where(u => u.IsActive).ToList() ?? new();

            if (!string.IsNullOrEmpty(identification))
            {
                activeClients = activeClients
                    .Where(c => c.Identification != null && c.Identification.Contains(identification))
                    .ToList();
            }

            // Validar que el cliente tenga una cuenta principal activa (TipoCuenta = Principal / 0, Estado = Activa / 0)
            var activeClientIds = activeClients.Select(c => c.Id).ToList();
            var clientsWithPrimary = await _context.CuentasAhorro
                .Where(c => activeClientIds.Contains(c.ClienteId) && (int)c.TipoCuenta == 0 && (int)c.Estado == 0)
                .Select(c => c.ClienteId)
                .ToListAsync();

            var eligibleClients = activeClients.Where(c => clientsWithPrimary.Contains(c.Id)).ToList();

            // Cálculo de deuda promedio del sistema
            var activeClientsEntities = await _context.Users
                .Include(u => u.Prestamos)
                    .ThenInclude(p => p.Cuotas)
                .Include(u => u.TarjetasCredito)
                .Where(u => activeClientIds.Contains(u.Id))
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

            return View("~/Views/Admin/SavingsAccounts/SelectClient.cshtml", eligibleClients);
        }

        // POST: /SavingsAccount/SelectClient
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

        // GET: /SavingsAccount/Create
        [HttpGet]
        public IActionResult Create(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                TempData["Error"] = "Debe seleccionar un cliente válido.";
                return RedirectToAction(nameof(SelectClient));
            }

            var model = new CreateSavingsAccountCommand(
                ClientId: clientId,
                InitialBalance: 0m,
                PerformedByUserId: string.Empty
            );

            return View("~/Views/Admin/SavingsAccounts/Create.cshtml", model);
        }

        // POST: /SavingsAccount/Create
        [HttpPost]
        public async Task<IActionResult> Create(CreateSavingsAccountCommand command)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            command = command with { PerformedByUserId = currentUserId ?? string.Empty };

            if (!ModelState.IsValid)
                return View("~/Views/Admin/SavingsAccounts/Create.cshtml", command);

            try
            {
                await _mediator.Send(command);
                TempData["Success"] = "Cuenta de ahorro secundaria creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("~/Views/Admin/SavingsAccounts/Create.cshtml", command);
            }
        }

        // GET: /SavingsAccount/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(string accountNumber)
        {
            var account = await _context.CuentasAhorro
                .Include(c => c.Cliente)
                .Include(c => c.Transacciones)
                .FirstOrDefaultAsync(c => c.NumeroCuenta == accountNumber);

            if (account == null)
            {
                TempData["Error"] = "La cuenta de ahorro seleccionada no existe.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Admin/SavingsAccounts/Details.cshtml", account);
        }

        // POST: /SavingsAccount/Cancel
        [HttpPost]
        public async Task<IActionResult> Cancel(string accountNumber)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var command = new CancelSavingsAccountCommand(accountNumber, currentUserId ?? string.Empty);
                await _mediator.Send(command);
                TempData["Success"] = "Cuenta de ahorro secundaria cancelada exitosamente y balance transferido a la principal.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}