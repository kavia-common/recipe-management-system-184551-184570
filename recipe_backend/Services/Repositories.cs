using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using RecipeBackend.Models;

namespace RecipeBackend.Services
{
    /// <summary>
    /// Simple in-memory user entity.
    /// </summary>
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// In-memory user repository.
    /// </summary>
    public interface IUserRepository
    {
        bool TryCreateUser(string username, string password);
        bool ValidateCredentials(string username, string password);
        bool Exists(string username);
    }

    public class InMemoryUserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);

        public InMemoryUserRepository()
        {
            // Seed demo user
            TryCreateUser("demo", "password123");
        }

        public bool TryCreateUser(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                return false;

            var user = new User
            {
                Username = username,
                PasswordHash = HashPassword(password),
                CreatedAtUtc = DateTime.UtcNow
            };
            return _users.TryAdd(username, user);
        }

        public bool ValidateCredentials(string username, string password)
        {
            if (!_users.TryGetValue(username, out var user))
                return false;

            var hash = HashPassword(password);
            return SlowEquals(hash, user.PasswordHash);
        }

        public bool Exists(string username) => _users.ContainsKey(username);

        private static string HashPassword(string password)
        {
            // Simple SHA256 for demo; not for production.
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        private static bool SlowEquals(string a, string b)
        {
            // constant-time compare
            var ba = Encoding.UTF8.GetBytes(a);
            var bb = Encoding.UTF8.GetBytes(b);
            uint diff = (uint)ba.Length ^ (uint)bb.Length;
            var len = Math.Min(ba.Length, bb.Length);
            for (int i = 0; i < len; i++)
                diff |= (uint)(ba[i] ^ bb[i]);
            return diff == 0 && ba.Length == bb.Length;
        }
    }

    /// <summary>
    /// In-memory recipe repository with basic search.
    /// </summary>
    public interface IRecipeRepository
    {
        Recipe Add(Recipe recipe);
        Recipe? Get(Guid id);
        IEnumerable<Recipe> GetAll();
        bool Update(Recipe recipe);
        bool Delete(Guid id);
        IEnumerable<Recipe> Search(string? query, string? ingredient, string? tag);
    }

    public class InMemoryRecipeRepository : IRecipeRepository
    {
        private readonly ConcurrentDictionary<Guid, Recipe> _store = new();

        public InMemoryRecipeRepository()
        {
            // Seed data
            var r1 = new Recipe
            {
                Title = "Classic Pancakes",
                Description = "Fluffy pancakes with maple syrup.",
                Ingredients = new List<string> { "Flour", "Eggs", "Milk", "Sugar", "Baking Powder", "Salt" },
                Tags = new List<string> { "breakfast", "sweet", "quick" },
                Author = "demo"
            };
            Add(r1);

            var r2 = new Recipe
            {
                Title = "Garlic Lemon Chicken",
                Description = "Pan-seared chicken with garlic and lemon butter sauce.",
                Ingredients = new List<string> { "Chicken Breast", "Garlic", "Lemon", "Butter", "Parsley", "Salt", "Pepper" },
                Tags = new List<string> { "dinner", "savory", "high-protein" },
                Author = "demo"
            };
            Add(r2);
        }

        public Recipe Add(Recipe recipe)
        {
            recipe.Id = Guid.NewGuid();
            recipe.CreatedAtUtc = DateTime.UtcNow;
            recipe.UpdatedAtUtc = recipe.CreatedAtUtc;
            _store[recipe.Id] = recipe;
            return recipe;
        }

        public Recipe? Get(Guid id) => _store.TryGetValue(id, out var r) ? r : null;

        public IEnumerable<Recipe> GetAll() => _store.Values.OrderByDescending(r => r.CreatedAtUtc);

        public bool Update(Recipe recipe)
        {
            if (!_store.ContainsKey(recipe.Id)) return false;
            recipe.UpdatedAtUtc = DateTime.UtcNow;
            _store[recipe.Id] = recipe;
            return true;
        }

        public bool Delete(Guid id) => _store.TryRemove(id, out _);

        public IEnumerable<Recipe> Search(string? query, string? ingredient, string? tag)
        {
            IEnumerable<Recipe> result = _store.Values;

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                result = result.Where(r =>
                    r.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (r.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(ingredient))
            {
                var ing = ingredient.Trim();
                result = result.Where(r => r.Ingredients.Any(i => i.Contains(ing, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var tg = tag.Trim();
                result = result.Where(r => r.Tags.Any(t => t.Equals(tg, StringComparison.OrdinalIgnoreCase)));
            }

            return result.OrderByDescending(r => r.CreatedAtUtc);
        }
    }
}
