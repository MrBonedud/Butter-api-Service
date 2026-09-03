using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Swipe> Swipes => Set<Swipe>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(u => u.DisplayName)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);

            entity.Property(rt => rt.Token)
                .IsRequired();

            // Token must be unique
            entity.HasIndex(rt => rt.Token)
                .IsUnique();

            // RefreshToken -> User relationship
            entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index on UserId
            entity.HasIndex(rt => rt.UserId);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Code)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsRequired();

            entity.HasIndex(r => r.Code)
                .IsUnique();

            entity.Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(r => r.CurrentMovieId);

            entity.HasOne(r => r.Creator)
                .WithMany(u => u.CreatedRooms)
                .HasForeignKey(r => r.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => r.CreatorId);
        });

        modelBuilder.Entity<Swipe>(entity =>
{
    entity.HasKey(swipe => swipe.Id);

    entity.Property(swipe => swipe.RoomId)
        .IsRequired();

    entity.Property(swipe => swipe.ParticipantId)
        .IsRequired();

    entity.Property(swipe => swipe.TmdbMovieId)
        .IsRequired();

    entity.Property(swipe => swipe.Direction)
        .HasConversion<string>()
        .HasMaxLength(10)
        .IsRequired();

    entity.HasIndex(swipe => new
    {
        swipe.RoomId,
        swipe.ParticipantId,
        swipe.TmdbMovieId
    })
    .IsUnique();

    entity.HasOne(swipe => swipe.Room)
        .WithMany(room => room.Swipes)
        .HasForeignKey(swipe => swipe.RoomId)
        .OnDelete(DeleteBehavior.Cascade);
});
    }


}
