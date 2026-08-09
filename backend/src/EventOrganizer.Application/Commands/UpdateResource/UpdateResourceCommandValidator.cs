using EventOrganizer.Application.Common.Validation;
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

            this.AddResourceDetailsRules();
        }
    }
}
