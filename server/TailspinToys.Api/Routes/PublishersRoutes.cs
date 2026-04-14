// This file defines the Minimal API route group for publisher-related endpoints.
using Microsoft.EntityFrameworkCore;
using TailspinToys.Api;

namespace TailspinToys.Api.Routes;

/// <summary>
/// Extension methods for registering publisher API routes.
/// </summary>
public static class PublishersRoutes
{
    /// <summary>
    /// Maps all publisher-related API endpoints onto the application.
    /// </summary>
    /// <param name="app">The web application to register routes on.</param>
    public static void MapPublishersRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/publishers");

        group.MapGet("/", async (TailspinToysContext db) =>
        {
            var publishers = await db.Publishers
                .Where(p => p.Games.Any())
                .OrderBy(p => p.Id)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            return Results.Ok(publishers);
        });
    }
}
