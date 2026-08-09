using EventOrganizer.Application.Commands.CreateResource;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class CreateResourceCommandValidatorTests
    {
        private readonly CreateResourceCommandValidator _validator = new();

        [Theory]
        [MemberData(nameof(ValidCommands))]
        public void Validate_WithValidCommand_IsValid(CreateResourceCommand command)
        {
            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithUndefinedResourceType_IsInvalid()
        {
            var command = ValidVenue() with { Type = (ResourceType)99 };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.Type));
        }

        [Fact]
        public void Validate_WithMissingVenueCapacity_IsInvalid()
        {
            var command = ValidVenue() with { Capacity = null };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.Capacity));
        }

        [Fact]
        public void Validate_WithMissingSpeakerExpertiseArea_IsInvalid()
        {
            var command = ValidSpeaker() with { ExpertiseArea = "" };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.ExpertiseArea));
        }

        [Fact]
        public void Validate_WithMissingPackageProviderName_IsInvalid()
        {
            var command = ValidEquipmentPackage() with { ProviderName = "" };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.ProviderName));
        }

        [Fact]
        public void Validate_WithMissingPackageTechnicalSupportFlag_IsInvalid()
        {
            var command = ValidEquipmentPackage() with { IncludesTechnicalSupport = null };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.IncludesTechnicalSupport));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_WithMissingPackageContentsSummary_IsInvalid(string? contentsSummary)
        {
            var command = ValidEquipmentPackage() with { ContentsSummary = contentsSummary };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.ContentsSummary));
        }

        [Fact]
        public void Validate_WithNegativeCost_IsInvalid()
        {
            var command = ValidVenue() with { Cost = -1m };

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateResourceCommand.Cost));
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
                error.PropertyName == nameof(CreateResourceCommand.QualityScore));
        }

        public static TheoryData<CreateResourceCommand> ValidCommands()
        {
            return new TheoryData<CreateResourceCommand>
            {
                ValidVenue(),
                ValidSpeaker(),
                ValidEquipmentPackage(),
            };
        }

        private static CreateResourceCommand ValidVenue()
        {
            return new CreateResourceCommand(
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

        private static CreateResourceCommand ValidSpeaker()
        {
            return new CreateResourceCommand(
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

        private static CreateResourceCommand ValidEquipmentPackage()
        {
            return new CreateResourceCommand(
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
