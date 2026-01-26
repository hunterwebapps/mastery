using FluentValidation;

namespace Mastery.Application.Features.Seasons.Commands.EndSeason;

public sealed class EndSeasonCommandValidator : AbstractValidator<EndSeasonCommand>
{
    public EndSeasonCommandValidator()
    {
        RuleFor(x => x.SeasonId)
            .NotEmpty().WithMessage("Season ID is required.");

        RuleFor(x => x.Outcome)
            .MaximumLength(2000).WithMessage("Outcome cannot exceed 2000 characters.")
            .When(x => x.Outcome != null);
    }
}
