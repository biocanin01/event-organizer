using EventOrganizer.Application.Commands.MarkResourceUnavailable;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class MarkResourceUnavailableCommandValidatorTests
    {
        private readonly MarkResourceUnavailableCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenCommandIsValid_ReturnsNoErrors()
        {
            var command = new MarkResourceUnavailableCommand(Guid.NewGuid());

            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WhenResourceIdIsEmpty_ReturnsError()
        {
            var command = new MarkResourceUnavailableCommand(Guid.Empty);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }
    }
}
