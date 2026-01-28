using Mastery.Domain.Entities.CheckIn;

namespace Mastery.Application.Features.CheckIns.Models;

/// <summary>
/// Mapping extensions from domain entities to DTOs.
/// </summary>
public static class CheckInMappings
{
    public static CheckInDto ToDto(this CheckIn checkIn) => new(
        Id: checkIn.Id,
        UserId: checkIn.UserId,
        CheckInDate: checkIn.CheckInDate.ToString("yyyy-MM-dd"),
        Type: checkIn.Type.ToString(),
        Status: checkIn.Status.ToString(),
        CompletedAt: checkIn.CompletedAt,
        EnergyLevel: checkIn.EnergyLevel,
        SelectedMode: checkIn.SelectedMode?.ToString(),
        Top1Type: checkIn.Top1Type?.ToString(),
        Top1EntityId: checkIn.Top1EntityId,
        Top1FreeText: checkIn.Top1FreeText,
        Intention: checkIn.Intention,
        EnergyLevelPm: checkIn.EnergyLevelPm,
        StressLevel: checkIn.StressLevel,
        Reflection: checkIn.Reflection,
        BlockerCategory: checkIn.BlockerCategory?.ToString(),
        BlockerNote: checkIn.BlockerNote,
        Top1Completed: checkIn.Top1Completed,
        CreatedAt: checkIn.CreatedAt,
        ModifiedAt: checkIn.ModifiedAt);

    public static CheckInSummaryDto ToSummaryDto(this CheckIn checkIn) => new(
        Id: checkIn.Id,
        CheckInDate: checkIn.CheckInDate.ToString("yyyy-MM-dd"),
        Type: checkIn.Type.ToString(),
        Status: checkIn.Status.ToString(),
        EnergyLevel: checkIn.EnergyLevel,
        EnergyLevelPm: checkIn.EnergyLevelPm,
        SelectedMode: checkIn.SelectedMode?.ToString(),
        Top1Completed: checkIn.Top1Completed,
        CompletedAt: checkIn.CompletedAt);
}
