using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Users.Models;

namespace Mastery.Application.Features.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(string UserId) : IQuery<UserDetailDto?>;
