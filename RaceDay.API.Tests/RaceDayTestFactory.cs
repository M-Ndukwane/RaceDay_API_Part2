

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Tests;

public class RaceDayTestFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName =
    $"RaceDayTestDatabase_{Guid.NewGuid():N}";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the original database context registrations.
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.RemoveAll<RaceDayDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<RaceDayDbContext>>();

            // Register only the InMemory provider for testing.
            services.AddDbContext<RaceDayDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }

    public async Task SeedParticipantRoleAsync()
    {
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<RaceDayDbContext>();

        if (!await context.Roles.AnyAsync(
                role => role.RoleName == "Participant"))
        {
            context.Roles.Add(new Role
            {
                RoleName = "Participant"
            });

            await context.SaveChangesAsync();
        }
    }

    public async Task SeedOrganiserAndVenueAsync()
    {
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<RaceDayDbContext>();

        if (!await context.Roles.AnyAsync(
                role => role.RoleName == "Organiser"))
        {
            context.Roles.Add(new Role
            {
                RoleName = "Organiser"
            });
        }

        if (!await context.Venues.AnyAsync())
        {
            context.Venues.Add(new Venue
            {
                VenueName = "Test Stadium",
                AddressLine = "1 Test Road",
                City = "Johannesburg",
                PostalCode = "2000",
                Province = "Gauteng"
            });
        }

        await context.SaveChangesAsync();
    }

    public async Task<(string Email, string Password)> CreateOrganiserAsync()
    {
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<RaceDayDbContext>();

        var role = await context.Roles
            .SingleOrDefaultAsync(r => r.RoleName == "Organiser");

        if (role == null)
        {
            role = new Role { RoleName = "Organiser" };
            context.Roles.Add(role);
            await context.SaveChangesAsync();
        }

        if (!await context.Venues.AnyAsync())
        {
            context.Venues.Add(new Venue
            {
                VenueName = "Test Stadium",
                AddressLine = "1 Test Road",
                City = "Johannesburg",
                PostalCode = "2000",
                Province = "Gauteng"
            });

            await context.SaveChangesAsync();
        }

        const string password = "TestPassword123!";
        var email = $"organiser{Guid.NewGuid():N}@example.com";

        var user = new User
        {
            FirstName = "Test",
            LastName = "Organiser",
            Email = email,
            RolesID = role.RoleID,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var passwordHasher =
            scope.ServiceProvider.GetRequiredService<
                Microsoft.AspNetCore.Identity.IPasswordHasher<User>>();

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return (email, password);
    }

}

