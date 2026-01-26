using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.UserProfiles.Models;

namespace Mastery.Application.Features.UserProfiles.Commands.UpdateValues;

/// <summary>
/// Updates the current user's values.
/// </summary>
public sealed record UpdateValuesCommand(List<UserValueDto> Values) : ICommand;
