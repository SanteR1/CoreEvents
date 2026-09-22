using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Users.Domain.Entities;

namespace Users.Infrastructure.Data.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
               .HasColumnName("id")
               .ValueGeneratedNever();

        builder.Property(x => x.UserId)
               .HasColumnName("user_id")
               .IsRequired();

        builder.Property(x => x.TokenHash)
               .HasColumnName("token_hash")
               .HasMaxLength(128)
               .IsRequired();

        builder.Property(x => x.CreatedAt)
               .HasColumnName("created_at")
               .IsRequired();

        builder.Property(x => x.ExpiresAt)
               .HasColumnName("expires_at")
               .IsRequired();

        builder.Property(x => x.RevokedAt)
               .HasColumnName("revoked_at");

        builder.Property(x => x.ReplacedByTokenHash)
               .HasColumnName("replaced_by_token_hash")
               .HasMaxLength(128);

        // 1. Уникальный индекс для мгновенного поиска по токену при /refresh и /logout
        builder.HasIndex(x => x.TokenHash)
               .HasDatabaseName("ix_refresh_tokens_token_hash")
               .IsUnique();

        // 2. Частичный составной индекс для активных токенов пользователя (поиск сессий / компрометация)
        // В индекс попадают ТОЛЬКО еще не отозванные токены!
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt })
               .HasDatabaseName("ix_refresh_tokens_user_id_active")
               .HasFilter("revoked_at IS NULL");

        // 3. Индекс для периодической очистки устаревших записей
        builder.HasIndex(x => x.ExpiresAt)
               .HasDatabaseName("ix_refresh_tokens_expires_at");

        // 4. Внешний ключ на таблицу users с каскадным удалением
        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
