using EventOrganizer.Application.Commands.MarkResourceAvailable;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class MarkResourceAvailableCommandValidatorTests
    {
        private readonly MarkResourceAvailableCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenCommandIsValid_ReturnsNoErrors()
        {
            var command = new MarkResourceAvailableCommand(Guid.NewGuid());

            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WhenResourceIdIsEmpty_ReturnsError()
        {
            var command = new MarkResourceAvailableCommand(Guid.Empty);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }
    }
}
