using System.Text.Json;

namespace MyStoreLaZeta.Utilities
{
    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T Value)
        {
            session.SetString(key, JsonSerializer.Serialize(Value));
        }

        public static T Get<T>(this ISession session, string key)
        { 
            var value = session.GetString(key);
           return value == null ? default(T) : JsonSerializer.Deserialize<T>(value);
        }
    }
}
