// This file contains integration tests for the categories API endpoints.
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
/// Integration tests for the /api/categories endpoint.
/// </summary>
public class TestCategoriesRoutes : IDisposable
{
    private readonly string _dbPath;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly Dictionary<string, object>[] TestCategories =
    [
        new() { ["name"] = "Strategy",    ["description"] = "Strategic thinking games" },
        new() { ["name"] = "Card Game",   ["description"] = "Games played with cards" },
        new() { ["name"] = "Cooperative", ["description"] = "Games where players work together" }
    ];

    // Category that has no games — should not appear in the API response.
    private const string UnusedCategoryName = "Unused Category";

    private const string CategoriesApiPath = "/api/categories";

    public TestCategoriesRoutes()
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
        // Seed a publisher required by the Game FK.
        var publisher = new Publisher { Name = "Test Publisher", Description = "A test publisher" };
        db.Publishers.Add(publisher);

        // Seed categories — each used category gets at least one game.
        var usedCategories = TestCategories.Select(c => new Category
        {
            Name        = (string)c["name"],
            Description = (string)c["description"]
        }).ToList();
        db.Categories.AddRange(usedCategories);

        // Seed a category that has no games attached — must not appear in the response.
        var unusedCategory = new Category { Name = UnusedCategoryName, Description = "No games here" };
        db.Categories.Add(unusedCategory);

        db.SaveChanges();

        // Attach one game to each used category so the endpoint returns them.
        foreach (var category in usedCategories)
        {
            db.Games.Add(new Game
            {
                Title       = $"Game for {category.Name}",
                Description = "A test game for filter endpoint coverage.",
                CategoryId  = category.Id,
                PublisherId = publisher.Id
            });
        }

        db.SaveChanges();
    }

    /// <summary>
    /// GET /api/categories returns 200 with all seeded categories.
    /// </summary>
    [Fact]
    public async Task GetCategories_ReturnsAllCategories()
    {
        var response = await _client.GetAsync(CategoriesApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.Equal(TestCategories.Length, data.Count);
    }

    /// <summary>
    /// GET /api/categories returns the correct id and name fields.
    /// </summary>
    [Fact]
    public async Task GetCategories_ReturnsCorrectStructure()
    {
        var response = await _client.GetAsync(CategoriesApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.NotEmpty(data);

        foreach (var category in data)
        {
            Assert.True(category.ContainsKey("id"),   "Missing field: id");
            Assert.True(category.ContainsKey("name"), "Missing field: name");
        }
    }

    /// <summary>
    /// GET /api/categories does not expose extraneous fields such as description.
    /// </summary>
    [Fact]
    public async Task GetCategories_DoesNotExposeExtraFields()
    {
        var response = await _client.GetAsync(CategoriesApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.NotEmpty(data);

        foreach (var category in data)
        {
            Assert.False(category.ContainsKey("description"), "Unexpected field: description");
            Assert.False(category.ContainsKey("gameCount"),   "Unexpected field: gameCount");
        }
    }

    /// <summary>
    /// GET /api/categories returns the correct category names matching seeded data.
    /// </summary>
    [Fact]
    public async Task GetCategories_ReturnsCorrectNames()
    {
        var response = await _client.GetAsync(CategoriesApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);

        var returnedNames = data
            .Select(c => Assert.IsType<JsonElement>(c["name"]).GetString())
            .ToHashSet();

        foreach (var expected in TestCategories)
        {
            Assert.Contains(expected["name"].ToString(), returnedNames);
        }
    }

    /// <summary>
    /// GET /api/categories on an empty database returns an empty list (not 404).
    /// </summary>
    [Fact]
    public async Task GetCategories_EmptyDatabase_ReturnsEmptyList()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TailspinToysContext>();
        db.Games.RemoveRange(db.Games);
        db.Categories.RemoveRange(db.Categories);
        db.SaveChanges();

        var response = await _client.GetAsync(CategoriesApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<object>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.Empty(data);
    }

    /// <summary>
    /// GET /api/categories omits categories that have no associated games.
    /// </summary>
    [Fact]
    public async Task GetCategories_OnlyReturnsUsedCategories()
    {
        var response = await _client.GetAsync(CategoriesApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);

        var returnedNames = data
            .Select(c => Assert.IsType<JsonElement>(c["name"]).GetString())
            .ToHashSet();

        // The unused category must not be present.
        Assert.DoesNotContain(UnusedCategoryName, returnedNames);

        // All used categories must be present.
        foreach (var expected in TestCategories)
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
