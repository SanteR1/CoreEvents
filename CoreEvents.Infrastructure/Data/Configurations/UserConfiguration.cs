using CoreEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreEvents.Infrastructure.Data.Configurations
{
    internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.UserName)
                .IsUnique();

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(x => x.UserName)
                .HasColumnName("user")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.PasswordHash)
                .HasColumnName("passwordhash")
                .IsRequired();

            builder.Property(x => x.Role)
                .HasColumnName("role")
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
        }
    }
}
