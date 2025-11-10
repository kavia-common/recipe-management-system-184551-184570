using RecipeBackend.DTOs;
using RecipeBackend.Models;

namespace RecipeBackend.Utils
{
    public static class Mapping
    {
        public static RecipeResponseDto ToDto(this Recipe r) => new()
        {
            Id = r.Id,
            Title = r.Title,
            Description = r.Description,
            Ingredients = r.Ingredients.ToList(),
            Tags = r.Tags.ToList(),
            Author = r.Author,
            CreatedAtUtc = r.CreatedAtUtc,
            UpdatedAtUtc = r.UpdatedAtUtc
        };
    }
}
