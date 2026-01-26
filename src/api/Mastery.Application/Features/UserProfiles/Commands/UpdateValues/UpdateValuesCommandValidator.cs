using FluentValidation;

namespace Mastery.Application.Features.UserProfiles.Commands.UpdateValues;

public sealed class UpdateValuesCommandValidator : AbstractValidator<UpdateValuesCommand>
{
    public UpdateValuesCommandValidator()
    {
        RuleFor(x => x.Values)
            .NotEmpty().WithMessage("At least one value is required.");

        RuleForEach(x => x.Values)
            .ChildRules(value =>
            {
                value.RuleFor(v => v.Label)
                    .NotEmpty().WithMessage("Value label is required.")
                    .MaximumLength(100).WithMessage("Value label cannot exceed 100 characters.");

                value.RuleFor(v => v.Rank)
                    .GreaterThan(0).WithMessage("Value rank must be greater than 0.");

                value.RuleFor(v => v.Key)
                    .MaximumLength(50).WithMessage("Value key cannot exceed 50 characters.")
                    .When(v => v.Key != null);

                value.RuleFor(v => v.Notes)
                    .MaximumLength(500).WithMessage("Value notes cannot exceed 500 characters.")
                    .When(v => v.Notes != null);
            });
    }
}
