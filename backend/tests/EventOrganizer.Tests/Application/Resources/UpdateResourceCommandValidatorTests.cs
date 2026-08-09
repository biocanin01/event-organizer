using EventOrganizer.Application.Commands.UpdateResource;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class UpdateResourceCommandValidatorTests
    {
        private readonly UpdateResourceCommandValidator _validator = new();

        [Theory]
        [MemberData(nameof(ValidCommands))]
        public void Validate_WithValidCommand_IsValid(UpdateResourceCommand command)
        {
            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithEmptyResourceId_IsInvalid()
        {
            var command = ValidVenue() with { ResourceId = Guid.Empty };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.ResourceId));
        }

        [Fact]
        public void Validate_WithEmptyName_IsInvalid()
        {
            var command = ValidVenue() with { Name = string.Empty };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.Name));
        }

        [Fact]
        public void Validate_WithMissingVenueCapacity_IsInvalid()
        {
            var command = ValidVenue() with { Capacity = null };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.Capacity));
        }

        [Fact]
        public void Validate_WithMissingSpeakerExpertiseArea_IsInvalid()
        {
            var command = ValidSpeaker() with { ExpertiseArea = "" };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.ExpertiseArea));
        }

        [Fact]
        public void Validate_WithMissingPackageSupportedCapacity_IsInvalid()
        {
            var command = ValidEquipmentPackage() with { SupportedCapacity = null };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.SupportedCapacity));
        }

        [Fact]
        public void Validate_WithNegativeCost_IsInvalid()
        {
            var command = ValidVenue() with { Cost = -1m };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.Cost));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public void Validate_WithQualityScoreOutsideRange_IsInvalid(int qualityScore)
        {
            var command = ValidVenue() with { QualityScore = qualityScore };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(UpdateResourceCommand.QualityScore));
        }

        public static TheoryData<UpdateResourceCommand> ValidCommands()
        {
            return new TheoryData<UpdateResourceCommand>
            {
                ValidVenue(),
                ValidSpeaker(),
                ValidEquipmentPackage(),
            };
        }

        private static UpdateResourceCommand ValidVenue()
        {
            return new UpdateResourceCommand(
                Guid.NewGuid(),
                "Main Conference Hall",
                "A hall suitable for conferences.",
                ResourceType.Venue,
                500m,
                4,
                200,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        private static UpdateResourceCommand ValidSpeaker()
        {
            return new UpdateResourceCommand(
                Guid.NewGuid(),
                "Architecture Speaker",
                "Speaker profile.",
                ResourceType.Speaker,
                300m,
                5,
                null,
                "IT",
                null,
                null,
                null,
                null,
                null);
        }

        private static UpdateResourceCommand ValidEquipmentPackage()
        {
            return new UpdateResourceCommand(
                Guid.NewGuid(),
                "Conference AV package",
                "Audio and video package.",
                ResourceType.EquipmentPackage,
                250m,
                5,
                null,
                null,
                "AV Supplier",
                150,
                "Belgrade",
                true,
                "Projector, microphones and on-site setup.");
        }
    }
}
