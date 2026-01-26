using FluentValidation;
using Mastery.Domain.Entities;

namespace Mastery.Application.Features.Seasons.Commands.CreateSeason;

public sealed class CreateSeasonCommandValidator : AbstractValidator<CreateSeasonCommand>
{
    public CreateSeasonCommandValidator()
    {
        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Season label is required.")
            .MaximumLength(100).WithMessage("Season label cannot exceed 100 characters.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Season type is required.")
            .Must(BeValidSeasonType).WithMessage("Invalid season type. Valid types: Sprint, Build, Maintain, Recover, Transition, Explore.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        When(x => x.ExpectedEndDate.HasValue, () =>
        {
            RuleFor(x => x.ExpectedEndDate!.Value)
                .GreaterThan(x => x.StartDate)
                .WithMessage("Expected end date must be after start date.");
        });

        RuleFor(x => x.Intensity)
            .InclusiveBetween(1, 10).WithMessage("Intensity must be between 1 and 10.");

        When(x => x.SuccessStatement != null, () =>
        {
            RuleFor(x => x.SuccessStatement)
                .MaximumLength(500).WithMessage("Success statement cannot exceed 500 characters.");
        });

        When(x => x.NonNegotiables != null, () =>
        {
            RuleForEach(x => x.NonNegotiables)
                .MaximumLength(200).WithMessage("Each non-negotiable cannot exceed 200 characters.");
        });
    }

    private static bool BeValidSeasonType(string type)
    {
        return Enum.TryParse<SeasonType>(type, out _);
    }
}
