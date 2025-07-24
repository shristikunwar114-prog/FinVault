using FinVault.API.Data;
using Microsoft.EntityFrameworkCore;
#pragma warning disable CS8603

namespace FinVault.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new AppDbContext(options);
    }
}
