using System.ComponentModel.DataAnnotations;

namespace RecipeBackend.DTOs
{
    /// <summary>
    /// Data transfer object for creating a new recipe.
    /// </summary>
    public class RecipeCreateDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        public List<string>? Ingredients { get; set; }

        public List<string>? Tags { get; set; }
    }

    /// <summary>
    /// Data transfer object for updating an existing recipe.
    /// </summary>
    public class RecipeUpdateDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        public List<string>? Ingredients { get; set; }

        public List<string>? Tags { get; set; }
    }

    /// <summary>
    /// Data transfer object used for returning recipe details to clients.
    /// </summary>
    public class RecipeResponseDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<string> Ingredients { get; set; } = new();

        public List<string> Tags { get; set; } = new();

        public string? Author { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }

    /// <summary>
    /// Query parameters for searching/filtering recipes.
    /// </summary>
    public class RecipeQuery
    {
        public string? Query { get; set; }
        public string? Ingredient { get; set; }
        public string? Tag { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
    }

    /// <summary>
    /// Auth DTOs for register/login.
    /// </summary>
    public class RegisterRequest
    {
        [Required, MinLength(3), MaxLength(32)]
        public string Username { get; set; } = string.Empty;

        [Required, MinLength(6), MaxLength(128)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}
