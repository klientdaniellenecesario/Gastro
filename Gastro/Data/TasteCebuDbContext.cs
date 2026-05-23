using GastroCebu.Models;
using Microsoft.EntityFrameworkCore;

namespace GastroCebu.Data;

public class TasteCebuDbContext : DbContext
{
    public TasteCebuDbContext(DbContextOptions<TasteCebuDbContext> options) : base(options) { }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<RestaurantListing> Restaurants { get; set; }
    public DbSet<DishListing> Dishes { get; set; }
    public DbSet<FoodEvent> Events { get; set; }
    public DbSet<ReviewEntry> Reviews { get; set; }
    public DbSet<BookmarkEntry> Bookmarks { get; set; }
    public DbSet<EventRegistration> EventRegistrations { get; set; }
    public DbSet<ActivityEntry> Activities { get; set; }
    public DbSet<TriedDish> TriedDishes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // AppUser
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("User");
            e.Property(u => u.Location).HasDefaultValue("Cebu City, Philippines");
        });

        // RestaurantListing
        modelBuilder.Entity<RestaurantListing>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Rating).HasColumnType("REAL");
            e.Property(r => r.Vibe).HasDefaultValue("all");
        });

        // DishListing
        modelBuilder.Entity<DishListing>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Price).HasColumnType("REAL");
        });

        // FoodEvent
        modelBuilder.Entity<FoodEvent>(e =>
        {
            e.HasKey(ev => ev.Id);
        });

        // ReviewEntry
        modelBuilder.Entity<ReviewEntry>(e =>
        {
            e.HasKey(r => r.Id);
        });

        // BookmarkEntry — unique per user+type+item
        modelBuilder.Entity<BookmarkEntry>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.UserId, b.ItemType, b.ItemId }).IsUnique();
        });

        // EventRegistration — unique per user+event
        modelBuilder.Entity<EventRegistration>(e =>
        {
            e.HasKey(er => er.Id);
            e.HasIndex(er => new { er.UserId, er.EventId }).IsUnique();
        });

        // ActivityEntry
        modelBuilder.Entity<ActivityEntry>(e =>
        {
            e.HasKey(a => a.Id);
        });
    }
}