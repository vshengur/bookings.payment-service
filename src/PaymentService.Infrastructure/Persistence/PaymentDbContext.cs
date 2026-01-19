using Bookings.Common;
using Bookings.Common.Events;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PaymentService.Infrastructure.Persistence
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> opts) : base(opts) { }

        public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var domainEvents = ChangeTracker
                .Entries<Entity>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            var result = await base.SaveChangesAsync(ct);

            if (domainEvents.Count != 0)
            {
                var dispatcher = this.GetService<IDomainEventDispatcher>();
                await dispatcher.DispatchAsync(domainEvents, ct);
            }

            ChangeTracker
                .Entries<Entity>()
                .ToList()
                .ForEach(e => e.Entity.ClearEvents());

            return result;
        }
    }

    public class PaymentIntent
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EUR";
        public string Status { get; set; } = "pending";
    }
}
