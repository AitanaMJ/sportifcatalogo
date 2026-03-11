namespace MyStoreLaZeta.Services
{
    public class PasswordHasher
    {
        // encripta la contraseña
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // verifica si la contraseña coincide con el hash guardado
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
