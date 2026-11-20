using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RealEstateAI.Api.Data;
using Xunit;

namespace RealEstateAI.Api.Tests;

[Collection("API")]
public sealed class DatabaseTests(ApiFixture fixture)
{
    [Fact]
    public async Task Local_database_uses_sql_server_with_migrations_and_seed_data()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Contains("SqlServer", db.Database.ProviderName);
        Assert.Contains(db.Database.GetAppliedMigrations(), m => m.Contains("InitialCreate"));
        Assert.True(await db.Properties.AnyAsync());
        Assert.True(await db.DocumentChecklist.AnyAsync());
        Assert.True(await db.Roles.AnyAsync());
    }
}
