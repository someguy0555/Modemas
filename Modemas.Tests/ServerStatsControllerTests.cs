using Microsoft.AspNetCore.Mvc;
using Moq;
using Modemas.Server.Controllers;
using Modemas.Server.Models;
using Modemas.Server.Interfaces;

public class ServerStatsControllerTests
{
    private readonly Mock<ILobbyStore> _lobbyStoreMock;
    private readonly ServerStatsController _controller;

    public ServerStatsControllerTests()
    {
        _lobbyStoreMock = new Mock<ILobbyStore>();
        _controller = new ServerStatsController(_lobbyStoreMock.Object);
    }

    [Fact]
    public void GetServerStats_NoLobbies_ReturnsZeroStats()
    {
        _lobbyStoreMock.Setup(s => s.GetAll()).Returns(new List<Lobby>());

        var result = _controller.GetServerStats();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stats = Assert.IsType<ServerStats>(okResult.Value);

        Assert.Equal(0, stats.TotalLobbies);
        Assert.Equal(0, stats.ActiveLobbies);
        Assert.Equal(0, stats.WaitingLobbies);
        Assert.Equal(0, stats.TotalPlayers);
        Assert.Equal(0, stats.AveragePlayersPerLobby);
        Assert.Empty(stats.ActiveTopics);
    }

    [Fact]
    public void GetServerStats_AllWaitingLobbies_ReturnsCorrectStats()
    {
        var lobbies = new List<Lobby>
        {
            new Lobby { State = LobbyState.Waiting, Players = new List<Player>{ new Player(), new Player() }, LobbySettings = new LobbySettings { Topic = "Math" } },
            new Lobby { State = LobbyState.Waiting, Players = new List<Player>{ new Player() }, LobbySettings = new LobbySettings { Topic = "Science" } }
        };
        _lobbyStoreMock.Setup(s => s.GetAll()).Returns(lobbies);

        var result = _controller.GetServerStats();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stats = Assert.IsType<ServerStats>(okResult.Value);

        Assert.Equal(2, stats.TotalLobbies);
        Assert.Equal(0, stats.ActiveLobbies);
        Assert.Equal(2, stats.WaitingLobbies);
        Assert.Equal(3, stats.TotalPlayers);
        Assert.Equal(1.5, stats.AveragePlayersPerLobby);
        Assert.Equal(new[] { "Math", "Science" }, stats.ActiveTopics);
    }

    [Fact]
    public void GetServerStats_AllActiveLobbies_ReturnsCorrectStats()
    {
        var lobbies = new List<Lobby>
        {
            new Lobby { State = LobbyState.Started, Players = new List<Player>{ new Player() }, LobbySettings = new LobbySettings { Topic = "History" } },
            new Lobby { State = LobbyState.Started, Players = new List<Player>{ new Player(), new Player() }, LobbySettings = new LobbySettings { Topic = "Geography" } }
        };
        _lobbyStoreMock.Setup(s => s.GetAll()).Returns(lobbies);

        var result = _controller.GetServerStats();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stats = Assert.IsType<ServerStats>(okResult.Value);

        Assert.Equal(2, stats.TotalLobbies);
        Assert.Equal(2, stats.ActiveLobbies);
        Assert.Equal(0, stats.WaitingLobbies);
        Assert.Equal(3, stats.TotalPlayers);
        Assert.Equal(1.5, stats.AveragePlayersPerLobby);
        Assert.Equal(new[] { "Geography", "History" }, stats.ActiveTopics);
    }

    [Fact]
    public void GetServerStats_MixedLobbiesWithDuplicateTopics_ReturnsDistinctTopicsSorted()
    {
        var lobbies = new List<Lobby>
        {
            new Lobby { State = LobbyState.Started, Players = new List<Player>{ new Player() }, LobbySettings = new LobbySettings { Topic = "Science" } },
            new Lobby { State = LobbyState.Waiting, Players = new List<Player>{ new Player(), new Player() }, LobbySettings = new LobbySettings { Topic = "science" } },
            new Lobby { State = LobbyState.Waiting, Players = new List<Player>{ new Player() }, LobbySettings = new LobbySettings { Topic = "Math" } }
        };
        _lobbyStoreMock.Setup(s => s.GetAll()).Returns(lobbies);

        var result = _controller.GetServerStats();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stats = Assert.IsType<ServerStats>(okResult.Value);

        Assert.Equal(3, stats.TotalLobbies);
        Assert.Equal(1, stats.ActiveLobbies);
        Assert.Equal(2, stats.WaitingLobbies);
        Assert.Equal(4, stats.TotalPlayers);
        Assert.InRange(stats.AveragePlayersPerLobby, 1.32, 1.34);
        Assert.Equal(new[] { "Math", "Science" }, stats.ActiveTopics);
    }

    [Fact]
    public void GetServerStats_LobbiesWithNullOrEmptyTopic_IgnoresForActiveTopics()
    {
        var lobbies = new List<Lobby>
        {
            new Lobby { State = LobbyState.Started, Players = new List<Player>{ new Player() }, LobbySettings = null },
            new Lobby { State = LobbyState.Waiting, Players = new List<Player>{ new Player() }, LobbySettings = new LobbySettings { Topic = "" } },
            new Lobby { State = LobbyState.Waiting, Players = new List<Player>{ new Player() }, LobbySettings = new LobbySettings { Topic = "ValidTopic" } }
        };
        _lobbyStoreMock.Setup(s => s.GetAll()).Returns(lobbies);

        var result = _controller.GetServerStats();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stats = Assert.IsType<ServerStats>(okResult.Value);

        Assert.Single(stats.ActiveTopics);
        Assert.Contains("ValidTopic", stats.ActiveTopics);
    }
}
