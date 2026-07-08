using EventOrganizer.Application.Commands.RegisterUser;

namespace EventOrganizer.Tests.Application.Auth
{
    public sealed class RegisterUserCommandValidatorTests
    {
        private readonly RegisterUserCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_IsValid()
        {
            var command = new RegisterUserCommand(
                "Test User",
                "test.user@example.com",
                "Password1");

            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithEmptyFullName_IsInvalid()
        {
            var command = new RegisterUserCommand(
                "",
                "test.user@example.com",
                "Password1");

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(RegisterUserCommand.FullName));
        }

        [Fact]
        public void Validate_WithInvalidEmail_IsInvalid()
        {
            var command = new RegisterUserCommand(
                "Test User",
                "invalid-email",
                "Password1");

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(RegisterUserCommand.Email));
        }

        [Fact]
        public void Validate_WithShortPassword_IsInvalid()
        {
            var command = new RegisterUserCommand(
                "Test User",
                "test.user@example.com",
                "short");

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(RegisterUserCommand.Password));
        }
    }
}