using EventOrganizer.Application.Queries.GetResourceById;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class GetResourceByIdQueryValidatorTests
    {
        private readonly GetResourceByIdQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidResourceId_IsValid()
        {
            var result = _validator.Validate(
                new GetResourceByIdQuery(Guid.NewGuid()));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithEmptyResourceId_IsInvalid()
        {
            var result = _validator.Validate(
                new GetResourceByIdQuery(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(GetResourceByIdQuery.ResourceId));
        }
    }
}