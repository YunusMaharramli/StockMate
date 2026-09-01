using StockMate.Domain.Entities;

namespace StockMate.Domain.Tests.TelegramInvites;

public sealed class TelegramInviteTests
{
    [Fact]
    public void Constructor_ShouldCreateUnusedInvite()
    {
        var invite = new TelegramInvite("secure-token");

        Assert.NotEqual(Guid.Empty, invite.Id);
        Assert.Equal("secure-token", invite.Token);
        Assert.False(invite.IsUsed);
        Assert.Null(invite.UsedByTelegramUserId);
        Assert.Null(invite.UsedAtUtc);
    }

    [Fact]
    public void Use_ShouldBindInviteToTelegramUser()
    {
        var invite = new TelegramInvite("secure-token");

        invite.Use(123456789);

        Assert.True(invite.IsUsed);
        Assert.Equal(123456789, invite.UsedByTelegramUserId);
        Assert.NotNull(invite.UsedAtUtc);
    }

    [Fact]
    public void Use_ShouldThrow_WhenInviteWasAlreadyUsed()
    {
        var invite = new TelegramInvite("secure-token");
        invite.Use(123456789);

        Assert.Throws<InvalidOperationException>(() =>
            invite.Use(987654321));
    }
}
