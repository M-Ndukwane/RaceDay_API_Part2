

using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Roles
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");

            entity.HasKey(r => r.RoleID)
                .HasName("PK_Roles");

            entity.Property(r => r.RoleName)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(r => r.RoleName)
                .IsUnique()
                .HasDatabaseName("UQ_Roles_RoleName");
        });

        // Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(u => u.UsersID)
                .HasName("PK_Users");

            entity.Property(u => u.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(255)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("UQ_Users_Email");

            entity.Property(u => u.PhoneNumber)
                .HasColumnName("Phone number")
                .HasMaxLength(30);

            entity.Property(u => u.PasswordHash)
                .HasColumnName("password")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETDATE()");

            entity.Property(u => u.IsActive)
                .IsRequired();

            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RolesID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Venues
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.ToTable("Venues");

            entity.HasKey(v => v.VenuesID)
                .HasName("PK_Venues");

            entity.Property(v => v.VenueName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(v => v.AddressLine)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(v => v.City)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(v => v.PostalCode)
                .HasColumnName("Postal Code")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(v => v.Province)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(v => new { v.VenueName, v.AddressLine })
                .IsUnique()
                .HasDatabaseName("UQ_Venues_NameAddress");
        });

        // Events
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");

            entity.HasKey(e => e.EventsID)
                .HasName("PK_Events");

            entity.Property(e => e.EventName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.EventDate)
                .HasColumnType("date")
                .IsRequired();

            entity.Property(e => e.StartTime)
                .HasColumnName("Start Time")
                .HasColumnType("time")
                .IsRequired();

            entity.Property(e => e.RegistrationDeadline)
                .HasColumnName("Registration DeadLine")
                .HasColumnType("datetime2")
                .IsRequired();

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Organiser)
                .WithMany(u => u.OrganisedEvents)
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(e => e.VenuesID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Categories
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.HasKey(c => c.CategoriesID)
                .HasName("PK_Categories");

            entity.Property(c => c.CategoryName)
                .HasColumnName("Categories Name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(c => c.EntryFee)
                .HasColumnName("Entry Fee")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(c => c.MaxParticipants)
                .IsRequired();

            entity.HasIndex(c => new { c.EventsID, c.CategoryName })
                .IsUnique()
                .HasDatabaseName("UQ_Categories_EventName");

            entity.HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventsID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Enrollments
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("Enrollments");

            entity.HasKey(e => e.EnrollmentsID)
                .HasName("PK_Enrollments");

            entity.Property(e => e.EnrolledAt)
                .HasColumnName("Enrolled At")
                .HasColumnType("datetime2")
                .IsRequired();

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(e => e.EmergencyContactName)
                .HasColumnName("Emergency Contact Name")
                .HasMaxLength(200);

            entity.Property(e => e.EmergencyContactPhone)
                .HasColumnName("Emergency Contact Phone")
                .HasMaxLength(30);

            entity.HasIndex(e => new { e.UsersID, e.EventsID })
                .IsUnique()
                .HasDatabaseName("UQ_Enrollments_UserEvent");

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CategoriesID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Event)
                .WithMany(ev => ev.Enrollments)
                .HasForeignKey(e => e.EventsID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Participant)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.UsersID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Results
        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results");

            entity.HasKey(r => r.ResultsID)
                .HasName("PK_Results");

            entity.Property(r => r.Position)
                .IsRequired(false);

            entity.Property(r => r.FinishTimeSeconds)
                .HasColumnName("FinishTime(seconds)")
                .IsRequired(false);

            entity.Property(r => r.ResultStatus)
                .HasColumnName("Result Status")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(r => r.RecordedAt)
                .HasColumnName("Recorded At")
                .HasColumnType("datetime2")
                .IsRequired();

            entity.HasIndex(r => r.EnrollmentsID)
                .IsUnique()
                .HasDatabaseName("UQ_Results_Enrollment");

            entity.HasOne(r => r.Enrollment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrollmentsID)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

