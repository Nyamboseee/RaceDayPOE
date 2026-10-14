using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organizer> Organizers => Set<Organizer>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Our tables are singular, EF would default to plural names
        modelBuilder.Entity<Organizer>().ToTable("Organizer");
        modelBuilder.Entity<Participant>().ToTable("Participant");
        modelBuilder.Entity<Event>().ToTable("Event");
        modelBuilder.Entity<Category>().ToTable("Category");
        modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
        modelBuilder.Entity<Result>().ToTable("Result");

        // Email must be unique so two accounts cannot share a login
        modelBuilder.Entity<Organizer>().HasIndex(o => o.Email).IsUnique();
        modelBuilder.Entity<Participant>().HasIndex(p => p.Email).IsUnique();

        // A participant cannot enter the same category twice
        modelBuilder.Entity<Enrollment>()
            .HasIndex(e => new { e.ParticipantId, e.CategoryId })
            .IsUnique();

        // One result per enrollment (1:1)
        modelBuilder.Entity<Result>()
            .HasOne(r => r.Enrollment)
            .WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrollmentId);

        // Match the database: no cascading deletes
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Event)
            .WithMany(ev => ev.Enrollments)
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>().Property(c => c.Distance).HasPrecision(6, 2);
        modelBuilder.Entity<Event>().Property(e => e.Distance).HasPrecision(6, 2);
    }
}