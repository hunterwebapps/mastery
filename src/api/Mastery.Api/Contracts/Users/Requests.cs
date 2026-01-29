namespace Mastery.Api.Contracts.Users;

public record UpdateUserRolesRequest(List<string> Roles);

public record SetUserDisabledRequest(bool Disabled);
