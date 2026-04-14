// Publishers route group — exposes publisher data via the /api/publishers endpoint.
using Microsoft.EntityFrameworkCore;
using TailspinToys.Api;

namespace TailspinToys.Api.Routes;

/// <summary>
/// Extension methods that register publisher-related API routes.
/// </summary>
public static class PublishersRoutes
{
    /// <summary>
    /// Maps all publisher endpoints onto the application.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to register routes on.</param>
    public static void MapPublishersRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/publishers");

        group.MapGet("/", async (TailspinToysContext db) =>
        {
            var publishers = await db.Publishers
                .OrderBy(p => p.Id)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            return Results.Ok(publishers);
        });
    }
}
