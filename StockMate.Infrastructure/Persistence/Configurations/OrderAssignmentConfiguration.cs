using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public class OrderAssignmentConfiguration
    : IEntityTypeConfiguration<OrderAssignment>
{
    public void Configure(
        EntityTypeBuilder<OrderAssignment> builder)
    {
        builder.ToTable("OrderAssignments");

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.OrderId)
            .IsRequired();

        builder.Property(assignment => assignment.StaffId)
            .IsRequired();

        builder.Property(assignment => assignment.AssignedByStaffId)
            .IsRequired();

        builder.Property(assignment => assignment.AssignedAtUtc)
            .IsRequired();

        builder.Ignore(assignment => assignment.IsActive);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(assignment => assignment.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Staff>()
            .WithMany()
            .HasForeignKey(assignment => assignment.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Staff>()
            .WithMany()
            .HasForeignKey(assignment =>
                assignment.AssignedByStaffId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(assignment => assignment.StaffId);

        builder.HasIndex(assignment => new
        {
            assignment.OrderId,
            assignment.ReleasedAtUtc
        });

        builder.HasIndex(assignment => assignment.OrderId)
            .IsUnique()
            .HasFilter("\"ReleasedAtUtc\" IS NULL");
    }
}