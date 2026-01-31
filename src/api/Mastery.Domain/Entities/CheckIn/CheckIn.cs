using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.Entities.CheckIn;

/// <summary>
/// Represents a daily check-in in the Mastery system.
/// Check-ins are the primary "sensors" for the daily control loop,
/// capturing energy, mode, top priority, and end-of-day reflections.
/// One morning and one evening check-in per user per date.
/// </summary>
public sealed class CheckIn : OwnedEntity, IAggregateRoot
{
    /// <summary>
    /// The user-local date this check-in is for.
    /// </summary>
    public DateOnly CheckInDate { get; private set; }

    /// <summary>
    /// Whether this is a morning or evening check-in.
    /// </summary>
    public CheckInType Type { get; private set; }

    /// <summary>
    /// Current lifecycle status (Draft, Completed, Skipped).
    /// </summary>
    public CheckInStatus Status { get; private set; }

    /// <summary>
    /// When the check-in was completed (submitted).
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    #region Morning Fields

    /// <summary>
    /// Morning energy level (1-5). Required for morning check-ins.
    /// </summary>
    public int? EnergyLevel { get; private set; }

    /// <summary>
    /// Day mode selection (Full/Maintenance/Minimum). Required for morning check-ins.
    /// </summary>
    public HabitMode? SelectedMode { get; private set; }

    /// <summary>
    /// Type of Top 1 priority selected.
    /// </summary>
    public Top1Type? Top1Type { get; private set; }

    /// <summary>
    /// Entity ID of the Top 1 (Task, Habit, or Project ID).
    /// </summary>
    public Guid? Top1EntityId { get; private set; }

    /// <summary>
    /// Free-text Top 1 description (when Top1Type is FreeText).
    /// </summary>
    public string? Top1FreeText { get; private set; }

    /// <summary>
    /// Optional morning intention.
    /// </summary>
    public string? Intention { get; private set; }

    #endregion

    #region Evening Fields

    /// <summary>
    /// Evening energy level (1-5). Optional for evening check-ins.
    /// </summary>
    public int? EnergyLevelPm { get; private set; }

    /// <summary>
    /// Stress level (1-5). Optional for evening check-ins.
    /// </summary>
    public int? StressLevel { get; private set; }

    /// <summary>
    /// End-of-day reflection text.
    /// </summary>
    public string? Reflection { get; private set; }

    /// <summary>
    /// Category of the biggest blocker today.
    /// </summary>
    public BlockerCategory? BlockerCategory { get; private set; }

    /// <summary>
    /// Additional detail about the blocker.
    /// </summary>
    public string? BlockerNote { get; private set; }

    /// <summary>
    /// Whether the morning's Top 1 was completed.
    /// </summary>
    public bool? Top1Completed { get; private set; }

    #endregion

    private CheckIn() { } // EF Core

    /// <summary>
    /// Creates a morning check-in with energy, mode, and Top 1 selection.
    /// </summary>
    public static CheckIn CreateMorning(
        string userId,
        DateOnly checkInDate,
        int energyLevel,
        HabitMode selectedMode,
        Top1Type? top1Type = null,
        Guid? top1EntityId = null,
        string? top1FreeText = null,
        string? intention = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        if (checkInDate == default)
            throw new DomainException("CheckInDate is required.");

        if (energyLevel < 1 || energyLevel > 5)
            throw new DomainException("Energy level must be between 1 and 5.");

        if (top1Type == Enums.Top1Type.FreeText && string.IsNullOrWhiteSpace(top1FreeText))
            throw new DomainException("Top 1 free text is required when type is FreeText.");

        if (top1Type is Enums.Top1Type.Task or Enums.Top1Type.Habit or Enums.Top1Type.Project
            && (!top1EntityId.HasValue || top1EntityId == Guid.Empty))
            throw new DomainException("Top 1 entity ID is required for Task, Habit, or Project type.");

        if (top1FreeText?.Length > 200)
            throw new DomainException("Top 1 free text cannot exceed 200 characters.");

        if (intention?.Length > 500)
            throw new DomainException("Intention cannot exceed 500 characters.");

        var checkIn = new CheckIn
        {
            UserId = userId,
            CheckInDate = checkInDate,
            Type = CheckInType.Morning,
            Status = CheckInStatus.Completed,
            CompletedAt = DateTime.UtcNow,
            EnergyLevel = energyLevel,
            SelectedMode = selectedMode,
            Top1Type = top1Type,
            Top1EntityId = top1EntityId,
            Top1FreeText = top1FreeText,
            Intention = intention
        };

        checkIn.AddDomainEvent(new MorningCheckInSubmittedEvent(
            checkIn.Id,
            userId,
            checkInDate,
            energyLevel,
            selectedMode));

        return checkIn;
    }

