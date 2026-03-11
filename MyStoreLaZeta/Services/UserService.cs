using System.Linq.Expressions;
using MyStoreLaZeta.Entities;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Repositories;

namespace MyStoreLaZeta.Services
{
    public class UserService(GenericRepository<User> _userRepository)
    {
        
        public async Task<UserVM> Login(LoginVM loginVM)
        {
            //  buscando al usuario únicamente por su Email
            var conditions = new List<Expression<Func<User, bool>>>()
            {
                x => x.Email == loginVM.Email
            };

            var found = await _userRepository.GetByFilter(conditions.ToArray());

            var userVM = new UserVM();

            // si el usuario existe, verificamos matemáticamente la contraseña
            if (found != null && PasswordHasher.VerifyPassword(loginVM.Password, found.Password))
            {
                userVM.UserId = found.UserId;
                userVM.FullName = found.FullName;
                userVM.Email = found.Email;
                userVM.Type = found.Type;
            }
            else
            {
                // si no coincide o no existe, nos aseguramos de que el ID sea 0
                userVM.UserId = 0;
            }

            return userVM;
        }

      
        public async Task Register(UserVM userVM)
        {
            // validación de coincidencia de claves (Seguridad en el cliente)
            if (userVM.Password != userVM.RepeatPassword)
                throw new InvalidCastException("Las contraseñas no coinciden.");

            // verificamos si el email ya esta en uso
            var conditions = new List<Expression<Func<User, bool>>>()
            {
                x => x.Email == userVM.Email
            };

            var foundEmail = await _userRepository.GetByFilter(conditions: conditions.ToArray());

            if (foundEmail != null)
                throw new InvalidCastException("Este correo electrónico ya se encuentra registrado.");

            // creo la entidad guardando el Hash, nunca el texto plano
            var entity = new User()
            {
                FullName = userVM.FullName,
                Email = userVM.Email,
                Password = PasswordHasher.HashPassword(userVM.Password),
                Type = userVM.Type
            };

            await _userRepository.AddAsync(entity);
        }
    }
}