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

        
        public AccountController(UserService userService, AppDbContext context, EmailService emailService) //constructor del controller que recibe el servicio de usuario, el contexto de la base de datos y el servicio de correo electrónico a través de la inyección de dependencias. Esto permite que el controlador utilice estos servicios para manejar las operaciones relacionadas con la cuenta, como el inicio de sesión, el registro y la recuperación de contraseña.
        {
            _userService = userService;
            _context = context;
            _emailService = emailService; //parametros del constructor para inyectar el servicio de correo electrónico
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model) //metodo que maneja la solicitud POST para restablecer la contraseña. Recibe un modelo ResetPasswordVM que contiene el token de restablecimiento y la nueva contraseña. El método verifica la validez del token, actualiza la contraseña del usuario si el token es válido, y luego redirige al usuario a la página de inicio de sesión con un mensaje de éxito. Si el token es inválido o ha expirado, muestra un mensaje de error.
        {
            if (!ModelState.IsValid) return View(model); //verifica si los datos enviados son correctos según las reglas de validación definidas en el modelo. Si no son válidos, devuelve la misma vista con el modelo para que el usuario pueda corregir los errores.

            var user = _context.Users.FirstOrDefault(u => u.ResetToken == model.Token && u.ResetTokenExpires > DateTime.Now); //busca usuario por token y verifica que el token no haya expirado. Si no encuentra un usuario con ese token o el token ha expirado, devuelve un mensaje de error indicando que el token es inválido o ha expirado.

            if (user == null)
            {
                return Content("Error: Token inválido o expirado."); //validar si el usuario existe y si el token es válido. Si no se encuentra un usuario con el token proporcionado o si el token ha expirado, devuelve un mensaje de error indicando que el token es inválido o ha expirado.
            }

           
            user.Password = PasswordHasher.HashPassword(model.NewPassword); //hasehear la nueva contraseña utilizando el servicio de hash de contraseñas para garantizar que la contraseña se almacene de forma segura en la base de datos. Esto implica convertir la contraseña en texto plano en un hash que no se puede revertir, lo que protege la contraseña del usuario incluso si la base de datos es comprometida.

            user.ResetToken = null; //limpiar el token y su fecha de expiración para evitar que se pueda reutilizar el mismo token para restablecer la contraseña nuevamente. Esto es una medida de seguridad para asegurarse de que cada token solo se pueda usar una vez y tenga una vida útil limitada. Al establecer el token en null, se invalida cualquier intento futuro de usar ese token para restablecer la contraseña.
            user.ResetTokenExpires = null;

            await _context.SaveChangesAsync(); //guarda cambios en la base de datos para actualizar la contraseña del usuario y limpiar el token de restablecimiento. Esto asegura que los cambios realizados en el objeto del usuario se reflejen en la base de datos, permitiendo que el usuario pueda iniciar sesión con su nueva contraseña y que el token de restablecimiento ya no sea válido para futuros intentos de restablecimiento de contraseña.

            ViewBag.Message = "Contraseña actualizada correctamente. ¡Ya puedes volver a entrar!"; //mensaje para la vista que indica que la contraseña se ha actualizado correctamente y que el usuario ya puede iniciar sesión con su nueva contraseña. Este mensaje se muestra después de que el proceso de restablecimiento de contraseña se haya completado con éxito, proporcionando retroalimentación al usuario sobre el resultado de su acción.
            return View("Login"); //retorna a la vista login después de actualizar la contraseña, lo que permite al usuario iniciar sesión con su nueva contraseña. Esto es útil para guiar al usuario de vuelta a la página de inicio de sesión después de haber restablecido su contraseña, facilitando el proceso de acceso a su cuenta con la nueva contraseña que acaba de establecer.
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
                List<Claim> claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.NameIdentifier, found.UserId.ToString()),
                    new Claim(ClaimTypes.Name, found.FullName),
                    new Claim(ClaimTypes.Email, found.Email),
                    new Claim(ClaimTypes.Role, found.Type),
                    
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