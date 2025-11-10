using System.ComponentModel.DataAnnotations;

namespace RecipeBackend.Models
{
    /// <summary>
    /// Domain model representing a recipe entity persisted in memory.
    /// </summary>
    public class Recipe
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        public List<string> Ingredients { get; set; } = new();

        public List<string> Tags { get; set; } = new();

        [MaxLength(200)]
        public string? Author { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
