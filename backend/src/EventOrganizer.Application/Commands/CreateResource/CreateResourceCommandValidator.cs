using FluentValidation;

namespace EventOrganizer.Application.Commands.CreateResource
{
    public sealed class CreateResourceCommandValidator
        : AbstractValidator<CreateResourceCommand>
    {
        public CreateResourceCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(command => command.Description)
                .MaximumLength(2000);

            RuleFor(command => command.Type)
                .IsInEnum();
        }
    }
}
