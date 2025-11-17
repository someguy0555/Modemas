using Moq;
using System.Text.Json;
using Modemas.Server.Models;
using Modemas.Server.Services;
using Modemas.Server.Interfaces;

public class MatchServiceTests
{
    private readonly Mock<ILobbyStore> _storeMock;
    private readonly Mock<ILobbyNotifier> _notifierMock;
    private readonly MatchService _service;

    public MatchServiceTests()
    {
        _storeMock = new Mock<ILobbyStore>();
        _notifierMock = new Mock<ILobbyNotifier>();
        _service = new MatchService(_storeMock.Object, _notifierMock.Object);
    }

    [Fact]
    public async Task StartMatch_LobbyNotFound_DoesNothing()
    {
        string lobbyId = "lobby1";
        _storeMock.Setup(s => s.Get(lobbyId)).Returns((Lobby?)null);

        await _service.StartMatch(lobbyId);

        _notifierMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartMatch_LobbyAlreadyStarted_DoesNothing()
    {
        string lobbyId = "lobby1";
        var fakeLobby = new Lobby { LobbyId = lobbyId, State = LobbyState.Started };
        _storeMock.Setup(s => s.Get(lobbyId)).Returns(fakeLobby);

        await _service.StartMatch(lobbyId);

        _notifierMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartMatch_NoQuestions_NotifiesFailure()
    {
        string lobbyId = "lobby1";
        var fakeLobby = new Lobby
        {
            LobbyId = lobbyId,
            Match = new LobbyMatch { Questions = new List<Question>() }
        };
        _storeMock.Setup(s => s.Get(lobbyId)).Returns(fakeLobby);

        _notifierMock.Setup(n => n.NotifyGroup(lobbyId, "MatchStartFailed", lobbyId, "No questions available."))
            .Returns(Task.CompletedTask);

        await _service.StartMatch(lobbyId);

        _notifierMock.Verify(n => n.NotifyGroup(lobbyId, "MatchStartFailed", lobbyId, "No questions available."), Times.Once);
    }

    [Fact]
    public async Task StartMatch_ValidLobby_ResetsPlayersAndStartsLoop()
    {
        string lobbyId = "lobby1";

        var questions = new List<Question>
        {
            new MultipleChoiceQuestion { Text = "Q1", TimeLimit = 0 }
        };

        var fakeLobby = new Lobby
        {
            LobbyId = lobbyId,
            State = LobbyState.Waiting,
            Players = new List<Player>
            {
                new Player { Name = "P1", HasAnsweredCurrent = true, QuestionScores = new List<ScoreEntry> { new(0,1,true) } },
                new Player { Name = "P2" }
            },
            Match = new LobbyMatch { Questions = questions }
        };

        _storeMock.Setup(s => s.Get(lobbyId)).Returns(fakeLobby);

        _notifierMock
            .Setup(n => n.NotifyGroup(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns(Task.CompletedTask);

        await _service.StartMatch(lobbyId);

        foreach (var p in fakeLobby.Players)
        {
            Assert.False(p.HasAnsweredCurrent);
            Assert.Empty(p.QuestionScores);
        }

        _notifierMock.Verify(n => n.NotifyGroup(lobbyId, "LobbyMatchStarted", lobbyId), Times.Once);
    }
}
