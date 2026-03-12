using System.ComponentModel.DataAnnotations;

namespace MyStoreLaZeta.Models
{
    public class CategoryVM
    {
        public int CategoryId { get; set; }
        [Required]


        public string Name { get; set; }


        // Agregamos esto para que en la vista del Admin aparezca el estado
        public bool IsActive { get; set; }

        // ==========================================
        // NUEVO: Descripción e Imagen
        // ==========================================
        public string? Description { get; set; }

        public string? ImageName { get; set; }

        public IFormFile? ImageFile { get; set; }
    }
}
