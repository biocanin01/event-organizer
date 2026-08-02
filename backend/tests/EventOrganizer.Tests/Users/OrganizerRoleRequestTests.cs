using EventOrganizer.Domain.Users;

namespace EventOrganizer.Tests.Users;

public sealed class OrganizerRoleRequestTests
{
    private static readonly DateTime SubmittedAtUtc =
        new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidData_CreatesPendingRequest()
    {
        var userId = Guid.NewGuid();

        var request = OrganizerRoleRequest.Create(
            userId,
            "  I would like to organize technology events.  ",
            SubmittedAtUtc);

        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal(userId, request.UserId);
        Assert.Equal(
            "I would like to organize technology events.",
            request.Motivation);
        Assert.Equal(OrganizerRoleRequestStatus.Pending, request.Status);
        Assert.Equal(SubmittedAtUtc, request.SubmittedAtUtc);
        Assert.Null(request.ReviewedByAdminUserId);
        Assert.Null(request.DecisionReason);
        Assert.Null(request.ReviewedAtUtc);
        Assert.Null(request.WithdrawnAtUtc);
        Assert.Equal(1, request.Version);
    }

    [Fact]
    public void Create_WhenUserIdIsEmpty_Throws()
    {
        var act = () => OrganizerRoleRequest.Create(
            Guid.Empty,
            "Valid motivation",
            SubmittedAtUtc);

        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenMotivationIsEmpty_Throws(string motivation)
    {
        var act = () => OrganizerRoleRequest.Create(
            Guid.NewGuid(),
            motivation,
            SubmittedAtUtc);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenMotivationIsTooLong_Throws()
    {
        var motivation = new string(
            'a',
            OrganizerRoleRequest.MaxMotivationLength + 1);

        var act = () => OrganizerRoleRequest.Create(
            Guid.NewGuid(),
            motivation,
            SubmittedAtUtc);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Approve_WhenRequestIsPending_ApprovesRequest()
    {
        var request = CreateRequest();
        var adminUserId = Guid.NewGuid();
        var reviewedAtUtc = SubmittedAtUtc.AddHours(1);

        request.Approve(adminUserId, reviewedAtUtc);

        Assert.Equal(OrganizerRoleRequestStatus.Approved, request.Status);
        Assert.Equal(adminUserId, request.ReviewedByAdminUserId);
        Assert.Equal(reviewedAtUtc, request.ReviewedAtUtc);
        Assert.Null(request.DecisionReason);
        Assert.Equal(2, request.Version);
    }

    [Fact]
    public void Reject_WhenRequestIsPending_RejectsRequest()
    {
        var request = CreateRequest();
        var adminUserId = Guid.NewGuid();
        var reviewedAtUtc = SubmittedAtUtc.AddHours(1);

        request.Reject(
            adminUserId,
            "  More event-planning experience is required.  ",
            reviewedAtUtc);

        Assert.Equal(OrganizerRoleRequestStatus.Rejected, request.Status);
        Assert.Equal(adminUserId, request.ReviewedByAdminUserId);
        Assert.Equal(
            "More event-planning experience is required.",
            request.DecisionReason);
        Assert.Equal(reviewedAtUtc, request.ReviewedAtUtc);
        Assert.Equal(2, request.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WhenDecisionReasonIsEmpty_Throws(string decisionReason)
    {
        var request = CreateRequest();

        var act = () => request.Reject(
            Guid.NewGuid(),
            decisionReason,
            SubmittedAtUtc.AddHours(1));

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Reject_WhenDecisionReasonIsTooLong_Throws()
    {
        var request = CreateRequest();
        var decisionReason = new string(
            'a',
            OrganizerRoleRequest.MaxDecisionReasonLength + 1);

        var act = () => request.Reject(
            Guid.NewGuid(),
            decisionReason,
            SubmittedAtUtc.AddHours(1));

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Withdraw_WhenRequestIsPending_WithdrawsRequest()
    {
        var request = CreateRequest();
        var withdrawnAtUtc = SubmittedAtUtc.AddMinutes(30);

        request.Withdraw(withdrawnAtUtc);

        Assert.Equal(OrganizerRoleRequestStatus.Withdrawn, request.Status);
        Assert.Equal(withdrawnAtUtc, request.WithdrawnAtUtc);
        Assert.Equal(2, request.Version);
    }

    [Fact]
    public void Approve_WhenAdminUserIdIsEmpty_Throws()
    {
        var request = CreateRequest();

        var act = () => request.Approve(
            Guid.Empty,
            SubmittedAtUtc.AddHours(1));

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Approve_WhenReviewTimeIsBeforeSubmission_Throws()
    {
        var request = CreateRequest();

        var act = () => request.Approve(
            Guid.NewGuid(),
            SubmittedAtUtc.AddTicks(-1));

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Approve_WhenRequestIsNotPending_Throws()
    {
        var request = CreateRequest();
        request.Withdraw(SubmittedAtUtc.AddMinutes(1));

        var act = () => request.Approve(
            Guid.NewGuid(),
            SubmittedAtUtc.AddHours(1));

        Assert.Throws<InvalidOperationException>(act);
    }

    private static OrganizerRoleRequest CreateRequest()
    {
        return OrganizerRoleRequest.Create(
            Guid.NewGuid(),
            "I would like to organize technology events.",
            SubmittedAtUtc);
    }
}
