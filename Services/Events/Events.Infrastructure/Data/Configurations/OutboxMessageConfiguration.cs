using Events.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Events.Infrastructure.Data.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id")
            .IsRequired();

        builder.Property(x => x.CausationId)
            .HasColumnName("causation_id");

        builder.Property(x => x.MessageType)
            .HasColumnName("message_type")
            .IsRequired();

        builder.Property(x => x.Topic)
            .HasColumnName("topic")
            .IsRequired();

        builder.Property(x => x.Key)
            .HasColumnName("message_key")
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Headers)
            .HasColumnName("headers")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(x => x.RetryCount)
            .HasColumnName("retry_count");

        builder.Property(x => x.NextRetryAt)
            .HasColumnName("next_retry_at");

        builder.Property(x => x.IsDeadLettered)
            .HasColumnName("is_dead_lettered");

        builder.Property(x => x.LastError)
            .HasColumnName("last_error");

        builder.HasIndex(x => new { x.CreatedAt, x.NextRetryAt })
            .HasFilter("published_at IS NULL AND is_dead_lettered = false");

        builder.HasIndex(x => x.PublishedAt);
    }
}
