using FluentValidation;

namespace Mastery.Application.Features.UserProfiles.Commands.UpdateRoles;

public sealed class UpdateRolesCommandValidator : AbstractValidator<UpdateRolesCommand>
{
    public UpdateRolesCommandValidator()
    {
        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("At least one role is required.");

        RuleForEach(x => x.Roles)
            .ChildRules(role =>
            {
                role.RuleFor(r => r.Label)
                    .NotEmpty().WithMessage("Role label is required.")
                    .MaximumLength(100).WithMessage("Role label cannot exceed 100 characters.");

                role.RuleFor(r => r.Key)
                    .MaximumLength(50).WithMessage("Role key cannot exceed 50 characters.")
                    .When(r => r.Key != null);

                role.RuleFor(r => r.Rank)
                    .GreaterThan(0).WithMessage("Role rank must be greater than 0.");

                role.RuleFor(r => r.SeasonPriority)
                    .InclusiveBetween(1, 5).WithMessage("Season priority must be between 1 and 5.");

                role.RuleFor(r => r.MinWeeklyMinutes)
                    .GreaterThanOrEqualTo(0).WithMessage("Minimum weekly minutes cannot be negative.");

                role.RuleFor(r => r.TargetWeeklyMinutes)
                    .GreaterThanOrEqualTo(r => r.MinWeeklyMinutes)
                    .WithMessage("Target weekly minutes must be at least the minimum.");

                role.RuleFor(r => r.Status)
                    .Must(s => s == "Active" || s == "Inactive")
                    .WithMessage("Status must be 'Active' or 'Inactive'.");
            });
    }
}
