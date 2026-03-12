using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyStoreLaZeta.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }
        [Required]
        public string Name { get; set; }

        public bool IsActive { get; set; } = true;

        // ==========================================
        // NUEVO: Descripción e Imagen
        // ==========================================
        public string? Description { get; set; }

        public string? ImageName { get; set; } // Acá se guarda el nombre del archivo (ej: remeras.jpg)

        [NotMapped] // Esto le dice a EF: "No intentes crear una columna para esto en SQL"
        public IFormFile? ImageFile { get; set; }

        public ICollection<Product> Products { get; set; }

    }
}
