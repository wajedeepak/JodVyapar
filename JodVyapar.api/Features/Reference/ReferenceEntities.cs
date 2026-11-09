using JodVyapar.Api.Infrastructure.Data;

namespace JodVyapar.Api.Features.Reference;

public class State : AuditableEntity
{
    public required string Name { get; set; }

    /// <summary>Two-letter state/UT code, e.g. "MH".</summary>
    public required string Code { get; set; }

    public bool IsUnionTerritory { get; set; }

    public List<City> Cities { get; set; } = [];
}

public class City : AuditableEntity
{
    public Guid StateId { get; set; }
    public State State { get; set; } = null!;

    public required string Name { get; set; }
}

/// <summary>Unit of measure for prices and quantities. Rows are seeded by Database/04_ProductsAndRequirements.sql.</summary>
public class Unit : AuditableEntity
{
    public required string Name { get; set; }
    public required string Symbol { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Two-level tree: top-level categories have no ParentId, subcategories point to their parent.</summary>
public class Category : AuditableEntity
{
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = [];

    public required string Name { get; set; }
    public required string Slug { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
