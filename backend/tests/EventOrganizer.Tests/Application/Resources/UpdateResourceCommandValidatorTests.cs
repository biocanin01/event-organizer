using EventOrganizer.Application.Commands.UpdateResource;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class UpdateResourceCommandValidatorTests
    {
        private readonly UpdateResourceCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_IsValid()
        {
            var result = _validator.Validate(new UpdateResourceCommand(
                Guid.NewGuid(),
                "Main Conference Hall",
                "A hall suitable for conferences with up to 200 participants.",
                500m,
                200,
                "IT",
                4));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithEmptyResourceId_IsInvalid()
        {
            var result = _validator.Validate(new UpdateResourceCommand(
                Guid.Empty,
                "Main Conference Hall",
                "A hall suitable for conferences with up to 200 participants.",
                500m,
                200,
                "IT",
                4));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.ResourceId));
        }

        [Fact]
        public void Validate_WithEmptyName_IsInvalid()
        {
            var result = _validator.Validate(new UpdateResourceCommand(
                Guid.NewGuid(),
                string.Empty,
                "A hall suitable for conferences with up to 200 participants.",
                500m,
                200,
                "IT",
                4));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.Name));
        }

        [Fact]
        public void Validate_WithNameLongerThan200Characters_IsInvalid()
        {
            var result = _validator.Validate(new UpdateResourceCommand(
                Guid.NewGuid(),
                new string('a', 201),
                "A hall suitable for conferences with up to 200 participants.",
                500m,
                200,
                "IT",
                4));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.Name));
        }

        [Fact]
        public void Validate_WithDescriptionLongerThan2000Characters_IsInvalid()
        {
            var result = _validator.Validate(new UpdateResourceCommand(
                Guid.NewGuid(),
                "Main Conference Hall",
                new string('a', 2001),
                500m,
                200,
                "IT",
                4));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.Description));
        }

        [Fact]
        public void Validate_WithNegativeCost_IsInvalid()
        {
            var result = _validator.Validate(new UpdateResourceCommand(
                Guid.NewGuid(),
                "Main Conference Hall",
                "A hall suitable for conferences.",
                -1m,
                200,
                "IT",
                4));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.Cost));
        }

        [Fact]
        public void Validate_WithNonPositiveCapacity_IsInvalid()
        {
            var result = _validator.Validate(new UpdateResourceCommand(
                Guid.NewGuid(),
                "Main Conference Hall",
                "A hall suitable for conferences.",
                500m,
                0,
                "IT",
                4));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.Capacity));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public void Validate_WithQualityScoreOutsideRange_IsInvalid(int qualityScore)
        {
            var result = _validator.Validate(new UpdateResourceCommand(
                Guid.NewGuid(),
                "Main Conference Hall",
                "A hall suitable for conferences.",
                500m,
                200,
                "IT",
                qualityScore));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.QualityScore));
        }
    }
}
