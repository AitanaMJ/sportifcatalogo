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
       
        private readonly UserService _userService;
        private readonly AppDbContext _context;
        private readonly EmailService _emailService; 

        
        public AccountController(UserService userService, AppDbContext context, EmailService emailService)
        {
            _userService = userService;
            _context = context;
            _emailService = emailService; 
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

           
            user.Password = PasswordHasher.HashPassword(model.NewPassword);

            user.ResetToken = null;
            user.ResetTokenExpires = null;

            await _context.SaveChangesAsync();

            ViewBag.Message = "Contraseña actualizada correctamente. ¡Ya puedes volver a entrar!";
            return View("Login");
        }



       
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
                //identificar el rol del usaurio encontrado para asignarle los permisos correspondientes en la aplicación. Esto se hace creando una lista de claims (reclamaciones) que representan la identidad del usuario y sus roles. Luego, se crea una ClaimsIdentity con esos claims y se firma al usuario utilizando la autenticación de cookies. Finalmente, se redirige al usuario a la página principal de la aplicación.
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

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }


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

            //si el usuario no existe, termino aquí para no dar error
            if (user == null)
            {
                ViewBag.Message = "Si el email existe, te hemos enviado instrucciones.";
                return View();
            }

            // Generar Token
            user.ResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpires = DateTime.Now.AddHours(1);

            await _context.SaveChangesAsync();

            
            try
            {
                
                _emailService.SendEmail(user.Email, user.ResetToken);

                ViewBag.Message = "Te hemos enviado un enlace a tu correo. Revisa Spam por las dudas.";
            }
            catch (Exception ex)
            {
                // Si falla Gmail, muestro el error en pantalla para saber quepaso
               
                ViewBag.Message = "Error enviando correo: " + ex.Message;
            }
            

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