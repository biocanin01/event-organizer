using EventOrganizer.Domain.Resources;
using FluentValidation;

namespace EventOrganizer.Application.Common.Validation
{
    public interface IResourceDetails
    {
        string Name { get; }
        string Description { get; }
        ResourceType Type { get; }
        decimal Cost { get; }
        int QualityScore { get; }
        int? Capacity { get; }
        string? ExpertiseArea { get; }
        string? ProviderName { get; }
        int? SupportedCapacity { get; }
        string? ServiceArea { get; }
        bool? IncludesTechnicalSupport { get; }
        string? ContentsSummary { get; }
    }

    internal static class ResourceDetailsValidationRules
    {
        public static void AddResourceDetailsRules<T>(this AbstractValidator<T> validator)
            where T : IResourceDetails
        {
            validator.RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(200);

            validator.RuleFor(command => command.Description)
                .NotNull()
                .MaximumLength(2000);

            validator.RuleFor(command => command.Type)
                .IsInEnum();

            validator.RuleFor(command => command.Cost)
                .GreaterThanOrEqualTo(0);

            validator.RuleFor(command => command.QualityScore)
                .InclusiveBetween(1, 5);

            validator.RuleFor(command => command.Capacity)
                .NotNull()
                .When(command => command.Type == ResourceType.Venue);

            validator.RuleFor(command => command.Capacity)
                .GreaterThan(0)
                .When(command => command.Capacity.HasValue);

            validator.RuleFor(command => command.ExpertiseArea)
                .NotEmpty()
                .MaximumLength(100)
                .When(command => command.Type == ResourceType.Speaker);

            validator.RuleFor(command => command.ProviderName)
                .NotEmpty()
                .MaximumLength(200)
                .When(command => command.Type == ResourceType.EquipmentPackage);

            validator.RuleFor(command => command.SupportedCapacity)
                .NotNull()
                .When(command => command.Type == ResourceType.EquipmentPackage);

            validator.RuleFor(command => command.SupportedCapacity)
                .GreaterThan(0)
                .When(command => command.SupportedCapacity.HasValue);

            validator.RuleFor(command => command.ServiceArea)
                .MaximumLength(100);

            validator.RuleFor(command => command.ServiceArea)
                .NotEmpty()
                .When(command => command.Type == ResourceType.EquipmentPackage);

            validator.RuleFor(command => command.IncludesTechnicalSupport)
                .NotNull()
                .When(command => command.Type == ResourceType.EquipmentPackage);

            validator.RuleFor(command => command.ContentsSummary)
                .NotEmpty()
                .MaximumLength(1000)
                .When(command => command.Type == ResourceType.EquipmentPackage);
        }
    }
}
