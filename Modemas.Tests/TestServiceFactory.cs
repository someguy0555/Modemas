using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modemas.Server.Data;
using Modemas.Server.Interfaces;
using Modemas.Server.Services;
using Modemas.Server.Models;
using Moq;

public static class TestServiceFactory
{
    public static ServiceProvider Create()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddSingleton<ILobbyStore, LobbyStore>();
        services.AddScoped<ILobbyManager, LobbyManager>();
        services.AddScoped<ILobbyService, LobbyService>();
        services.AddScoped<IQuestionRepository, EfQuestionRepository>();

        services.AddScoped(_ => Mock.Of<ILobbyNotifier>());
        services.AddScoped(_ => Mock.Of<IMatchService>());
        services.AddScoped(_ => Mock.Of<IQuestionGenerationService>());

        return services.BuildServiceProvider();
    }
}
