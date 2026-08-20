using ArtemisBankingPro.Core.Application.CreditCards.CancelCreditCard;
using ArtemisBankingPro.Core.Application.CreditCards.CreateCreditCard;
using ArtemisBankingPro.Core.Application.CreditCards.GetCreditCardById;
using ArtemisBankingPro.Core.Application.CreditCards.GetCreditCards;
using ArtemisBankingPro.Core.Application.CreditCards.UpdateCreditCardLimit;
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
    public class CreditCardController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ApplicationDbContext _context;

        public CreditCardController(IMediator mediator, ApplicationDbContext context)
        {
            _mediator = mediator;
            _context = context;
        }

        // GET: /CreditCard/Index
        public async Task<IActionResult> Index(int page = 1, string? status = "Activas", string? identification = null)
        {
            var query = new GetCreditCardsQuery(Page: page, PageSize: 20, Status: status, Identification: identification);
            var result = await _mediator.Send(query);

            ViewBag.CurrentStatus = status ?? "Activas";
            ViewBag.CurrentIdentification = identification;

            return View("~/Views/Admin/CreditCards/Index.cshtml", result);
        }

        // GET: /CreditCard/SelectClient
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

            return View("~/Views/Admin/CreditCards/SelectClient.cshtml", activeClients);
        }

        // POST: /CreditCard/SelectClient
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

        // GET: /CreditCard/Create
        [HttpGet]
        public IActionResult Create(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                TempData["Error"] = "Debe seleccionar un cliente válido.";
                return RedirectToAction(nameof(SelectClient));
            }

            var model = new CreateCreditCardCommand(
                ClientId: clientId,
                CreditLimit: 0m,
                PerformedByUserId: string.Empty
            );

            return View("~/Views/Admin/CreditCards/Create.cshtml", model);
        }

        // POST: /CreditCard/Create
        [HttpPost]
        public async Task<IActionResult> Create(CreateCreditCardCommand command)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            command = command with { PerformedByUserId = currentUserId ?? string.Empty };

            if (!ModelState.IsValid)
                return View("~/Views/Admin/CreditCards/Create.cshtml", command);

            try
            {
                await _mediator.Send(command);
                TempData["Success"] = "Tarjeta de crédito asignada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("~/Views/Admin/CreditCards/Create.cshtml", command);
            }
        }

        // GET: /CreditCard/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var card = await _mediator.Send(new GetCreditCardByIdQuery(id));
            if (card == null)
            {
                TempData["Error"] = "La tarjeta de crédito seleccionada no existe.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Admin/CreditCards/Details.cshtml", card);
        }

        // GET: /CreditCard/EditLimit/5
        [HttpGet]
        public async Task<IActionResult> EditLimit(int id)
        {
            var card = await _mediator.Send(new GetCreditCardByIdQuery(id));
            if (card == null)
            {
                TempData["Error"] = "La tarjeta de crédito seleccionada no existe.";
                return RedirectToAction(nameof(Index));
            }

            var command = new UpdateCreditCardLimitCommand(int.Parse(card.Id), card.CreditLimit);
            ViewBag.CardNumberMasked = card.MaskedCardNumber;
            return View("~/Views/Admin/CreditCards/EditLimit.cshtml", command);
        }

        // POST: /CreditCard/EditLimit
        [HttpPost]
        public async Task<IActionResult> EditLimit(UpdateCreditCardLimitCommand command)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Admin/CreditCards/EditLimit.cshtml", command);

            try
            {
                await _mediator.Send(command);
                TempData["Success"] = "Límite de crédito actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("~/Views/Admin/CreditCards/EditLimit.cshtml", command);
            }
        }

        // POST: /CreditCard/Cancel
        [HttpPost]
        public async Task<IActionResult> Cancel(string id)
        {
            try
            {
                if (int.TryParse(id, out int cardId))
                {
                    await _mediator.Send(new CancelCreditCardCommand(cardId));
                    TempData["Success"] = "Tarjeta de crédito cancelada exitosamente.";
                }
                else
                {
                    TempData["Error"] = "El identificador de la tarjeta no es válido.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}