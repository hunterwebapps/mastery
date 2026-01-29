namespace Mastery.Domain.Common;

/// <summary>
/// Marks a class or property as ignored for embedding purposes.
///
/// When applied to a class: The entity will NOT be queued for embedding on Added or Modified
/// operations (Deleted still creates an outbox entry for vector store cleanup).
///
/// When applied to a property: If only properties with this attribute are modified,
/// the entity will NOT be queued for re-embedding via the Outbox.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
public sealed class EmbeddingIgnoreAttribute : Attribute
{
}
