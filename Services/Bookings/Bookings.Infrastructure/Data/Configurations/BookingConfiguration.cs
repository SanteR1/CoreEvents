using Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookings.Infrastructure.Data.Configurations;

internal class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);

        builder
            .HasIndex(x => x.CreatedAt)
            .HasFilter("\"status\" = 'Pending'");

        builder
            .HasIndex(x => new { x.UserId, x.Status });

        builder
            .HasIndex(x => new { x.EventId, x.Status });

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(b => b.Seats)
            .HasColumnName("seats")
            .IsRequired();

        builder.Property(b => b.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(b => b.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(b => b.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(b => b.TotalPrice)
            .HasColumnName("total_price")
            .HasPrecision(18, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(b => b.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(18, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(b => b.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(18, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(b => b.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("KZT")
            .IsRequired();

        builder.Property(b => b.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(200);

        builder.Property(b => b.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(b => b.CancellationRequestedAt)
            .HasColumnName("cancellation_requested_at");
    }
}
