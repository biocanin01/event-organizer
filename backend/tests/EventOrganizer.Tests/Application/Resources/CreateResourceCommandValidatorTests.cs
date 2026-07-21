using EventOrganizer.Application.Commands.CreateResource;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class CreateResourceCommandValidatorTests
    {
        private readonly CreateResourceCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_IsValid()
        {
            var command = new CreateResourceCommand(
                "Projector",
                "4K presentation projector.",
                ResourceType.Equipment);

            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithEmptyName_IsInvalid()
        {
            var command = new CreateResourceCommand(
                "",
                "4K presentation projector.",
                ResourceType.Equipment);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.Name));
        }

        [Fact]
        public void Validate_WithNameLongerThan200Characters_IsInvalid()
        {
            var command = new CreateResourceCommand(
                new string('a', 201),
                "4K presentation projector.",
                ResourceType.Equipment);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.Name));
        }

        [Fact]
        public void Validate_WithDescriptionLongerThan2000Characters_IsInvalid()
        {
            var command = new CreateResourceCommand(
                "Projector",
                new string('a', 2001),
                ResourceType.Equipment);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.Description));
        }

        [Fact]
        public void Validate_WithUndefinedResourceType_IsInvalid()
        {
            var command = new CreateResourceCommand(
                "Projector",
                "4K presentation projector.",
                (ResourceType)99);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.Type));
        }
    }
}
