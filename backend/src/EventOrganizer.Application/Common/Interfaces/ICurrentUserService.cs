namespace EventOrganizer.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }

        string? Email { get; }

        bool IsAuthenticated { get; }

        IReadOnlyCollection<string> Roles { get; }

        bool IsInRole(string role);
    }
}