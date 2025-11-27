using Moq;
using Modemas.Server.Models;
using Modemas.Server.Interfaces;

public class LobbyManagerTests
{
    private readonly Mock<ILobbyStore> _storeMock;
    private readonly LobbyManager _manager;

    public LobbyManagerTests()
    {
        _storeMock = new Mock<ILobbyStore>();
        _manager = new LobbyManager(_storeMock.Object);
    }

    private Lobby CreateTestLobby(string hostId = "host-1")
    {
        return new Lobby
        {
            LobbyId = "abcd1234",
            HostConnectionId = hostId,
            Players = new List<Player>()
        };
    }

    [Fact]
    public void CreateLobby_CreatesLobbyWithHost_AndStoresIt()
    {
        var lobby = _manager.CreateLobby("host-xyz");

        Assert.NotNull(lobby);
        Assert.Equal("host-xyz", lobby.HostConnectionId);
        Assert.Equal(8, lobby.LobbyId.Length);

        _storeMock.Verify(s => s.Add(It.Is<Lobby>(l => l == lobby)), Times.Once);
    }

    [Fact]
    public void AddPlayer_AddsPlayer_WhenUnique()
    {
        var lobby = CreateTestLobby();

        var result = _manager.AddPlayer(lobby, "c1", "Alice");

        Assert.True(result);
        Assert.Single(lobby.Players);
        Assert.Equal("Alice", lobby.Players[0].Name);
        Assert.Equal("c1", lobby.Players[0].ConnectionId);
    }

    [Fact]
    public void AddPlayer_Fails_WhenNameAlreadyExists()
    {
        var lobby = CreateTestLobby();
        lobby.Players.Add(new Player { Name = "Alice", ConnectionId = "c1" });

        var result = _manager.AddPlayer(lobby, "c2", "Alice");

        Assert.False(result);
        Assert.Single(lobby.Players);
    }

    [Fact]
    public void AddPlayer_Fails_WhenConnectionIdAlreadyExists()
    {
        var lobby = CreateTestLobby();
        lobby.Players.Add(new Player { Name = "Alice", ConnectionId = "c1" });

        var result = _manager.AddPlayer(lobby, "c1", "Bob");

        Assert.False(result);
        Assert.Single(lobby.Players);
    }

    [Fact]
    public void RemovePlayer_RemovesMatchingPlayer()
    {
        var lobby = CreateTestLobby();
        lobby.Players.Add(new Player { Name = "Alice", ConnectionId = "c1" });

        _manager.RemovePlayer(lobby, "c1");

        Assert.Empty(lobby.Players);
    }

    [Fact]
    public void RemovePlayer_NoAction_WhenPlayerNotFound()
    {
        var lobby = CreateTestLobby();
        lobby.Players.Add(new Player { Name = "Alice", ConnectionId = "c1" });

        _manager.RemovePlayer(lobby, "missing");

        Assert.Single(lobby.Players);
    }

    [Fact]
    public void UpdateSettings_UpdatesLobbySettings()
    {
        var lobby = CreateTestLobby();

        var newSettings = new LobbySettings(NumberOfQuestions: 10, QuestionTimerInSeconds: 30, Topic: "math");

        _manager.UpdateSettings(lobby, newSettings);

        Assert.Equal(10, lobby.LobbySettings.NumberOfQuestions);
        Assert.Equal(30, lobby.LobbySettings.QuestionTimerInSeconds);
        Assert.Equal("math", lobby.LobbySettings.Topic);
    }

    [Fact]
    public void RemoveLobby_CallsStoreRemove()
    {
        _manager.RemoveLobby("abcd1234");

        _storeMock.Verify(s => s.Remove("abcd1234"), Times.Once);
    }

    [Fact]
    public void GetLobby_ReturnsStoreValue()
    {
        var lobby = CreateTestLobby();

        _storeMock.Setup(s => s.Get("abcd1234")).Returns(lobby);

        var result = _manager.GetLobby("abcd1234");

        Assert.Equal(lobby, result);
    }

    [Fact]
    public void FindLobbyByConnection_ReturnsStoreValue()
    {
        var lobby = CreateTestLobby();

        _storeMock.Setup(s => s.FindByConnection("c1")).Returns(lobby);

        var result = _manager.FindLobbyByConnection("c1");

        Assert.Equal(lobby, result);
    }
}
