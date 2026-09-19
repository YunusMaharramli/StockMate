using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Infrastructure.Persistence.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public sealed class ProcessedOperationConfiguration
    : IEntityTypeConfiguration<ProcessedOperation>
{
    public void Configure(
        EntityTypeBuilder<ProcessedOperation> builder)
    {
        builder.ToTable("ProcessedOperations");

        builder.HasKey(operation => operation.Id);

        builder.Property(operation => operation.OperationId)
            .IsRequired();

        builder.Property(operation => operation.TelegramUserId)
            .IsRequired();

        builder.Property(operation => operation.OperationType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(operation => operation.PayloadHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(operation => operation.ResultJson)
            .HasColumnType("jsonb");

        builder.Property(operation => operation.CreatedAtUtc)
            .IsRequired();

        builder.Property(operation => operation.CompletedAtUtc);

        builder.Property(operation => operation.IsCompleted)
            .IsRequired();

        builder.HasIndex(operation => new
        {
            operation.TelegramUserId,
            operation.OperationId
        })
        .IsUnique();

        builder.HasIndex(operation => operation.CreatedAtUtc);
    }
}