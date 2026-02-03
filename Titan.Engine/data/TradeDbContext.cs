using Microsoft.EntityFrameworkCore;

namespace Titan.Engine.Data;

public class TradeDbContext : DbContext
{
    public TradeDbContext(DbContextOptions<TradeDbContext> options) : base(options)
    {
    }

    public DbSet<TradeEntity> Trades { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TradeEntity>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Symbol).HasMaxLength(20);
            entity.Property(t => t.Price).HasPrecision(18, 8);
            entity.Property(t => t.Quantity).HasPrecision(18, 8);
            entity.HasIndex(t => t.Timestamp);
            entity.Property(t => t.Type).HasConversion<string>();
        });
    }
}
