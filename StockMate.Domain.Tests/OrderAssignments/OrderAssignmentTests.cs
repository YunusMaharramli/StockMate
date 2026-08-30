using StockMate.Domain.Entities;

namespace StockMate.Domain.Tests.OrderAssignments;

public class OrderAssignmentTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveAssignment()
    {
        var assignment = CreateAssignment();
        Assert.NotEqual(Guid.Empty, assignment.Id);
        Assert.True(assignment.IsActive);
        Assert.Null(assignment.ReleasedAtUtc);
    }

    [Fact]
    public void Release_ShouldDeactivateAssignment()
    {
        var assignment = CreateAssignment();
        assignment.Release();
        Assert.False(assignment.IsActive);
        Assert.NotNull(assignment.ReleasedAtUtc);
    }

    [Fact]
    public void Release_ShouldThrow_WhenAlreadyReleased()
    {
        var assignment = CreateAssignment();
        assignment.Release();
        Assert.Throws<InvalidOperationException>(assignment.Release);
    }

    private static OrderAssignment CreateAssignment()
        => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
}
