using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.UserProfiles.Models;

namespace Mastery.Application.Features.UserProfiles.Commands.UpdatePreferences;

/// <summary>
/// Updates the current user's preferences.
/// </summary>
public sealed record UpdatePreferencesCommand(PreferencesDto Preferences) : ICommand;
