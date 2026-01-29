namespace Mastery.Domain.Common;

public class OwnedEntity : AuditableEntity
{
    [EmbeddingIgnore]
    public string UserId { get; protected set; } = null!;
}
