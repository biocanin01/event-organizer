using EventOrganizer.Application.Commands.RejectResourceReservation;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class RejectResourceReservationCommandValidatorTests
    {
        private readonly RejectResourceReservationCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenCommandIsValid_ReturnsNoErrors()
        {
            var command = new RejectResourceReservationCommand(Guid.NewGuid());

            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WhenReservationIdIsEmpty_ReturnsError()
        {
            var command = new RejectResourceReservationCommand(Guid.Empty);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }
    }
}
