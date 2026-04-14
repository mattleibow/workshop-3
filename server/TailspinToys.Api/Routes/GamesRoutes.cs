using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using TailspinToys.Api;

namespace TailspinToys.Api.Routes;

public static class GamesRoutes
{
    public static void MapGamesRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/games");

        group.MapGet("/", async (
            [Microsoft.AspNetCore.Mvc.FromQuery] int[]? categoryIds,
            [Microsoft.AspNetCore.Mvc.FromQuery] int[]? publisherIds,
            TailspinToysContext db) =>
        {
            var query = db.Games
                .Include(g => g.Publisher)
                .Include(g => g.Category)
                .AsQueryable();

            if (categoryIds is { Length: > 0 })
                query = query.Where(g => categoryIds.Contains(g.CategoryId));

            if (publisherIds is { Length: > 0 })
                query = query.Where(g => publisherIds.Contains(g.PublisherId));

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
