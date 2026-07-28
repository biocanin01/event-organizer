using EventOrganizer.Application.Queries.GetResourceReservationById;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class GetResourceReservationByIdQueryValidatorTests
    {
        private readonly GetResourceReservationByIdQueryValidator _validator = new();

        [Fact]
        public void Validate_WhenQueryIsValid_ReturnsNoErrors()
        {
            var query = new GetResourceReservationByIdQuery(Guid.NewGuid());

            var result = _validator.Validate(query);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WhenReservationIdIsEmpty_ReturnsError()
        {
            var query = new GetResourceReservationByIdQuery(Guid.Empty);

            var result = _validator.Validate(query);

            Assert.False(result.IsValid);
        }
    }
}
