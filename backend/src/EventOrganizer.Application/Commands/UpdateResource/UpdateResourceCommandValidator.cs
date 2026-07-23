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
        }
    }
}
