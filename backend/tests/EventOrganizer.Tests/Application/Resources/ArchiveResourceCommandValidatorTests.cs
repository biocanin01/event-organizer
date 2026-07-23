using EventOrganizer.Application.Commands.ArchiveResource;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class ArchiveResourceCommandValidatorTests
    {
        private readonly ArchiveResourceCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenCommandIsValid_ReturnsNoErrors()
        {
            var command = new ArchiveResourceCommand(Guid.NewGuid());

            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WhenResourceIdIsEmpty_ReturnsError()
        {
            var command = new ArchiveResourceCommand(Guid.Empty);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }
    }
}
