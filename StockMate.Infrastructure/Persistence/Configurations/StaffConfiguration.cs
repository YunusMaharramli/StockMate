using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.ToTable("Staff");

        builder.HasKey(staff => staff.Id);

        builder.Property(staff => staff.TelegramUserId)
            .IsRequired();

        builder.HasIndex(staff => staff.TelegramUserId)
            .IsUnique();

        builder.Property(staff => staff.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(staff => staff.LastName)
            .HasMaxLength(100);

        builder.Property(staff => staff.Username)
            .HasMaxLength(100);

        builder.Property(staff => staff.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(staff => staff.IsActive)
            .IsRequired();

        builder.Property(staff => staff.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(staff => staff.Role);

        builder.HasIndex(staff => staff.IsActive);
    }
}