using EventOrganizer.Application.Common.Validation;
using FluentValidation;

namespace EventOrganizer.Application.Commands.CreateResource
{
    public sealed class CreateResourceCommandValidator
        : AbstractValidator<CreateResourceCommand>
    {
        public CreateResourceCommandValidator()
        {
            this.AddResourceDetailsRules();
        }
    }
}
