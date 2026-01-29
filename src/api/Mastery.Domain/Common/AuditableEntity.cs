namespace Mastery.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    [EmbeddingIgnore]
    public DateTime CreatedAt { get; set; }
    [EmbeddingIgnore]
    public string? CreatedBy { get; set; }
    [EmbeddingIgnore]
    public DateTime? ModifiedAt { get; set; }
    [EmbeddingIgnore]
    public string? ModifiedBy { get; set; }
}
