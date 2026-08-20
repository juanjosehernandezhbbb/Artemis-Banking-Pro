using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Users.CreateUser;
using ArtemisBankingPro.Core.Application.Users.GetUsers;
using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Users.CreateUser;
using ArtemisBankingPro.Core.Application.Users.GetUsers;
using ArtemisBankingPro.Core.Application.Users.GetUserById;
using ArtemisBankingPro.Core.Application.Users.UpdateUser;
using ArtemisBankingPro.Core.Application.Users.ChangeUserStatus;
using ArtemisBankingPro.Core.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.WebApp.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsersController : Controller
    {
        private readonly IMediator _mediator;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IIdentityService _identityService; // <-- Nuevo servicio inyectado

        public UsersController(
            IMediator mediator,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IIdentityService identityService) // <-- Añadido aquí
        {
            _mediator = mediator;
            _userManager = userManager;
            _emailService = emailService;
            _identityService = identityService;
        }

        // GET: /Users/Index
        public async Task<IActionResult> Index(int page = 1, string? role = null)
        {
            var query = new GetUsersQuery(Page: page, PageSize: 20, Role: role);
            var result = await _mediator.Send(query);

            ViewBag.CurrentRole = role;
            return View("~/Views/Admin/Users/Index.cshtml", result);
        }

        // GET: /Users/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View("~/Views/Admin/Users/Create.cshtml");
        }

        // POST: /Users/Create
        [HttpPost]
        public async Task<IActionResult> Create(CreateUserCommand command)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Admin/Users/Create.cshtml", command);

            try
            {
                await _mediator.Send(command);
                TempData["Success"] = "Usuario creado exitosamente. Se ha enviado un correo de activación.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("~/Views/Admin/Users/Create.cshtml", command);
            }
        }

        // GET: /Users/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _mediator.Send(new GetUserByIdQuery(id));
            if (user == null)
            {
                TempData["Error"] = "El usuario seleccionado no existe.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == currentUserId)
            {
                TempData["Error"] = "No puede editar su propia cuenta desde este módulo.";
                return RedirectToAction(nameof(Index));
            }

            var command = new UpdateUserCommand(
                Id: user.Id,
                FirstName: user.FirstName,
                LastName: user.LastName,
                Identification: user.Identification,
                Email: user.Email,
                UserName: user.UserName,
                Password: null,
                ConfirmPassword: null,
                AdditionalAmount: 0m,
                PerformedByUserId: currentUserId ?? string.Empty
            );

            ViewBag.UserRole = user.Role;
            return View("~/Views/Admin/Users/Edit.cshtml", command);
        }

        // POST: /Users/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(UpdateUserCommand command)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            command = command with { PerformedByUserId = currentUserId ?? string.Empty };

            if (!ModelState.IsValid)
                return View("~/Views/Admin/Users/Edit.cshtml", command);

            try
            {
                await _mediator.Send(command);
                TempData["Success"] = "Usuario actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("~/Views/Admin/Users/Edit.cshtml", command);
            }
        }

        // POST: /Users/ToggleStatus
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == currentUserId)
            {
                TempData["Error"] = "No puede modificar el estado de su propia cuenta.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var user = await _mediator.Send(new GetUserByIdQuery(id));
                if (user == null)
                {
                    TempData["Error"] = "El usuario seleccionado no existe.";
                    return RedirectToAction(nameof(Index));
                }

                bool newStatus = !user.IsActive;
                var command = new ChangeUserStatusCommand(id, newStatus, currentUserId ?? string.Empty);
                await _mediator.Send(command);

                TempData["Success"] = "Estado del usuario actualizado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // REENVIAR CORREO DE ACTIVACIÓN 
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> ResendActivationEmail(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "El usuario seleccionado no existe.";
                return RedirectToAction(nameof(Index));
            }

            if (user.EmailConfirmed)
            {
                TempData["Error"] = "Este usuario ya tiene su correo confirmado y su cuenta activa.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Usamos IIdentityService para generar el token empaquetado y codificado correctamente
                var token = await _identityService.GenerateEmailConfirmationTokenAsync(user);
                string userNameForEmail = user.UserName ?? "Usuario";

                await _emailService.SendActivationEmailAsync(user.Email, userNameForEmail, token);

                TempData["Success"] = $"Correo de activación reenviado exitosamente a {user.Email}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al reenviar el correo: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}