// This file defines the Minimal API route group for game-related endpoints.
using Microsoft.EntityFrameworkCore;
using TailspinToys.Api;

namespace TailspinToys.Api.Routes;

/// <summary>
/// Extension methods for registering game API routes.
/// </summary>
public static class GamesRoutes
{
    /// <summary>
    /// Maps all game-related API endpoints onto the application.
    /// </summary>
    /// <param name="app">The web application to register routes on.</param>
    public static void MapGamesRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/games");

        group.MapGet("/", async (TailspinToysContext db, int? categoryId, int? publisherId) =>
        {
            var query = db.Games
                .Include(g => g.Publisher)
                .Include(g => g.Category)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(g => g.CategoryId == categoryId.Value);

            if (publisherId.HasValue)
                query = query.Where(g => g.PublisherId == publisherId.Value);

            var games = await query.OrderBy(g => g.Id).ToListAsync();

            return Results.Ok(games.Select(g => g.ToDict()));
        });

        group.MapGet("/{id:int}", async (int id, TailspinToysContext db) =>
        {
            var game = await db.Games
                .Include(g => g.Publisher)
                .Include(g => g.Category)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game is null)
                return Results.NotFound(new { error = "Game not found" });

            return Results.Ok(game.ToDict());
        });
    }
}