    /// <summary>
    /// Creates an evening check-in with completion review, blocker, and reflection.
    /// </summary>
    public static CheckIn CreateEvening(
        string userId,
        DateOnly checkInDate,
        bool? top1Completed = null,
        int? energyLevelPm = null,
        int? stressLevel = null,
        string? reflection = null,
        BlockerCategory? blockerCategory = null,
        string? blockerNote = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        if (checkInDate == default)
            throw new DomainException("CheckInDate is required.");

        if (energyLevelPm.HasValue && (energyLevelPm < 1 || energyLevelPm > 5))
            throw new DomainException("Evening energy level must be between 1 and 5.");

        if (stressLevel.HasValue && (stressLevel < 1 || stressLevel > 5))
            throw new DomainException("Stress level must be between 1 and 5.");

        if (reflection?.Length > 1000)
            throw new DomainException("Reflection cannot exceed 1000 characters.");

        if (blockerNote?.Length > 500)
            throw new DomainException("Blocker note cannot exceed 500 characters.");

        var checkIn = new CheckIn
        {
            UserId = userId,
            CheckInDate = checkInDate,
            Type = CheckInType.Evening,
            Status = CheckInStatus.Completed,
            CompletedAt = DateTime.UtcNow,
            Top1Completed = top1Completed,
            EnergyLevelPm = energyLevelPm,
            StressLevel = stressLevel,
            Reflection = reflection,
            BlockerCategory = blockerCategory,
            BlockerNote = blockerNote
        };

        checkIn.AddDomainEvent(new EveningCheckInSubmittedEvent(
            checkIn.Id,
            userId,
            checkInDate,
            top1Completed));

        return checkIn;
    }

    /// <summary>
    /// Creates a skipped check-in placeholder.
    /// </summary>
    public static CheckIn CreateSkipped(
        string userId,
        DateOnly checkInDate,
        CheckInType type)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        var checkIn = new CheckIn
        {
            UserId = userId,
            CheckInDate = checkInDate,
            Type = type,
            Status = CheckInStatus.Skipped,
            CompletedAt = DateTime.UtcNow
        };

        checkIn.AddDomainEvent(new CheckInSkippedEvent(
            checkIn.Id,
            userId,
            checkInDate,
            type));

        return checkIn;
    }

    #region Updates

    /// <summary>
    /// Updates a morning check-in's fields.
    /// </summary>
    public void UpdateMorning(
        int? energyLevel = null,
        HabitMode? selectedMode = null,
        Top1Type? top1Type = null,
        Guid? top1EntityId = null,
        string? top1FreeText = null,
        string? intention = null)
    {
        if (Type != CheckInType.Morning)
            throw new DomainException("Cannot update morning fields on an evening check-in.");

        if (energyLevel.HasValue)
        {
            if (energyLevel < 1 || energyLevel > 5)
                throw new DomainException("Energy level must be between 1 and 5.");
            EnergyLevel = energyLevel;
        }

        if (selectedMode.HasValue)
            SelectedMode = selectedMode;

        if (top1Type.HasValue)
        {
            Top1Type = top1Type;
            Top1EntityId = top1EntityId;
            Top1FreeText = top1FreeText;
        }

        if (intention != null)
        {
            if (intention.Length > 500)
                throw new DomainException("Intention cannot exceed 500 characters.");
            Intention = intention;
        }

        AddDomainEvent(new CheckInUpdatedEvent(Id, "Morning"));
    }

    /// <summary>
    /// Updates an evening check-in's fields.
    /// </summary>
    public void UpdateEvening(
        bool? top1Completed = null,
        int? energyLevelPm = null,
        int? stressLevel = null,
        string? reflection = null,
        BlockerCategory? blockerCategory = null,
        string? blockerNote = null)
    {
        if (Type != CheckInType.Evening)
            throw new DomainException("Cannot update evening fields on a morning check-in.");

        if (top1Completed.HasValue)
            Top1Completed = top1Completed;

        if (energyLevelPm.HasValue)
        {
            if (energyLevelPm < 1 || energyLevelPm > 5)
                throw new DomainException("Evening energy level must be between 1 and 5.");
            EnergyLevelPm = energyLevelPm;
        }

        if (stressLevel.HasValue)
        {
            if (stressLevel < 1 || stressLevel > 5)
                throw new DomainException("Stress level must be between 1 and 5.");
            StressLevel = stressLevel;
        }

        if (reflection != null)
        {
            if (reflection.Length > 1000)
                throw new DomainException("Reflection cannot exceed 1000 characters.");
            Reflection = reflection;
        }

        if (blockerCategory.HasValue)
            BlockerCategory = blockerCategory;

        if (blockerNote != null)
        {
            if (blockerNote.Length > 500)
                throw new DomainException("Blocker note cannot exceed 500 characters.");
            BlockerNote = blockerNote;
        }

        AddDomainEvent(new CheckInUpdatedEvent(Id, "Evening"));
    }

    #endregion
}
