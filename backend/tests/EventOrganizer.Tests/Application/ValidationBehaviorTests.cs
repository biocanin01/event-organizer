using EventOrganizer.Application.Commands.CreateEvent;
using EventOrganizer.Application.Common.Behaviors;
using FluentValidation;
using MediatR;

namespace EventOrganizer.Tests.Application
{
    public sealed class ValidationBehaviorTests
    {
        [Fact]
        public async Task Handle_WhenRequestIsInvalid_ThrowsValidationException()
        {
            var validators = new List<IValidator<CreateEventCommand>>
            {
                new CreateEventCommandValidator(),
            };

            var behavior = new ValidationBehavior<CreateEventCommand, Guid>(validators);

            var invalidCommand = new CreateEventCommand(
                "",
                "Invalid event.",
                new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc),
                0,
                Guid.Empty);

            Task<Guid> Next(CancellationToken cancellationToken)
            {
                return Task.FromResult(Guid.NewGuid());
            }

            await Assert.ThrowsAsync<ValidationException>(() =>
                behavior.Handle(
                    invalidCommand,
                    new RequestHandlerDelegate<Guid>(Next),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenRequestIsValid_CallsNext()
        {
            var validators = new List<IValidator<CreateEventCommand>>
            {
                new CreateEventCommandValidator(),
            };

            var behavior = new ValidationBehavior<CreateEventCommand, Guid>(validators);

            var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);
            var expectedId = Guid.NewGuid();

            var validCommand = new CreateEventCommand(
                "Software Architecture Seminar",
                "Valid event.",
                startsAtUtc,
                startsAtUtc.AddHours(4),
                80,
                Guid.NewGuid());

            Task<Guid> Next(CancellationToken cancellationToken)
            {
                return Task.FromResult(expectedId);
            }

            var result = await behavior.Handle(
                validCommand,
                new RequestHandlerDelegate<Guid>(Next),
                CancellationToken.None);

            Assert.Equal(expectedId, result);
        }
    }
}