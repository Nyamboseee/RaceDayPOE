using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

// Starts the real API in memory, but swaps SQL Server for an in-memory database
public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server registrations for RaceDayDbContext
            var old = services
                .Where(d => d.ServiceType.IsGenericType
                         && d.ServiceType.GenericTypeArguments.Contains(typeof(RaceDayDbContext)))
                .ToList();
            foreach (var descriptor in old)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}