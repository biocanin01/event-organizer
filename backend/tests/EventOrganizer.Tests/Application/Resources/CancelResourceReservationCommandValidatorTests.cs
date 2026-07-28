using EventOrganizer.Application.Commands.CancelResourceReservation;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class CancelResourceReservationCommandValidatorTests
    {
        private readonly CancelResourceReservationCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenCommandIsValid_ReturnsNoErrors()
        {
            var command = new CancelResourceReservationCommand(Guid.NewGuid());

            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WhenReservationIdIsEmpty_ReturnsError()
        {
            var command = new CancelResourceReservationCommand(Guid.Empty);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }
    }
}
