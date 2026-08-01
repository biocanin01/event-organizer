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
                ResourceType.Equipment,
                100m,
                null,
                null,
                3);

            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithEmptyName_IsInvalid()
        {
            var command = new CreateResourceCommand(
                "",
                "4K presentation projector.",
                ResourceType.Equipment,
                100m,
                null,
                null,
                3);

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
                ResourceType.Equipment,
                100m,
                null,
                null,
                3);

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
                ResourceType.Equipment,
                100m,
                null,
                null,
                3);

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
                (ResourceType)99,
                100m,
                null,
                null,
                3);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.Type));
        }

        [Fact]
        public void Validate_WithNegativeCost_IsInvalid()
        {
            var command = new CreateResourceCommand(
                "Projector",
                "4K presentation projector.",
                ResourceType.Equipment,
                -1m,
                null,
                null,
                3);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.Cost));
        }

        [Fact]
        public void Validate_WithNonPositiveCapacity_IsInvalid()
        {
            var command = new CreateResourceCommand(
                "Main Conference Hall",
                "A hall suitable for conferences.",
                ResourceType.Venue,
                500m,
                0,
                "IT",
                3);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.Capacity));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public void Validate_WithQualityScoreOutsideRange_IsInvalid(int qualityScore)
        {
            var command = new CreateResourceCommand(
                "Projector",
                "4K presentation projector.",
                ResourceType.Equipment,
                100m,
                null,
                null,
                qualityScore);

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.QualityScore));
        }
    }
}
