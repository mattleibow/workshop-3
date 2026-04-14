// This file contains E2E tests for the game filtering functionality on the home page.
using Microsoft.Playwright;

namespace TailspinToys.E2E;

/// <summary>
/// E2E tests for the category and publisher filter UI on the games list page.
/// </summary>
public class FilterTests : PlaywrightTestBase
{
    /// <summary>
    /// Waits for the Blazor interactive circuit to be established on the home page.
    /// OnAfterRenderAsync sets data-blazor-connected="true" only in the interactive render,
    /// never in the SSR pre-render, so this reliably guards against clicking before the
    /// circuit is ready.
    /// </summary>
    private async Task WaitForInteractiveAsync()
    {
        await Page.GotoAsync("/");
        await Page.WaitForSelectorAsync("[data-blazor-connected='true']", new() { Timeout = 15000 });
    }

    /// <summary>
    /// The filter panel should be visible on the home page.
    /// </summary>
    [Fact]
    public async Task ShouldDisplayFilterPanel()
    {
        await WaitForInteractiveAsync();

        await Expect(Page.GetByTestId("game-filter")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Clicking a category badge should filter the games grid to show only games
    /// in that category, and the badge should appear active (aria-pressed=true).
    /// </summary>
    [Fact]
    public async Task ShouldFilterGamesByCategoryBadge()
    {
        await WaitForInteractiveAsync();

        var initialCount = await Page.GetByTestId("game-card").CountAsync();

        // Click the first category badge
        var firstCategoryBadge = Page.Locator("[data-testid^='filter-category-']").First;
        await Expect(firstCategoryBadge).ToBeVisibleAsync();
        var badgeText = await firstCategoryBadge.InnerTextAsync();
        await firstCategoryBadge.ClickAsync();

        // Badge should be marked active
        await Expect(firstCategoryBadge).ToHaveAttributeAsync("aria-pressed", "true");

        // Games grid should still be visible (possibly with fewer games)
        await Expect(Page.GetByTestId("games-grid")).ToBeVisibleAsync();

        // Each visible game-category badge should match the selected category
        var gameCategoryBadges = Page.GetByTestId("game-category");
        var count = await gameCategoryBadges.CountAsync();
        for (var i = 0; i < count; i++)
        {
            await Expect(gameCategoryBadges.Nth(i)).ToHaveTextAsync(badgeText);
        }
    }

    /// <summary>
    /// Clicking a publisher badge should filter the grid to only show games
    /// from that publisher, and the badge should appear active.
    /// </summary>
    [Fact]
    public async Task ShouldFilterGamesByPublisherBadge()
    {
        await WaitForInteractiveAsync();

        // Click the first publisher badge
        var firstPublisherBadge = Page.Locator("[data-testid^='filter-publisher-']").First;
        await Expect(firstPublisherBadge).ToBeVisibleAsync();
        var badgeText = await firstPublisherBadge.InnerTextAsync();
        await firstPublisherBadge.ClickAsync();

        // Badge should be active
        await Expect(firstPublisherBadge).ToHaveAttributeAsync("aria-pressed", "true");

        // Each visible game-publisher badge should match the selected publisher
        await Expect(Page.GetByTestId("games-grid")).ToBeVisibleAsync();

        var gamePublisherBadges = Page.GetByTestId("game-publisher");
        var count = await gamePublisherBadges.CountAsync();
        for (var i = 0; i < count; i++)
        {
            await Expect(gamePublisherBadges.Nth(i)).ToHaveTextAsync(badgeText);
        }
    }

    /// <summary>
    /// Clicking a category and a publisher badge should narrow results to the intersection.
    /// </summary>
    [Fact]
    public async Task ShouldCombineCategoryAndPublisherFilters()
    {
        await WaitForInteractiveAsync();

        var unfilteredCount = await Page.GetByTestId("game-card").CountAsync();

        // Apply category filter
        var categoryBadge = Page.Locator("[data-testid^='filter-category-']").First;
        await categoryBadge.ClickAsync();
        await Expect(categoryBadge).ToHaveAttributeAsync("aria-pressed", "true");

        // Apply publisher filter on top
        var publisherBadge = Page.Locator("[data-testid^='filter-publisher-']").First;
        await publisherBadge.ClickAsync();
        await Expect(publisherBadge).ToHaveAttributeAsync("aria-pressed", "true");

        // Result should be ≤ number of games from either filter alone
        // (grid is shown or EmptyState is shown — neither is an error)
        var gridVisible = await Page.GetByTestId("games-grid").IsVisibleAsync();
        var emptyVisible = await Page.GetByTestId("empty-state").IsVisibleAsync();
        Assert.True(gridVisible || emptyVisible, "Expected games-grid or empty-state after combined filter");

        if (gridVisible)
        {
            var filteredCount = await Page.GetByTestId("game-card").CountAsync();
            Assert.True(filteredCount <= unfilteredCount);
        }
    }

    /// <summary>
    /// Clicking the "Clear filters" button should restore the full unfiltered game list.
    /// </summary>
    [Fact]
    public async Task ShouldClearFiltersAndRestoreFullList()
    {
        await WaitForInteractiveAsync();

        var unfilteredCount = await Page.GetByTestId("game-card").CountAsync();

        // Apply a filter to trigger the clear button
        var categoryBadge = Page.Locator("[data-testid^='filter-category-']").First;
        await categoryBadge.ClickAsync();
        await Expect(categoryBadge).ToHaveAttributeAsync("aria-pressed", "true");

        // Clear button should now be visible
        var clearButton = Page.GetByTestId("filter-clear");
        await Expect(clearButton).ToBeVisibleAsync();
        await clearButton.ClickAsync();

        // Badge should no longer be active
        await Expect(categoryBadge).ToHaveAttributeAsync("aria-pressed", "false");

        // Clear button should be gone
        await Expect(clearButton).Not.ToBeVisibleAsync();

        // Full game count should be restored
        await Expect(Page.GetByTestId("games-grid")).ToBeVisibleAsync();
        var restoredCount = await Page.GetByTestId("game-card").CountAsync();
        Assert.Equal(unfilteredCount, restoredCount);
    }

    /// <summary>
    /// Clicking an active filter badge a second time should deselect it.
    /// </summary>
    [Fact]
    public async Task ShouldDeselectFilterBadgeOnSecondClick()
    {
        await WaitForInteractiveAsync();

        var unfilteredCount = await Page.GetByTestId("game-card").CountAsync();

        var categoryBadge = Page.Locator("[data-testid^='filter-category-']").First;

        // First click — select
        await categoryBadge.ClickAsync();
        await Expect(categoryBadge).ToHaveAttributeAsync("aria-pressed", "true");

        // Second click — deselect
        await categoryBadge.ClickAsync();
        await Expect(categoryBadge).ToHaveAttributeAsync("aria-pressed", "false");

        // Full list should be restored
        await Expect(Page.GetByTestId("games-grid")).ToBeVisibleAsync();
        var restoredCount = await Page.GetByTestId("game-card").CountAsync();
        Assert.Equal(unfilteredCount, restoredCount);
    }

    private static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);
    private static IPageAssertions Expect(IPage page) => Assertions.Expect(page);
}
