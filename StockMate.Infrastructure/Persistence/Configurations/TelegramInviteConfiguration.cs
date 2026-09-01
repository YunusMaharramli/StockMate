using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public sealed class TelegramInviteConfiguration
    : IEntityTypeConfiguration<TelegramInvite>
{
    public void Configure(
        EntityTypeBuilder<TelegramInvite> builder)
    {
        builder.ToTable("TelegramInvites");

        builder.HasKey(invite => invite.Id);

        builder.Property(invite => invite.Token)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(invite => invite.Token)
            .IsUnique();

        builder.Property(invite => invite.CreatedAtUtc)
            .IsRequired();

        builder.Property(invite => invite.UsedByTelegramUserId);

        builder.Property(invite => invite.UsedAtUtc)
            .IsConcurrencyToken();

        builder.Ignore(invite => invite.IsUsed);
    }
}