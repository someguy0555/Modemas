using Modemas.Server.Models;

public class LobbyTests
{
    [Fact]
    public void LobbyId_SetValidValue_SetsCorrectly()
    {
        var lobby = new Lobby();
        lobby.LobbyId = "abc123";
        Assert.Equal("abc123", lobby.LobbyId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LobbyId_SetInvalidValue_Throws(string value)
    {
        var lobby = new Lobby();
        var ex = Assert.Throws<ArgumentException>(() => lobby.LobbyId = value);
        Assert.Equal("Lobby ID cannot be empty.", ex.Message);
    }

    [Fact]
    public void HostConnectionId_SetValidValue_SetsCorrectly()
    {
        var lobby = new Lobby();
        lobby.HostConnectionId = "conn1";
        Assert.Equal("conn1", lobby.HostConnectionId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HostConnectionId_SetInvalidValue_Throws(string value)
    {
        var lobby = new Lobby();
        var ex = Assert.Throws<ArgumentException>(() => lobby.HostConnectionId = value);
        Assert.Equal("Host connection ID cannot be empty.", ex.Message);
    }

    [Fact]
    public void State_SetValidValue_SetsCorrectly()
    {
        var lobby = new Lobby();
        lobby.State = LobbyState.Started;
        Assert.Equal(LobbyState.Started, lobby.State);
    }

    [Fact]
    public void State_SetInvalidValue_Throws()
    {
        var lobby = new Lobby();
        var ex = Assert.Throws<ArgumentException>(() => lobby.State = (LobbyState)999);
        Assert.Equal("Invalid lobby state.", ex.Message);
    }

    [Fact]
    public void Match_SetValidValue_SetsCorrectly()
    {
        var lobby = new Lobby();
        var match = new LobbyMatch();
        lobby.Match = match;
        Assert.Equal(match, lobby.Match);
    }

    [Fact]
    public void Match_SetNull_Throws()
    {
        var lobby = new Lobby();
        Assert.Throws<ArgumentNullException>(() => lobby.Match = null!);
    }

    [Fact]
    public void Players_SetValidValue_SetsCorrectly()
    {
        var lobby = new Lobby();
        var players = new List<Player> { new Player { Name = "Alice", ConnectionId = "1" } };
        lobby.Players = players;
        Assert.Equal(players, lobby.Players);
        Assert.Equal(1, lobby.TotalPlayers);
    }

    [Fact]
    public void Players_SetNull_SetsEmptyList()
    {
        var lobby = new Lobby();
        lobby.Players = null!;
        Assert.NotNull(lobby.Players);
        Assert.Empty(lobby.Players);
    }

    [Fact]
    public void LobbySettings_SetValidValue_SetsCorrectly()
    {
        var lobby = new Lobby();
        var settings = new LobbySettings(NumberOfQuestions: 10, QuestionTimerInSeconds: 30, Topic: "Math");
        lobby.LobbySettings = settings;
        Assert.Equal(settings, lobby.LobbySettings);
    }

    [Fact]
    public void LobbySettings_SetNull_SetsDefault()
    {
        var lobby = new Lobby();
        lobby.LobbySettings = null!;
        Assert.NotNull(lobby.LobbySettings);
        Assert.Equal(5, lobby.LobbySettings.NumberOfQuestions);
        Assert.Equal(15, lobby.LobbySettings.QuestionTimerInSeconds);
        Assert.Equal("", lobby.LobbySettings.Topic);
    }
}
