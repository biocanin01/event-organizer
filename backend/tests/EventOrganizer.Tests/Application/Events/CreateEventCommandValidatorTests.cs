using EventOrganizer.Application.Commands.CreateEvent;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class CreateEventCommandValidatorTests
    {
        private readonly CreateEventCommandValidator _validator = new();

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithNonPositiveRequiredSpeakerCount_IsInvalid(int requiredSpeakerCount)
        {
            var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);
            var command = new CreateEventCommand(
                "Software Architecture Seminar",
                "Seminar about modern web architecture.",
                startsAtUtc,
                startsAtUtc.AddHours(4),
                80,
                1000m,
                "IT",
                requiredSpeakerCount);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateEventCommand.RequiredSpeakerCount));
        }
    }
}
