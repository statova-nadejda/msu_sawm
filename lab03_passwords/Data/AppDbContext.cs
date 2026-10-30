using Microsoft.EntityFrameworkCore;
using XssLab.Models;

namespace XssLab.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<GuestEntry> Entries => Set<GuestEntry>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(
    ModelBuilder modelBuilder)
{
    var entry = modelBuilder.Entity<GuestEntry>();

    entry.ToTable("guest", "public");

    entry.HasKey(x => x.Id);

    entry.Property(x => x.Id)
        .HasColumnName("id")
        .ValueGeneratedOnAdd();

    entry.Property(x => x.User)
        .HasColumnName("user")
        .HasMaxLength(100)
        .IsRequired();

    entry.Property(x => x.TextMessage)
        .HasColumnName("text_message")
        .HasMaxLength(2000)
        .IsRequired();

    entry.Property(x => x.Email)
        .HasColumnName("e_mail")
        .HasMaxLength(200)
        .IsRequired();

    entry.Property(x => x.DataTimeMessage)
        .HasColumnName("data_time_message")
        .HasColumnType("timestamp with time zone")
        .IsRequired();

    var user = modelBuilder.Entity<AppUser>();

    user.ToTable("user", "public");

    user.HasKey(x => x.Id);

    user.Property(x => x.Id)
        .HasColumnName("id");

    user.Property(x => x.Login)
        .HasColumnName("login")
        .HasMaxLength(100)
        .IsRequired();

    user.HasIndex(x => x.Login)
        .IsUnique();

    user.Property(x => x.Password)
        .HasColumnName("password");

    user.Property(x => x.PasswordHash)
        .HasColumnName("password_hash");
}
}