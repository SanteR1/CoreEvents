using CoreEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreEvents.Infrastructure.Data.Configurations
{
    internal sealed class EventConfiguration: IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.ToTable("events");

            builder.ToTable(x=> x.HasCheckConstraint("CK_events_dates", "\"start_at\" < \"end_at\""));
            
            builder.HasKey(x=> x.Id);

            // Композитный индекс для фильтрации и сортировки дат
            builder.HasIndex(e => new { e.StartAt, e.EndAt })
                .IsDescending(true, false); // StartAt по убыванию, EndAt по возрастанию

            // GIN индекс для быстрого регистронезависимого поиска по подстроке
            builder.HasIndex(e => e.Title)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.Title)
                .HasColumnName("title")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Description)
                .HasColumnName("description")
                .HasMaxLength(2000);

            builder.Property(x => x.StartAt)
                .HasColumnName("start_at")
                .IsRequired();

            builder.Property(x => x.EndAt)
                .HasColumnName("end_at")
                .IsRequired();

            builder.Property(e => e.AvailableSeats)
                .HasColumnName("available_seats")
                .IsRequired();

            builder.Property(x => x.TotalSeats)
                .HasColumnName("total_seats")
                .IsRequired();

            builder.HasMany(e => e.Bookings)
                .WithOne(b => b.Event)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
