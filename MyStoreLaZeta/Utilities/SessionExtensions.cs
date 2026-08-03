using System.Text.Json;

namespace CatalogoPro.Utilities
{
    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T Value)
        {
            session.SetString(key, JsonSerializer.Serialize(Value)); //convierte el bojeto a json y guarda el json en la sesión con la clave especificada. Esto permite almacenar objetos complejos en la sesión de manera sencilla, ya que el objeto se serializa a una cadena JSON antes de ser guardado, y luego se puede deserializar de nuevo al tipo original cuando se recupere de la sesión.
        }

        public static T Get<T>(this ISession session, string key)
        { 
            var value = session.GetString(key); //busca el string json guardado en la sesión con la clave especificada. Si no se encuentra ningún valor para esa clave, GetString devuelve null. Luego, el método verifica si el valor es null; si lo es, devuelve el valor predeterminado para el tipo T (que es null para tipos de referencia y 0 o false para tipos de valor). Si el valor no es null, deserializa la cadena JSON de nuevo al tipo T utilizando JsonSerializer.Deserialize<T>(value) y devuelve el objeto resultante. Esto permite recuperar objetos complejos almacenados en la sesión de manera sencilla, ya que el objeto se deserializa automáticamente al tipo original cuando se obtiene de la sesión.

            return value == null 
                ? default(T) 
                : JsonSerializer.Deserialize<T>(value); //convierte nuevamente el json en un objeto del tipo T y lo devuelve. Si el valor es null, devuelve el valor predeterminado para el tipo T. Esto permite recuperar objetos complejos almacenados en la sesión de manera sencilla, ya que el objeto se deserializa automáticamente al tipo original cuando se obtiene de la sesión.
        }
    }
}
