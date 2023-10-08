using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VehicleRentalSystem.Domain.Entities;
using VehicleRentalSystem.Domain.Constants;

namespace VehicleRentalSystem.Infrastructure.Persistence.Seed;

public class DbInitializer : IDbInitializer
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DbInitializer(ApplicationDbContext dbContext, 
        UserManager<IdentityUser> userManager, 
        RoleManager<IdentityRole> roleManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task Initialize()
    {
        try
        {
            if (_dbContext.Database.GetPendingMigrations().Count() > 0)
            {
                _dbContext.Database.Migrate();
            }
        }
        catch (Exception)
        {
            throw;
        }

        if (!_roleManager.RoleExistsAsync(Constants.Admin).GetAwaiter().GetResult())
        {
            _roleManager.CreateAsync(new IdentityRole(Constants.Admin)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(Constants.Staff)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(Constants.Customer)).GetAwaiter().GetResult();
        }

        var user = new AppUser
        {
            FullName = "Bishal Thapa",
            Email = "bishal.thapa1200@gmail.com",
            NormalizedEmail = "BISHAL.THAPA1200@GMAIL.COM",
            UserName = "bishal.thapa1200@gmail.com",
            NormalizedUserName = "BISHAL",
            Address = "khajura ",
            State = "State 5",
            PhoneNumber = "9812533103",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("D")
        };

        var userManager = _userManager.CreateAsync(user, "@Bishal01").GetAwaiter().GetResult();

        var result = _dbContext.Users.FirstOrDefault(u => u.Email == "bishal.thapa1200@gmail.com");

        _userManager.AddToRoleAsync(user, Constants.Admin).GetAwaiter().GetResult();

        await _dbContext.SaveChangesAsync();
    }
}
