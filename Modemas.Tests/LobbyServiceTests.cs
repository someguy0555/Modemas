using Moq;
using Modemas.Server.Services;
using Modemas.Server.Models;
using Modemas.Server.Interfaces;
using Xunit;

public class LobbyServiceTests
{
    private readonly Mock<ILobbyManager> _managerMock;
    private readonly Mock<ILobbyNotifier> _notifierMock;
    private readonly Mock<IMatchService> _matchServiceMock;
    private readonly Mock<IQuestionGenerationService> _questionServiceMock;
    private readonly Mock<IQuestionRepository> _repoMock;
    private readonly LobbyService _service;

    public LobbyServiceTests()
    {
        _managerMock = new Mock<ILobbyManager>();
        _notifierMock = new Mock<ILobbyNotifier>();
        _matchServiceMock = new Mock<IMatchService>();
        _questionServiceMock = new Mock<IQuestionGenerationService>();
        _repoMock = new Mock<IQuestionRepository>();

        _service = new LobbyService(
            _managerMock.Object,
            _notifierMock.Object,
            _matchServiceMock.Object,
            _questionServiceMock.Object,
            _repoMock.Object
        );
    }

    [Fact]
    public async Task CreateLobby_Creates_AndNotifiesHost()
    {
        var fakeLobby = new Lobby { LobbyId = "lobby1", HostConnectionId = "conn1" };

        _managerMock.Setup(m => m.CreateLobby("conn1")).Returns(fakeLobby);
        _managerMock.Setup(m => m.GetLobby("lobby1")).Returns(fakeLobby);
        _managerMock.Setup(m => m.AddPlayer(fakeLobby, "conn1", "Host")).Returns(true);

        _notifierMock.Setup(n => n.AddPlayerToGroup("conn1", "lobby1")).Returns(Task.CompletedTask);
        _notifierMock.Setup(n => n.NotifyLobbyCreated("conn1", "lobby1")).Returns(Task.CompletedTask);
        _notifierMock.Setup(n => n.NotifyPlayerJoined("lobby1", "Host")).Returns(Task.CompletedTask);

        await _service.CreateLobby("conn1", "Host");

        _managerMock.Verify(m => m.CreateLobby("conn1"), Times.Once);
        _managerMock.Verify(m => m.AddPlayer(fakeLobby, "conn1", "Host"), Times.Once);
        _notifierMock.Verify(n => n.AddPlayerToGroup("conn1", "lobby1"), Times.AtLeastOnce);
        _notifierMock.Verify(n => n.NotifyLobbyCreated("conn1", "lobby1"), Times.Once);
        _notifierMock.Verify(n => n.NotifyPlayerJoined("lobby1", "Host"), Times.Once);
    }

