// This file contains integration tests for the publishers API endpoints.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TailspinToys.Api;
using TailspinToys.Api.Models;

namespace TailspinToys.Api.Tests;

/// <summary>
/// Integration tests for the /api/publishers endpoint.
/// </summary>
public class TestPublishersRoutes : IDisposable
{
    private readonly string _dbPath;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly Dictionary<string, object>[] TestPublishers =
    [
        new() { ["name"] = "DevGames Inc", ["description"] = "Makers of developer-themed games" },
        new() { ["name"] = "Scrum Masters", ["description"] = "Agile board game experts" },
        new() { ["name"] = "Binary Bros",   ["description"] = "Puzzle and logic game studio" }
    ];

    // Publisher that has no games — should not appear in the API response.
    private const string UnusedPublisherName = "Unused Publisher";

    private const string PublishersApiPath = "/api/publishers";

    public TestPublishersRoutes()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_{Guid.NewGuid()}.db");
        var connectionString = $"Data Source={_dbPath}";

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = connectionString
                    });
                });
            });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TailspinToysContext>();
        SeedTestData(db);
    }

    private static void SeedTestData(TailspinToysContext db)
    {
        // Seed a category required by the Game FK.
        var category = new Category { Name = "Test Category", Description = "A test category" };
        db.Categories.Add(category);

        // Seed publishers — each used publisher gets at least one game.
        var usedPublishers = TestPublishers.Select(p => new Publisher
        {
            Name        = (string)p["name"],
            Description = (string)p["description"]
        }).ToList();
        db.Publishers.AddRange(usedPublishers);

        // Seed a publisher that has no games attached — must not appear in the response.
        var unusedPublisher = new Publisher { Name = UnusedPublisherName, Description = "No games here" };
        db.Publishers.Add(unusedPublisher);

        db.SaveChanges();

        // Attach one game to each used publisher so the endpoint returns them.
        foreach (var publisher in usedPublishers)
        {
            db.Games.Add(new Game
            {
                Title       = $"Game for {publisher.Name}",
                Description = "A test game for filter endpoint coverage.",
                CategoryId  = category.Id,
                PublisherId = publisher.Id
            });
        }

        db.SaveChanges();
    }

    /// <summary>
    /// GET /api/publishers returns 200 with all seeded publishers.
    /// </summary>
    [Fact]
    public async Task GetPublishers_ReturnsAllPublishers()
    {
        var response = await _client.GetAsync(PublishersApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.Equal(TestPublishers.Length, data.Count);
    }

    /// <summary>
    /// GET /api/publishers returns the correct id and name fields for each publisher.
    /// </summary>
    [Fact]
    public async Task GetPublishers_ReturnsCorrectStructure()
    {
        var response = await _client.GetAsync(PublishersApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.NotEmpty(data);

        foreach (var publisher in data)
        {
            Assert.True(publisher.ContainsKey("id"), "Missing field: id");
            Assert.True(publisher.ContainsKey("name"), "Missing field: name");
        }
    }

    /// <summary>
    /// GET /api/publishers does not include extraneous fields such as description.
    /// </summary>
    [Fact]
    public async Task GetPublishers_DoesNotExposeExtraFields()
    {
        var response = await _client.GetAsync(PublishersApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.NotEmpty(data);

        foreach (var publisher in data)
        {
            Assert.False(publisher.ContainsKey("description"), "Unexpected field: description");
            Assert.False(publisher.ContainsKey("gameCount"),   "Unexpected field: gameCount");
        }
    }

    /// <summary>
    /// GET /api/publishers returns publishers with correct names matching seeded data.
    /// </summary>
    [Fact]
    public async Task GetPublishers_ReturnsCorrectNames()
    {
        var response = await _client.GetAsync(PublishersApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);

        var returnedNames = data
            .Select(p => Assert.IsType<JsonElement>(p["name"]).GetString())
            .ToHashSet();

        foreach (var expected in TestPublishers)
        {
            Assert.Contains(expected["name"].ToString(), returnedNames);
        }
    }

    /// <summary>
    /// GET /api/publishers on an empty database returns an empty list (not 404).
    /// </summary>
    [Fact]
    public async Task GetPublishers_EmptyDatabase_ReturnsEmptyList()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TailspinToysContext>();
        db.Games.RemoveRange(db.Games);
        db.Publishers.RemoveRange(db.Publishers);
        db.SaveChanges();

        var response = await _client.GetAsync(PublishersApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<object>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.Empty(data);
    }

    /// <summary>
    /// GET /api/publishers omits publishers that have no associated games.
    /// </summary>
    [Fact]
    public async Task GetPublishers_OnlyReturnsUsedPublishers()
    {
        var response = await _client.GetAsync(PublishersApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);

        var returnedNames = data
            .Select(p => Assert.IsType<JsonElement>(p["name"]).GetString())
            .ToHashSet();

        // The unused publisher must not be present.
        Assert.DoesNotContain(UnusedPublisherName, returnedNames);

        // All used publishers must be present.
        foreach (var expected in TestPublishers)
        {
            Assert.Contains(expected["name"].ToString(), returnedNames);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        try
        {
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // Best-effort cleanup
        }
    }
}
