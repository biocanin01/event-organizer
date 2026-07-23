using FluentValidation;

namespace EventOrganizer.Application.Commands.ArchiveResource
{
    public sealed class ArchiveResourceCommandValidator
        : AbstractValidator<ArchiveResourceCommand>
    {
        public ArchiveResourceCommandValidator()
        {
            RuleFor(command => command.ResourceId)
                .NotEmpty();
        }
    }
}
