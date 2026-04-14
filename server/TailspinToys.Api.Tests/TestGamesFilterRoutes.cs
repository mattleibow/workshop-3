// This file contains integration tests for the filtering query parameters on the games API.
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
/// Integration tests for category and publisher filtering on GET /api/games.
/// </summary>
public class TestGamesFilterRoutes : IDisposable
{
    private readonly string _dbPath;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    // Two publishers
    private static readonly string PublisherA = "DevGames Inc";
    private static readonly string PublisherB = "Scrum Masters";

    // Two categories
    private static readonly string CategoryStrategy = "Strategy";
    private static readonly string CategoryCard = "Card Game";

    private const string GamesApiPath = "/api/games";

    // IDs are resolved after seeding
    private int _publisherAId;
    private int _publisherBId;
    private int _categoryStrategyId;
    private int _categoryCardId;

    public TestGamesFilterRoutes()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_{Guid.NewGuid()}.db");
        var connectionString = $"Data Source={_dbPath}";

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, config) =>
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

    private void SeedTestData(TailspinToysContext db)
    {
        var pubA = new Publisher { Name = PublisherA };
        var pubB = new Publisher { Name = PublisherB };
        db.Publishers.AddRange(pubA, pubB);

        var catS = new Category { Name = CategoryStrategy };
        var catC = new Category { Name = CategoryCard };
        db.Categories.AddRange(catS, catC);
        db.SaveChanges();

        _publisherAId     = pubA.Id;
        _publisherBId     = pubB.Id;
        _categoryStrategyId = catS.Id;
        _categoryCardId     = catC.Id;

        db.Games.AddRange(
            // publisher A, strategy
            new Game { Title = "Pipeline Panic",   Description = "Build your DevOps pipeline", StarRating = 4.5, Publisher = pubA, Category = catS },
            // publisher A, card
            new Game { Title = "Standup Shuffle",  Description = "Daily standup card game",    StarRating = 3.8, Publisher = pubA, Category = catC },
            // publisher B, strategy
            new Game { Title = "Sprint Sprint Sprint", Description = "Race through your backlog", StarRating = 4.2, Publisher = pubB, Category = catS },
            // publisher B, card
            new Game { Title = "Agile Adventures", Description = "Navigate sprints",            StarRating = 4.0, Publisher = pubB, Category = catC }
        );
        db.SaveChanges();
    }

    /// <summary>
    /// No filter params — returns all four games.
    /// </summary>
    [Fact]
    public async Task GetGames_NoFilter_ReturnsAllGames()
    {
        var response = await _client.GetAsync(GamesApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.Equal(4, data.Count);
    }

    /// <summary>
    /// Filter by categoryId — returns only games in that category.
    /// </summary>
    [Fact]
    public async Task GetGames_FilterByCategoryId_ReturnsMatchingGames()
    {
        var response = await _client.GetAsync($"{GamesApiPath}?categoryId={_categoryStrategyId}");
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.Equal(2, data.Count);

        foreach (var game in data)
        {
            var category = Assert.IsType<JsonElement>(game["category"]);
            Assert.Equal(CategoryStrategy, category.GetProperty("name").GetString());
        }
    }

    /// <summary>
    /// Filter by publisherId — returns only games from that publisher.
    /// </summary>
    [Fact]
    public async Task GetGames_FilterByPublisherId_ReturnsMatchingGames()
    {
        var response = await _client.GetAsync($"{GamesApiPath}?publisherId={_publisherAId}");
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.Equal(2, data.Count);

        foreach (var game in data)
        {
            var publisher = Assert.IsType<JsonElement>(game["publisher"]);
            Assert.Equal(PublisherA, publisher.GetProperty("name").GetString());
        }
    }

    /// <summary>
    /// Filter by both categoryId and publisherId — returns only the game matching both.
    /// </summary>
    [Fact]
    public async Task GetGames_FilterByCategoryAndPublisher_ReturnsIntersection()
    {
        var response = await _client.GetAsync($"{GamesApiPath}?categoryId={_categoryStrategyId}&publisherId={_publisherAId}");
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.Single(data);
        Assert.Equal("Pipeline Panic", data[0]["title"]?.ToString());
    }

    /// <summary>
    /// Filter with a categoryId that matches no games returns an empty list.
    /// </summary>
    [Fact]
    public async Task GetGames_FilterByUnknownCategoryId_ReturnsEmptyList()
    {
        var response = await _client.GetAsync($"{GamesApiPath}?categoryId=9999");
        var data = await response.Content.ReadFromJsonAsync<List<object>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.Empty(data);
    }

    /// <summary>
    /// Filter with a publisherId that matches no games returns an empty list.
    /// </summary>
    [Fact]
    public async Task GetGames_FilterByUnknownPublisherId_ReturnsEmptyList()
    {
        var response = await _client.GetAsync($"{GamesApiPath}?publisherId=9999");
        var data = await response.Content.ReadFromJsonAsync<List<object>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.Empty(data);
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
