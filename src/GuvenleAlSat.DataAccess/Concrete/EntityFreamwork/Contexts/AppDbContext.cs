using GuvenleAlSat.DataAccess.Entities.Categories;
using GuvenleAlSat.DataAccess.Entities.Listings;
using GuvenleAlSat.DataAccess.Entities.Locations;
using GuvenleAlSat.DataAccess.Entities.Messages;
using GuvenleAlSat.DataAccess.Entities.Subscriptions;
using GuvenleAlSat.DataAccess.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GuvenleAlSat.DataAccess.Concrete.EntityFramework.Contexts;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<VehicleDetail> VehicleDetails => Set<VehicleDetail>();
    public DbSet<VehicleDamageReport> VehicleDamageReports => Set<VehicleDamageReport>();
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();
    public DbSet<ListingFavorite> ListingFavorites => Set<ListingFavorite>();
    public DbSet<MessageConversation> MessageConversations => Set<MessageConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Neighborhood> Neighborhoods => Set<Neighborhood>();

    public DbSet<RealEstateDetail> RealEstateDetails => Set<RealEstateDetail>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PostgreSQL Sequence: 9 haneli İlan No
        modelBuilder.HasSequence<long>("ListingNoSequence").StartsAt(1000000000).IncrementsBy(1);
        modelBuilder.Entity<Listing>()
            .Property(l => l.ListingNo)
            .HasDefaultValueSql("nextval('\"ListingNoSequence\"')");

        modelBuilder.Entity<Listing>()
            .HasIndex(l => l.ListingNo)
            .IsUnique();

        // PostgreSQL JSONB sütunu
        modelBuilder.Entity<Listing>()
            .Property(l => l.DynamicAttributesJson)
            .HasColumnType("jsonb");

        // Arama ve Filtreleme Kompozit İndeksi
        modelBuilder.Entity<Listing>()
            .HasIndex(l => new { l.Status, l.CategoryId, l.City, l.Price });

        // Birebir İlişkiler
        modelBuilder.Entity<Listing>()
            .HasOne(l => l.VehicleDetail)
            .WithOne(v => v.Listing)
            .HasForeignKey<VehicleDetail>(v => v.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Listing>()
            .HasOne(l => l.DamageReport)
            .WithOne(d => d.Listing)
            .HasForeignKey<VehicleDamageReport>(d => d.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Çift Favori Engeli
        modelBuilder.Entity<ListingFavorite>()
            .HasIndex(f => new { f.UserId, f.ListingId })
            .IsUnique();

        // Mesajlaşma Yabancı Anahtarları
        modelBuilder.Entity<MessageConversation>()
            .HasOne(m => m.Buyer)
            .WithMany()
            .HasForeignKey(m => m.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MessageConversation>()
            .HasOne(m => m.Seller)
            .WithMany()
            .HasForeignKey(m => m.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(r => r.Token)
            .IsUnique();
    }
}