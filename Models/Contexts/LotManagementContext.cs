using CurrencyLotManagementLibrary.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CurrencyLotManagementSystem.Models.Contexts;

public class LotManagementContext : IdentityDbContext<User>
{
    public LotManagementContext(DbContextOptions options) : base(options)
    {
        //empty
    }
}