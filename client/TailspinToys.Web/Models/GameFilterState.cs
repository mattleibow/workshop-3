// This file defines the filter state model used by the GameFilter component.
namespace TailspinToys.Web.Models;

/// <summary>
/// Holds the currently selected category and publisher filter values.
/// Null means no filter is applied for that dimension.
/// </summary>
public class GameFilterState
{
    /// <summary>The selected category ID, or null if no category filter is active.</summary>
    public int? CategoryId { get; set; }

    /// <summary>The selected publisher ID, or null if no publisher filter is active.</summary>
    public int? PublisherId { get; set; }

    /// <summary>Returns true when at least one filter is active.</summary>
    public bool HasActiveFilters => CategoryId.HasValue || PublisherId.HasValue;
}
