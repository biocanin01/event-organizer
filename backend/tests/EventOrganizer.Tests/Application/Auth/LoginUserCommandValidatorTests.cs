using EventOrganizer.Application.Commands.LoginUser;

namespace EventOrganizer.Tests.Application.Auth
{
    public sealed class LoginUserCommandValidatorTests
    {
        private readonly LoginUserCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_IsValid()
        {
            var command = new LoginUserCommand(
                "test.user@example.com",
                "Password1");

            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithInvalidEmail_IsInvalid()
        {
            var command = new LoginUserCommand(
                "invalid-email",
                "Password1");

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(LoginUserCommand.Email));
        }

        [Fact]
        public void Validate_WithEmptyPassword_IsInvalid()
        {
            var command = new LoginUserCommand(
                "test.user@example.com",
                "");

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(LoginUserCommand.Password));
        }
    }
}