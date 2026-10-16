using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.API.Data;

namespace RaceDay.API.Tests
{
    /// <summary>
    /// Starts the real API in memory for testing, but swaps SQL Server for an
    /// in-memory database so the tests need no local SQL Server.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = "RaceDayTests_" + Guid.NewGuid();

        /// <summary>The organiser invite code the running API expects.</summary>
        public string InviteCode =>
            Services.GetRequiredService<IConfiguration>()["OrganiserInviteCode"]!;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<RaceDayContext>();
                services.RemoveAll<DbContextOptions<RaceDayContext>>();

                var databaseServices = services
                    .Where(service =>
                        service.ServiceType.IsGenericType &&
                        service.ServiceType.GetGenericTypeDefinition().Name
                            == "IDbContextOptionsConfiguration`1" &&
                        service.ServiceType.GetGenericArguments()[0]
                            == typeof(RaceDayContext))
                    .ToList();

                foreach (var service in databaseServices)
                {
                    services.Remove(service);
                }

                services.AddDbContext<RaceDayContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
            });

            builder.UseEnvironment("Development");
        }
    }
}
