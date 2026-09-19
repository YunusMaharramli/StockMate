using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Infrastructure.Persistence.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(
        EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.MessageType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(message => message.PayloadJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.CreatedAtUtc)
            .IsRequired();

        builder.Property(message => message.ProcessedAtUtc);

        builder.Property(message => message.AttemptCount)
            .IsRequired();

        builder.Property(message => message.NextAttemptAtUtc);

        builder.Property(message => message.LastError)
            .HasMaxLength(2000);

        builder.Ignore(message => message.IsProcessed);

        builder.HasIndex(message => new
        {
            message.ProcessedAtUtc,
            message.NextAttemptAtUtc,
            message.CreatedAtUtc
        });
    }
}