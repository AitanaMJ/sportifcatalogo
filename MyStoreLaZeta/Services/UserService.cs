using System.Linq.Expressions;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using MyStoreLaZeta.Entities;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Repositories;


namespace MyStoreLaZeta.Services
{
    public class UserService(GenericRepository<User> _userRepository)
    {
        public async Task<UserVM> Login(LoginVM loginVM)
        {
            var conditions = new List<Expression<Func<User, bool>>>()
            {
               x => x.Email == loginVM.Email,
               x => x.Password == loginVM.Password
            };

            var found = await _userRepository.GetByFilter(conditions.ToArray());

            var userVM = new UserVM();
            if (found != null)
            {
                userVM.UserId = found.UserId;
                userVM.FullName = found.FullName;
                userVM.Email = found.Email;
                userVM.Type = found.Type;


            }
            ;

            return userVM;
        }


        public async Task Register(UserVM userVM)
        {
            if (userVM.Password != userVM.RepeatPassword)
                throw new InvalidCastException("Las contraseñas no son iguales.");

                var conditions = new List<Expression<Func<User, bool>>>()
                {
                      x => x.Email == userVM.Email,
                };

            var foundEmail = await _userRepository.GetByFilter(conditions: conditions.ToArray());

            if (foundEmail !=null)
                throw new InvalidCastException("Este correo electrónico ya está registrado.");

            var entity = new User()
            {
                FullName = userVM.FullName,
                Email = userVM.Email,
                Password = userVM.Password,
                Type = userVM.Type
            };

            await _userRepository.AddAsync(entity);
        }
    }
}
