using Microsoft.EntityFrameworkCore;
using ConnectCore.Shared.Models;

namespace ConnectCore.UserService.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.OwnsOne(e => e.Address, address =>
            {
                address.Property(a => a.Street).HasMaxLength(100);
                address.Property(a => a.City).HasMaxLength(50);
                address.Property(a => a.State).HasMaxLength(50);
                address.Property(a => a.PostalCode).HasMaxLength(20);
                address.Property(a => a.Country).HasMaxLength(50);
            });
        });

        var users = GenerateSeedUsers();
        modelBuilder.Entity<User>().HasData(users);
    }

    private static User[] GenerateSeedUsers()
    {
        return new[]
        {
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "+1234567890",
                DateOfBirth = new DateTime(1990, 1, 15),
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                CreatedBy = "System"
            },
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                PhoneNumber = "+1234567891",
                DateOfBirth = new DateTime(1985, 5, 20),
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                CreatedBy = "System"
            },
            new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                FirstName = "Bob",
                LastName = "Johnson",
                Email = "bob.johnson@example.com",
                PhoneNumber = "+1234567892",
                DateOfBirth = new DateTime(1992, 8, 10),
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                CreatedBy = "System"
            }
        };
    }
}
