// Tests for the /api/publishers endpoint, verifying list behaviour and response structure.
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

public class TestPublishersRoutes : IDisposable
{
    private readonly string _dbPath;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    // Test data
    private static readonly Dictionary<string, object>[] TestPublishers =
    [
        new() { ["name"] = "DevGames Inc" },
        new() { ["name"] = "Scrum Masters" },
        new() { ["name"] = "Pixel Pushers" }
    ];

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

    private void SeedTestData(TailspinToysContext db)
    {
        var publishers = TestPublishers.Select(p =>
            new Publisher { Name = (string)p["name"] }).ToList();
        db.Publishers.AddRange(publishers);
        db.SaveChanges();
    }

    /// <summary>
    /// Verifies that GET /api/publishers returns HTTP 200 with all seeded publishers.
    /// </summary>
    [Fact]
    public async Task GetPublishers_ReturnsAllPublishers()
    {
        var response = await _client.GetAsync(PublishersApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.Equal(TestPublishers.Length, data.Count);

        for (var i = 0; i < data.Count; i++)
        {
            Assert.Equal(TestPublishers[i]["name"].ToString(), data[i]["name"]?.ToString());
        }
    }

    /// <summary>
    /// Verifies that each publisher entry contains exactly the id and name fields.
    /// </summary>
    [Fact]
    public async Task GetPublishers_ReturnsCorrectStructure()
    {
        var response = await _client.GetAsync(PublishersApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.NotEmpty(data);

        var requiredFields = new[] { "id", "name" };
        foreach (var field in requiredFields)
        {
            Assert.True(data[0].ContainsKey(field), $"Missing field: {field}");
        }
    }

    /// <summary>
    /// Verifies that each publisher has a positive integer id.
    /// </summary>
    [Fact]
    public async Task GetPublishers_IdsArePositiveIntegers()
    {
        var response = await _client.GetAsync(PublishersApiPath);
        var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);

        foreach (var publisher in data)
        {
            var id = Assert.IsType<JsonElement>(publisher["id"]);
            Assert.True(id.GetInt32() > 0, "Publisher id should be a positive integer");
        }
    }

    /// <summary>
    /// Verifies that GET /api/publishers returns an empty list when no publishers exist.
    /// </summary>
    [Fact]
    public async Task GetPublishers_EmptyDatabase_ReturnsEmptyList()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TailspinToysContext>();
        db.Publishers.RemoveRange(db.Publishers);
        db.SaveChanges();

        var response = await _client.GetAsync(PublishersApiPath);
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
            // Best-effort cleanup; ignore if the file is locked or already deleted
        }
    }
}
