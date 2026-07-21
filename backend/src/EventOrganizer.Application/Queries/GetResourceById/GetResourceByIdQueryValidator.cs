using FluentValidation;

namespace EventOrganizer.Application.Queries.GetResourceById
{
    public sealed class GetResourceByIdQueryValidator
        : AbstractValidator<GetResourceByIdQuery>
    {
        public GetResourceByIdQueryValidator()
        {
            RuleFor(query => query.ResourceId)
                .NotEmpty();
        }
    }
}