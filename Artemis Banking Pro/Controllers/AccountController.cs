using Artemis_Banking_Pro.Models.ViewModels;
using ArtemisBankingPro.Core.Application.Account.ConfirmAccount;
using ArtemisBankingPro.Core.Application.Account.GetResetToken;
using ArtemisBankingPro.Core.Application.Account.ResetPassword;
using ArtemisBankingPro.Core.Domain.Entities;
using Artemis_Banking_Pro.Models.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMediator _mediator;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IMediator mediator)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _mediator = mediator;
        }

        // ==========================================
        // 1. LOGIN
        // ==========================================

        [AllowAnonymous]
        public IActionResult Login()
        {
            // Si ya está autenticado, redirigir al Home correspondiente
            if (User.Identity.IsAuthenticated)
            {
                return RedirectBasedOnRole();
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Los datos de acceso son inválidos.");
                return View(model);
            }

            // Validar si la cuenta está inactiva
            if (!user.Activo)
            {
                ModelState.AddModelError(string.Empty, "Su cuenta se encuentra inactiva. Debe activar su cuenta mediante el enlace enviado a su correo electrónico registrado para poder acceder al sistema.");
                return View(model);
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Validar que el rol Comercio no acceda a la WebApp
            if (roles.Contains("Comercio"))
            {
                ModelState.AddModelError(string.Empty, "Este usuario no tiene permisos para acceder a la aplicación web.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectBasedOnRole(roles.FirstOrDefault());
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Los datos de acceso son inválidos.");
                return View(model);
            }
        }

        // ==========================================
        // 2. LOGOUT Y ACCESO DENEGADO
        // ==========================================

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==========================================
        // 3. CONFIRMAR / ACTIVAR CUENTA
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmAccount(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "El enlace de activación no es válido.";
                return RedirectToAction("Login");
            }

            try
            {
                await _mediator.Send(new ConfirmAccountCommand(token));
                TempData["SuccessMessage"] = "Su cuenta ha sido activada correctamente. Ya puede iniciar sesión.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Login");
        }

        // ==========================================
        // 4. SOLICITAR RESTABLECIMIENTO DE CONTRASEÑA
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var allowedRoles = new List<string> { "Administrador", "Cajero", "Cliente" };

                await _mediator.Send(new GetResetTokenCommand(model.UserName, allowedRoles));

                TempData["SuccessMessage"] = "Se ha enviado un enlace de restablecimiento de contraseña al correo electrónico registrado.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // ==========================================
        // 5. CREAR NUEVA CONTRASEÑA
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "El enlace de restablecimiento no es válido.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel { UserId = userId, Token = token };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _mediator.Send(new ResetPasswordCommand(model.UserId, model.Token, model.Password, model.ConfirmPassword));

                TempData["SuccessMessage"] = "Su contraseña ha sido restablecida correctamente. Ya puede iniciar sesión.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // ==========================================
        // MÉTODOS PRIVADOS
        // ==========================================

        private IActionResult RedirectBasedOnRole(string role = null)
        {
            if (string.IsNullOrEmpty(role))
            {
                if (User.IsInRole("Administrador")) role = "Administrador";
                else if (User.IsInRole("Cajero")) role = "Cajero";
                else if (User.IsInRole("Cliente")) role = "Cliente";
            }

            return role switch
            {
                "Administrador" => RedirectToAction("Index", "Admin"),
                "Cajero" => RedirectToAction("Index", "Cashier"),
                "Cliente" => RedirectToAction("Index", "Client"),
                _ => RedirectToAction("AccessDenied", "Account")
            };
        }
    }
}