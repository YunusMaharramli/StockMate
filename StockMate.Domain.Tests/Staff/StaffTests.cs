using StockMate.Domain.Entities;
using StockMate.Domain.Enums;

namespace StockMate.Domain.Tests.Staff;

public class StaffTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveWorker()
    {
        var staff = CreateWorker();

        Assert.NotEqual(Guid.Empty, staff.Id);
        Assert.Equal(123456789, staff.TelegramUserId);
        Assert.Equal("Əli", staff.FirstName);
        Assert.Equal("Məmmədov", staff.LastName);
        Assert.Equal("ali", staff.Username);
        Assert.Equal(StaffRole.Worker, staff.Role);
        Assert.True(staff.IsActive);
    }

    [Fact]
    public void Constructor_ShouldRemoveAtSignFromUsername()
    {
        var staff = new StockMate.Domain.Entities.Staff(
            telegramUserId: 123456789,
            firstName: "Əli",
            lastName: null,
            username: "@ali",
            role: StaffRole.Worker);

        Assert.Equal("ali", staff.Username);
    }

    [Fact]
    public void Deactivate_ShouldMakeStaffInactive()
    {
        var staff = CreateWorker();

        staff.Deactivate();

        Assert.False(staff.IsActive);
    }

    [Fact]
    public void Activate_ShouldMakeStaffActive()
    {
        var staff = CreateWorker();

        staff.Deactivate();
        staff.Activate();

        Assert.True(staff.IsActive);
    }

    private static StockMate.Domain.Entities.Staff CreateWorker()
    {
        return new StockMate.Domain.Entities.Staff(
            telegramUserId: 123456789,
            firstName: "Əli",
            lastName: "Məmmədov",
            username: "ali",
            role: StaffRole.Worker);
    }
}