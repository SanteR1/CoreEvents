using Events.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Events.Infrastructure.Data.Configurations;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id")
            .IsRequired();

        builder.Property(x => x.CausationId)
            .HasColumnName("causation_id");

        builder.Property(x => x.ConsumerName)
            .HasColumnName("consumer_name")
            .IsRequired();

        builder.Property(x => x.Topic)
            .HasColumnName("topic")
            .IsRequired();

        builder.Property(x => x.Partition)
            .HasColumnName("partition")
            .IsRequired();

        builder.Property(x => x.Offset)
            .HasColumnName("offset")
            .IsRequired();

        builder.Property(x => x.MessageKey)
            .HasColumnName("message_key")
            .IsRequired();

        builder.Property(x => x.MessageType)
            .HasColumnName("message_type")
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Headers)
            .HasColumnName("headers")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ReceivedAt)
            .HasColumnName("received_at")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();

        builder.Property(x => x.LastError)
            .HasColumnName("last_error");

        builder.HasIndex(x => x.ReceivedAt);

        builder.HasIndex(x => x.ProcessedAt);

        builder.HasIndex(x => new
        {
            x.ConsumerName,
            x.Topic,
            x.Partition,
            x.Offset
        })
            .IsUnique();
    }
}
