using FluentValidation;

namespace EventOrganizer.Application.Commands.UpdateResource
{
    public sealed class UpdateResourceCommandValidator
        : AbstractValidator<UpdateResourceCommand>
    {
        public UpdateResourceCommandValidator()
        {
            RuleFor(command => command.ResourceId)
                .NotEmpty();

            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(command => command.Description)
                .MaximumLength(2000);

            RuleFor(command => command.Cost)
                .GreaterThanOrEqualTo(0);

            RuleFor(command => command.Capacity)
                .GreaterThan(0)
                .When(command => command.Capacity.HasValue);

            RuleFor(command => command.Area)
                .MaximumLength(100);

            RuleFor(command => command.QualityScore)
                .InclusiveBetween(1, 5);
        }
    }
}
