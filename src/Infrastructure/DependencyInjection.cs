using DndCampaignManager.Application.Common.Interfaces;
using DndCampaignManager.Infrastructure.DndBeyond;
using DndCampaignManager.Infrastructure.Identity;
using DndCampaignManager.Infrastructure.Persistence;
using DndCampaignManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DndCampaignManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // D&D Beyond import service — HttpClient with configured timeout
        services.Configure<DndBeyondImportServiceOptions>(
            configuration.GetSection(DndBeyondImportServiceOptions.SectionName));

        services.AddHttpClient<IDndBeyondImportService, DndBeyondImportService>(client =>
        {
            var timeout = configuration.GetValue<int?>("DndBeyond:TimeoutSeconds") ?? 15;
            client.Timeout = TimeSpan.FromSeconds(timeout);
        });

        // Magic item search service — queries local DB + Open5e + dnd5eapi.co
        services.AddHttpClient<IMagicItemSearchService, MagicItemSearchService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
