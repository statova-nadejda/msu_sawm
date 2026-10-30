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
}
}