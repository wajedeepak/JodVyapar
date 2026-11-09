namespace JodVyapar.Api.Infrastructure.Data;

/// <summary>CreatedAt/UpdatedAt (UTC) are stamped automatically by <see cref="AppDbContext"/>.</summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

/// <summary>Deleting an entity with this interface sets IsDeleted instead of removing the row.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}

public abstract class AuditableEntity : IAuditable
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
