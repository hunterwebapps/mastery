using FluentValidation;

namespace Mastery.Application.Features.Recommendations.Commands.GenerateRecommendations;

public sealed class GenerateRecommendationsCommandValidator : AbstractValidator<GenerateRecommendationsCommand>
{
    public GenerateRecommendationsCommandValidator()
    {
        RuleFor(x => x.Context)
            .NotEmpty()
            .WithMessage("Context is required.");
    }
}
