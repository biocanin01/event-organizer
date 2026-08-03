using FluentValidation;

namespace EventOrganizer.Application.Queries.GetUserById
{
    public sealed class GetUserByIdQueryValidator
        : AbstractValidator<GetUserByIdQuery>
    {
        public GetUserByIdQueryValidator()
        {
            RuleFor(query => query.UserId)
                .NotEmpty();
        }
    }
}
