using Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookings.Infrastructure.Data.Configurations;

internal sealed class EventProjectionConfiguration : IEntityTypeConfiguration<EventProjection>
{
    public void Configure(EntityTypeBuilder<EventProjection> builder)
    {
        builder.ToTable("event_projections");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.StartAt)
            .HasColumnName("start_at")
            .IsRequired();

        builder.Property(e => e.EndAt)
            .HasColumnName("end_at")
            .IsRequired();

        builder.Property(e => e.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(18, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(e => e.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("KZT")
            .IsRequired();

        builder.Property(e => e.PriceVersion)
            .HasColumnName("price_version")
            .HasDefaultValue(1L)
            .IsRequired();

        builder.Property(e => e.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .HasSentinel(false)
            .IsRequired();

        builder.Property(e => e.Version)
            .HasColumnName("version")
            .HasDefaultValue(1L)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
