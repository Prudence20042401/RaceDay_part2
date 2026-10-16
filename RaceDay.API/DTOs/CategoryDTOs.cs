using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    /// <summary>Fields an Organiser supplies to create or update a category.</summary>
    public class CategoryDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "EventID is required.")]
        public int EventID { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }
    }

    /// <summary>A category as returned by the API.</summary>
    public class CategoryResponseDTO
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
