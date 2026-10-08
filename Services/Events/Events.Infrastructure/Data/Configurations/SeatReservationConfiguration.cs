using Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Events.Infrastructure.Data.Configurations;

internal sealed class SeatReservationConfiguration : IEntityTypeConfiguration<SeatReservation>
{
    public void Configure(EntityTypeBuilder<SeatReservation> builder)
    {
        builder.ToTable("event_seat_reservations", t =>
        {
            t.HasCheckConstraint("chk_reservations_seats_positive", "\"seats\" > 0");
            t.HasCheckConstraint("chk_reservations_total_price_non_negative", "\"total_price\" >= 0");
            t.HasCheckConstraint("chk_reservations_unit_price_non_negative", "\"unit_price\" >= 0");
            t.HasCheckConstraint("chk_reservations_discount_non_negative", "\"discount_amount\" >= 0");
            t.HasCheckConstraint("chk_reservations_price_math", "\"total_price\" = (\"seats\" * \"unit_price\") - \"discount_amount\"");
            t.HasCheckConstraint("chk_reservations_reserved_has_expiration", "\"status\" != 'Reserved' OR \"expires_at\" IS NOT NULL");
            t.HasCheckConstraint("chk_reservations_rejected_has_reason", "\"status\" != 'Rejected' OR \"rejection_reason\" IS NOT NULL");
        });

        builder.HasKey(x => x.BookingId);

        builder.Property(x => x.BookingId)
            .HasColumnName("booking_id")
            .ValueGeneratedNever();

        builder.Property(x => x.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(x => x.Seats)
            .HasColumnName("seats")
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.TotalPrice)
            .HasColumnName("total_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.PriceVersion)
            .HasColumnName("price_version")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id")
            .IsRequired();

        builder.Property(x => x.CausationId)
            .HasColumnName("causation_id")
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(x => new { x.EventId, x.Status })
            .HasDatabaseName("ix_seat_reservations_event_status");

        builder.HasIndex(x => x.ExpiresAt)
            .HasFilter("\"status\" = 'Reserved'")
            .HasDatabaseName("ix_seat_reservations_expires_at");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("ix_seat_reservations_correlation_id");
    }
}
