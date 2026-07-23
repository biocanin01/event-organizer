using EventOrganizer.Application.Commands.CreateResourceReservation;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class CreateResourceReservationCommandValidatorTests
    {
        private readonly CreateResourceReservationCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenCommandIsValid_ReturnsNoErrors()
        {
            var command = CreateValidCommand();

            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("event")]
        [InlineData("resource")]
        public void Validate_WhenRequiredIdIsEmpty_ReturnsError(string emptyId)
        {
            var command = emptyId == "event"
                ? CreateValidCommand(eventId: Guid.Empty)
                : CreateValidCommand(resourceId: Guid.Empty);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_WhenEndIsNotAfterStart_ReturnsError()
        {
            var startsAtUtc = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
            var command = CreateValidCommand(
                startsAtUtc: startsAtUtc,
                endsAtUtc: startsAtUtc);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }

        private static CreateResourceReservationCommand CreateValidCommand(
            Guid? eventId = null,
            Guid? resourceId = null,
            DateTime? startsAtUtc = null,
            DateTime? endsAtUtc = null)
        {
            var resolvedStartsAtUtc = startsAtUtc
                ?? new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

            return new CreateResourceReservationCommand(
                eventId ?? Guid.NewGuid(),
                resourceId ?? Guid.NewGuid(),
                resolvedStartsAtUtc,
                endsAtUtc ?? resolvedStartsAtUtc.AddHours(2));
        }
    }
}