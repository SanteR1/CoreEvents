using CoreEvents.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreEvents.Data.DataAccess.Configurations
{
    internal class BookingConfiguration:IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("bookings");

            builder.HasKey(b => b.Id);
            builder.HasIndex(x => x.EventId);

            builder.Property(b => b.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(b => b.EventId)
                .HasColumnName("event_id")
                .IsRequired();

            builder.Property(b => b.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(b => b.ProcessedAt)
                .HasColumnName("processed_at");

            builder.HasOne(x => x.Event)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x=> x.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
        }
    }
}
