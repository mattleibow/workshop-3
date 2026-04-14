// This file defines the Minimal API route group for category-related endpoints.
using Microsoft.EntityFrameworkCore;
using TailspinToys.Api;

namespace TailspinToys.Api.Routes;

/// <summary>
/// Extension methods for registering category API routes.
/// </summary>
public static class CategoriesRoutes
{
    /// <summary>
    /// Maps all category-related API endpoints onto the application.
    /// </summary>
    /// <param name="app">The web application to register routes on.</param>
    public static void MapCategoriesRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/categories");

        group.MapGet("/", async (TailspinToysContext db) =>
        {
            var categories = await db.Categories
                .OrderBy(c => c.Id)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return Results.Ok(categories);
        });
    }
}
