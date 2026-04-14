// Categories route group — exposes category data via the /api/categories endpoint.
using Microsoft.EntityFrameworkCore;
using TailspinToys.Api;

namespace TailspinToys.Api.Routes;

/// <summary>
/// Extension methods that register category-related API routes.
/// </summary>
public static class CategoriesRoutes
{
    /// <summary>
    /// Maps all category endpoints onto the application.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to register routes on.</param>
    public static void MapCategoriesRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/categories");

        group.MapGet("/", async (TailspinToysContext db) =>
        {
            var categories = await db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return Results.Ok(categories);
        });
    }
}
