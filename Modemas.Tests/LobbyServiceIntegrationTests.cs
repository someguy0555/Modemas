using Modemas.Server.Models;
using Modemas.Server.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

public class LobbyServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IServiceProvider _services;

    public LobbyServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _services = factory.Services;
    }

    [Fact]
    public async Task WaitForQuestions_Persists_And_Reuses_Questions()
    {
        using var scope = _services.CreateScope();

        var lobbyManager = scope.ServiceProvider.GetRequiredService<ILobbyManager>();
        var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();
        var repo = scope.ServiceProvider.GetRequiredService<IQuestionRepository>();

        var lobby = lobbyManager.CreateLobby("host1");
        lobby.LobbySettings = new LobbySettings
        {
            Topic = "IntegrationTest",
            NumberOfQuestions = 1
        };

        var first = await lobbyService.WaitForQuestionsAsync(lobby.LobbyId);
        Assert.True(first);

        var stored = await repo.GetByTopicAsync("IntegrationTest");
        Assert.NotEmpty(stored);

        var second = await lobbyService.WaitForQuestionsAsync(lobby.LobbyId);
        Assert.True(second);
    }

    [Fact]
    public async Task StartVoting_Fails_When_No_Questions_Available()
    {
        using var scope = _services.CreateScope();

        var lobbyManager = scope.ServiceProvider.GetRequiredService<ILobbyManager>();
        var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();

        var lobby = lobbyManager.CreateLobby("host1");
        lobby.LobbySettings = new LobbySettings
        {
            Topic = "EmptyTopic",
            NumberOfQuestions = 3
        };

        await lobbyService.StartVoting(lobby.LobbyId);

        Assert.NotNull(lobby.Match);
        Assert.NotEmpty(lobby.Match.Questions);
    }

    [Fact]
    public async Task CreateLobby_HostIsAddedToLobby()
    {
        var provider = TestServiceFactory.Create();
        var service = provider.GetRequiredService<ILobbyService>();
        var manager = provider.GetRequiredService<ILobbyManager>();

        string connectionId = "host-1";
        string hostName = "Host";

        await service.CreateLobby(connectionId, hostName);

        var lobby = manager.FindLobbyByConnection(connectionId);

        Assert.NotNull(lobby);
        Assert.Equal(connectionId, lobby.HostConnectionId);
        Assert.Single(lobby.Players);
        Assert.Equal(hostName, lobby.Players[0].Name);
    }

    [Fact]
    public async Task JoinLobby_PlayerIsAddedToExistingLobby()
    {
        var provider = TestServiceFactory.Create();
        var service = provider.GetRequiredService<ILobbyService>();
        var manager = provider.GetRequiredService<ILobbyManager>();

        await service.CreateLobby("host", "Host");
        var lobby = manager.FindLobbyByConnection("host");

        await service.JoinLobby("player1", lobby.LobbyId, "Player1");

        Assert.Equal(2, lobby.Players.Count);
        Assert.Contains(lobby.Players, p => p.Name == "Player1");
    }

    [Fact]
    public async Task WaitForQuestionsAsync_UsesExistingQuestions()
    {
        var provider = TestServiceFactory.Create();
        var service = provider.GetRequiredService<ILobbyService>();
        var manager = provider.GetRequiredService<ILobbyManager>();
        var repo = provider.GetRequiredService<IQuestionRepository>();

        await service.CreateLobby("host", "Host");
        var lobby = manager.FindLobbyByConnection("host");

        lobby.LobbySettings = new LobbySettings
        {
            Topic = "Science",
            NumberOfQuestions = 1
        };

        await repo.SaveAsync("Science", new[]
        {
        new MultipleChoiceQuestion { Text = "Existing question" }
        });

        bool result = await service.WaitForQuestionsAsync(lobby.LobbyId);

        Assert.True(result);
        Assert.NotNull(lobby.Match);
        Assert.Single(lobby.Match.Questions);
    }
}
