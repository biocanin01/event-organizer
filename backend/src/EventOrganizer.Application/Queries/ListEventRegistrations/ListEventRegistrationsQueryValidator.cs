using FluentValidation;

namespace EventOrganizer.Application.Queries.ListEventRegistrations
{
    public sealed class ListEventRegistrationsQueryValidator
        : AbstractValidator<ListEventRegistrationsQuery>
    {
        public ListEventRegistrationsQueryValidator()
        {
            RuleFor(query => query.EventId).NotEmpty();
            RuleFor(query => query.Status).IsInEnum().When(query => query.Status.HasValue);
        }
    }
}
