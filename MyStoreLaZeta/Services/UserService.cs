using System.Linq.Expressions;
using MyStoreLaZeta.Entities;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Repositories;

namespace MyStoreLaZeta.Services
{
    public class UserService(GenericRepository<User> _userRepository)
    {
        // ==========================================
        // LOGIN: Verificación Segura con BCrypt
        // ==========================================
        public async Task<UserVM> Login(LoginVM loginVM)
        {
            // Paso 1: Buscamos al usuario únicamente por su Email
            var conditions = new List<Expression<Func<User, bool>>>()
            {
                x => x.Email == loginVM.Email
            };

            var found = await _userRepository.GetByFilter(conditions.ToArray());

            var userVM = new UserVM();

            // Paso 2: Si el usuario existe, verificamos matemáticamente la contraseña
            if (found != null && PasswordHasher.VerifyPassword(loginVM.Password, found.Password))
            {
                userVM.UserId = found.UserId;
                userVM.FullName = found.FullName;
                userVM.Email = found.Email;
                userVM.Type = found.Type;
            }
            else
            {
                // Si no coincide o no existe, nos aseguramos de que el ID sea 0
                userVM.UserId = 0;
            }

            return userVM;
        }

        // ==========================================
        // REGISTRO: Encriptación de Contraseña
        // ==========================================
        public async Task Register(UserVM userVM)
        {
            // Validación de coincidencia de claves (Seguridad en el cliente)
            if (userVM.Password != userVM.RepeatPassword)
                throw new InvalidCastException("Las contraseñas no coinciden.");

            // Verificamos si el email ya está en uso
            var conditions = new List<Expression<Func<User, bool>>>()
            {
                x => x.Email == userVM.Email
            };

            var foundEmail = await _userRepository.GetByFilter(conditions: conditions.ToArray());

            if (foundEmail != null)
                throw new InvalidCastException("Este correo electrónico ya se encuentra registrado.");

            // Paso 3: Creamos la entidad guardando el Hash, nunca el texto plano
            var entity = new User()
            {
                FullName = userVM.FullName,
                Email = userVM.Email,
                // ¡AQUÍ ESTÁ LA MAGIA!
                Password = PasswordHasher.HashPassword(userVM.Password),
                Type = userVM.Type
            };

            await _userRepository.AddAsync(entity);
        }
    }
}