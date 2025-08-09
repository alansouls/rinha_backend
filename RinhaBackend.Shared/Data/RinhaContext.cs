using Microsoft.EntityFrameworkCore;
using RinhaBackend.Shared.Data.Configurations;

namespace RinhaBackend.Shared.Data;

public class RinhaContext : DbContext
{
    public RinhaContext()
    {
    }
    
    public RinhaContext(DbContextOptions<RinhaContext> options)
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentLogConfiguration());
    }
}