    [Fact]
    public async Task UpdateLobbySettings_LobbyIsNull_NotifiesError()
    {
        string connectionId = "conn1";
        string lobbyId = "lobby1";
        string errorMessage = "Lobby not found";
        var settings = new LobbySettings { Topic = "History" };

        _managerMock.Setup(m => m.GetLobby(lobbyId)).Returns((Lobby?)null);
        _notifierMock.Setup(n => n.NotifyError(connectionId, errorMessage)).Returns(Task.CompletedTask);

        await _service.UpdateLobbySettings(connectionId, lobbyId, settings);

        _notifierMock.Verify(n => n.NotifyError(connectionId, errorMessage), Times.Once);
        _managerMock.Verify(m => m.UpdateSettings(It.IsAny<Lobby>(), It.IsAny<LobbySettings>()), Times.Never);
        _notifierMock.Verify(n => n.NotifyLobbySettingsUpdated(It.IsAny<string>(), It.IsAny<LobbySettings>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLobbySettings_NotHost_NotifiesError()
    {
        string connectionId = "conn2";
        string hostConnectionId = "host1";
        string lobbyId = "lobby1";
        string errorMessage = "Only the host can update settings";
        var settings = new LobbySettings { Topic = "Science" };
        var fakeLobby = new Lobby { LobbyId = lobbyId, HostConnectionId = hostConnectionId };

        _managerMock.Setup(m => m.GetLobby(lobbyId)).Returns(fakeLobby);
        _notifierMock.Setup(n => n.NotifyError(connectionId, errorMessage)).Returns(Task.CompletedTask);

        await _service.UpdateLobbySettings(connectionId, lobbyId, settings);

        _notifierMock.Verify(n => n.NotifyError(connectionId, errorMessage), Times.Once);
        _managerMock.Verify(m => m.UpdateSettings(It.IsAny<Lobby>(), It.IsAny<LobbySettings>()), Times.Never);
        _notifierMock.Verify(n => n.NotifyLobbySettingsUpdated(It.IsAny<string>(), It.IsAny<LobbySettings>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLobbySettings_HostUpdatesSettings_Success()
    {
        string hostConnectionId = "host1";
        string lobbyId = "lobby1";
        var settings = new LobbySettings { Topic = "Math", NumberOfQuestions = 5 };
        var fakeLobby = new Lobby { LobbyId = lobbyId, HostConnectionId = hostConnectionId };

        _managerMock.Setup(m => m.GetLobby(lobbyId)).Returns(fakeLobby);
        _notifierMock.Setup(n => n.NotifyLobbySettingsUpdated(lobbyId, settings)).Returns(Task.CompletedTask);

        await _service.UpdateLobbySettings(hostConnectionId, lobbyId, settings);

        _managerMock.Verify(m => m.UpdateSettings(fakeLobby, settings), Times.Once);
        _notifierMock.Verify(n => n.NotifyLobbySettingsUpdated(lobbyId, settings), Times.Once);
        _notifierMock.Verify(n => n.NotifyError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task KickPlayer_RemovesPlayerAndNotifies()
    {
        string targetPlayer = "target";
        string connectionId = "conn2";
        string lobbyId = "lobby1";
        string hostConnectionId = "host1";
        var fakeLobby = new Lobby
        {
            LobbyId = lobbyId,
            HostConnectionId = hostConnectionId,
            Players = new List<Player>
            {
                new() { Name = targetPlayer, ConnectionId = connectionId }
            }
        };

        _managerMock.Setup(m => m.GetLobby(lobbyId)).Returns(fakeLobby);
        _notifierMock.Setup(n => n.NotifyKicked(connectionId, It.IsAny<string>())).Returns(Task.CompletedTask);
        _notifierMock.Setup(n => n.NotifyGroup(lobbyId, "LobbyRemovePlayer", targetPlayer)).Returns(Task.CompletedTask);

        await _service.KickPlayer(hostConnectionId, lobbyId, targetPlayer);

        _managerMock.Verify(m => m.RemovePlayer(fakeLobby, connectionId), Times.Once);
        _notifierMock.Verify(n => n.NotifyKicked(connectionId, It.Is<string>(msg => msg.Contains("kicked"))), Times.Once);
        _notifierMock.Verify(n => n.NotifyGroup(lobbyId, "LobbyRemovePlayer", targetPlayer), Times.Once);
    }

    [Fact]
    public async Task KickPlayer_LobbyIsNull_NotifiesErrorToHost()
    {
        string targetPlayer = "target";
        string lobbyId = "lobby1";
        string hostConnectionId = "host1";

        _managerMock.Setup(m => m.GetLobby(lobbyId)).Returns((Lobby?)null);
        _notifierMock.Setup(n => n.NotifyError(hostConnectionId, It.IsAny<string>())).Returns(Task.CompletedTask);

        await _service.KickPlayer(hostConnectionId, lobbyId, targetPlayer);

        _notifierMock.Verify(n => n.NotifyError(hostConnectionId, "Lobby not found"), Times.Once);
    }

    [Fact]
    public async Task KickPlayer_SenderIsNotHost_NotifiesErrorToSender()
    {
        string targetPlayer = "target";
        string connectionId = "conn2";
        string lobbyId = "lobby1";
        string hostConnectionId = "host1";
        string senderId = "sender1";

        var fakeLobby = new Lobby
        {
            LobbyId = lobbyId,
            HostConnectionId = hostConnectionId,
            Players = new List<Player>
            {
                new() { Name = targetPlayer, ConnectionId = connectionId }
            }
        };

        _managerMock.Setup(m => m.GetLobby(lobbyId)).Returns(fakeLobby);
        _notifierMock.Setup(n => n.NotifyError(senderId, It.IsAny<string>())).Returns(Task.CompletedTask);

        await _service.KickPlayer(senderId, lobbyId, targetPlayer);

        _notifierMock.Verify(n => n.NotifyError(senderId, "Only the host can kick players"), Times.Once);
    }

    [Fact]
    public async Task KickPlayer_TargetNotFound_NotifiesError()
    {
        string targetPlayer = "NonExistentPlayer";
        string lobbyId = "lobby1";
        string hostConnectionId = "host1";

        var fakeLobby = new Lobby
        {
            LobbyId = lobbyId,
            HostConnectionId = hostConnectionId,
            Players = new List<Player>()
        };

        _managerMock.Setup(m => m.GetLobby(lobbyId)).Returns(fakeLobby);
        _notifierMock.Setup(n => n.NotifyError(hostConnectionId, It.IsAny<string>())).Returns(Task.CompletedTask);

        await _service.KickPlayer(hostConnectionId, lobbyId, targetPlayer);

        _notifierMock.Verify(n => n.NotifyError(hostConnectionId, "Player 'NonExistentPlayer' not found"), Times.Once);
    }

    [Fact]
    public async Task HandleDisconnect_HostClosesLobby()
    {
        string lobbyId = "lobby1";
        string hostConnectionId = "host1";
        string playerName = "Player2";
        string playerConnectionId = "conn2";
        string disconnectMessage = "Host disconnected. Lobby closed.";

        var fakeLobby = new Lobby
        {
            LobbyId = lobbyId,
            HostConnectionId = hostConnectionId,
            Players = new List<Player>
        {
            new() { Name = playerName, ConnectionId = playerConnectionId }
        }
        };

        _managerMock.Setup(m => m.FindLobbyByConnection(hostConnectionId)).Returns(fakeLobby);
        _notifierMock.Setup(n => n.NotifyKicked(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await _service.HandleDisconnect(hostConnectionId);

        _notifierMock.Verify(n => n.NotifyKicked(playerConnectionId, disconnectMessage), Times.Once);
        _managerMock.Verify(m => m.RemoveLobby(lobbyId), Times.Once);
    }

    [Fact]
    public async Task HandleDisconnect_PlayerLeavesLobby()
    {
        string lobbyId = "lobby1";
        string hostConnectionId = "host1";
        string playerName = "Player2";
        string playerConnectionId = "conn2";
        string groupEvent = "LobbyRemovePlayer";

        var fakeLobby = new Lobby
        {
            LobbyId = lobbyId,
            HostConnectionId = hostConnectionId,
            Players = new List<Player>
        {
            new() { Name = playerName, ConnectionId = playerConnectionId }
        }
        };

        _managerMock.Setup(m => m.FindLobbyByConnection(playerConnectionId)).Returns(fakeLobby);
        _notifierMock.Setup(n => n.NotifyGroup(lobbyId, groupEvent, playerName)).Returns(Task.CompletedTask);

        await _service.HandleDisconnect(playerConnectionId);

        _managerMock.Verify(m => m.RemovePlayer(fakeLobby, playerConnectionId), Times.Once);
        _notifierMock.Verify(n => n.NotifyGroup(lobbyId, groupEvent, playerName), Times.Once);
    }

    [Fact]
    public async Task HandleDisconnect_NoLobby_DoesNothing()
    {
        string connectionId = "conn9";

        _managerMock.Setup(m => m.FindLobbyByConnection(connectionId)).Returns((Lobby?)null);

        await _service.HandleDisconnect(connectionId);

        _managerMock.Verify(m => m.FindLobbyByConnection(connectionId), Times.Once);
        _notifierMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleDisconnect_PlayerNotFoundInLobby_DoesNothing()
    {
        string lobbyId = "lobby1";
        string hostConnectionId = "host1";
        string playerConnectionId = "conn2";

        var fakeLobby = new Lobby
        {
            LobbyId = lobbyId,
            HostConnectionId = hostConnectionId,
            Players = new List<Player>()
        };

        _managerMock.Setup(m => m.FindLobbyByConnection(playerConnectionId)).Returns(fakeLobby);

        await _service.HandleDisconnect(playerConnectionId);

        _managerMock.Verify(m => m.RemovePlayer(It.IsAny<Lobby>(), It.IsAny<string>()), Times.Never);
        _notifierMock.Verify(n => n.NotifyGroup(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
