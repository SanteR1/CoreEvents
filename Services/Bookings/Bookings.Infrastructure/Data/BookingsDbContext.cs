using Bookings.Domain.Entities;
using Bookings.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Infrastructure.Data
{
    internal sealed class BookingsDbContext : DbContext
    {
        public BookingsDbContext(DbContextOptions<BookingsDbContext> options) : base(options) { }

        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingsDbContext).Assembly);
        }
    }
}