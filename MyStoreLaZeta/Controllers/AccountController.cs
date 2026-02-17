using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyStoreLaZeta.Context;
using MyStoreLaZeta.Entities;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Services;

namespace MyStoreLaZeta.Controllers
{
    public class AccountController : Controller
    {
        // 1. DEFINIMOS LAS HERRAMIENTAS
        private readonly UserService _userService;
        private readonly AppDbContext _context;
        private readonly EmailService _emailService; // <--- NUEVO: Agregamos el servicio de Email

        // 2. CONSTRUCTOR ACTUALIZADO
        public AccountController(UserService userService, AppDbContext context, EmailService emailService)
        {
            _userService = userService;
            _context = context;
            _emailService = emailService; // <--- NUEVO: Conectamos el servicio
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = _context.Users.FirstOrDefault(u => u.ResetToken == model.Token && u.ResetTokenExpires > DateTime.Now);

            if (user == null)
            {
                return Content("Error: Token inválido o expirado.");
            }

            // --- ¡ZETIFICACIÓN DE SEGURIDAD! ---
            // Encriptamos la nueva contraseña antes de guardarla
            user.Password = PasswordHasher.HashPassword(model.NewPassword);

            user.ResetToken = null;
            user.ResetTokenExpires = null;

            await _context.SaveChangesAsync();

            ViewBag.Message = "Contraseña actualizada correctamente. ¡Ya puedes volver a entrar!";
            return View("Login");
        }



        // ==========================================
        // LOGIN
        // ==========================================
        public IActionResult Login()
        {
            var viewModel = new LoginVM();
            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginVM viewmodel)
        {
            if (!ModelState.IsValid) return View(viewmodel);

            var found = await _userService.Login(viewmodel);

            if (found.UserId == 0)
            {
                ViewBag.message = "Usuario o contraseña incorrectos";
                return View();
            }
            else
            {
                List<Claim> claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.NameIdentifier, found.UserId.ToString()),
                    new Claim(ClaimTypes.Name, found.FullName),
                    new Claim(ClaimTypes.Email, found.Email),
                    new Claim(ClaimTypes.Role, found.Type)
                };

                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties { AllowRefresh = true }
                );

                return RedirectToAction("Index", "Home");
            }
        }

        

        // ==========================================
        // REGISTRO
        // ==========================================
        public IActionResult Register()
        {
            var viewModel = new UserVM();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Register(UserVM viewmodel)
        {
            if (!ModelState.IsValid) return View(viewmodel);

            try
            {
                await _userService.Register(viewmodel);
                ViewBag.message = "Tu cuenta ha sido registrada. Por favor, inicia sesión.";
                ViewBag.Class = "alert-success";
            }
            catch (Exception ex)
            {
                ViewBag.message = ex.Message;
                ViewBag.Class = "alert-danger";
            }

            return View(viewmodel);
        }

        // ==========================================
        // SALIR (LOGOUT)
        // ==========================================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // RECUPERAR CONTRASEÑA (AQUÍ ESTÁ EL CAMBIO IMPORTANTE)
        // ==========================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

            // IMPORTANTE: Si el usuario no existe, terminamos aquí para no dar error
            if (user == null)
            {
                ViewBag.Message = "Si el email existe, te hemos enviado instrucciones.";
                return View();
            }

            // Generar Token
            user.ResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpires = DateTime.Now.AddHours(1);

            await _context.SaveChangesAsync();

            // --- CAMBIO: ENVIAR EL EMAIL REAL ---
            try
            {
                // Ya no usamos Console.WriteLine, usamos el servicio real
                _emailService.SendEmail(user.Email, user.ResetToken);

                ViewBag.Message = "Te hemos enviado un enlace a tu correo. Revisa Spam por las dudas.";
            }
            catch (Exception ex)
            {
                // Si falla Gmail, mostramos el error en pantalla para que sepas qué pasó
                // (Luego cuando funcione bien, puedes quitar este mensaje de error)
                ViewBag.Message = "Error enviando correo: " + ex.Message;
            }
            // ------------------------------------

            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            var user = _context.Users.FirstOrDefault(u => u.ResetToken == token && u.ResetTokenExpires > DateTime.Now);

            if (user == null)
            {
                return Content("El enlace es inválido o ha expirado.");
            }

            var model = new ResetPasswordVM { Token = token };
            return View(model);
        }

    }
}