using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- UNIQUE CONSTRAINTS ---
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => new { c.EventId, c.Name })
                .IsUnique();

            modelBuilder.Entity<Enrolment>()
                .HasIndex(e => new { e.EventId, e.ParticipantId })
                .IsUnique();

            modelBuilder.Entity<Result>()
                .HasIndex(r => r.EnrolmentId)
                .IsUnique();

            // --- RELATIONSHIPS ---

            // 1. Enrolment -> Category (The Composite Foreign Key Fix)
            // This ensures a participant can only choose a category that belongs to the specific event.
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => new { e.EventId, e.CategoryId })
                .HasPrincipalKey(c => new { c.EventId, c.CategoryId })
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Category -> Event (Cascade Delete)
            // If an Organiser deletes an event, all its categories are automatically deleted.
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. Enrolment -> Event (Restrict Delete)
            // Prevents an Organiser from deleting an event if participants are already enrolled.
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- DATA SEEDING ---
            // This forces EF Core to insert these default roles into the database automatically
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Organiser" },
                new Role { RoleId = 2, RoleName = "Participant" }
            );
        }
    }
}