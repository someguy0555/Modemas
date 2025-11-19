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

    // Answer questions
    [Fact]
    public async Task AnswerQuestion_LobbyNotFound_NotifiesError()
    {
        string connId = "c1", lobbyId = "l1";
        _storeMock.Setup(s => s.Get(lobbyId)).Returns((Lobby?)null);

        var json = JsonDocument.Parse("null").RootElement;
        await _service.AnswerQuestion(connId, lobbyId, json);

        _notifierMock.Verify(n => n.NotifyClient(connId, "Error", "Lobby not found"), Times.Once);
    }

    [Fact]
    public async Task AnswerQuestion_PlayerNotFound_NotifiesError()
    {
        string connId = "c1", lobbyId = "l1";
        var fakeLobby = new Lobby
        {
            LobbyId = lobbyId,
            Players = new List<Player>(),
            Match = new LobbyMatch { Questions = new List<Question> { new MultipleChoiceQuestion() } }
        };
        _storeMock.Setup(s => s.Get(lobbyId)).Returns(fakeLobby);

        var json = JsonDocument.Parse("null").RootElement;
        await _service.AnswerQuestion(connId, lobbyId, json);

        _notifierMock.Verify(n => n.NotifyClient(connId, "Error", "You are not in this lobby"), Times.Once);
    }

    [Fact]
    public async Task AnswerQuestion_NoActiveQuestion_NotifiesError()
    {
        string connId = "c1", lobbyId = "l1";
        var fakeLobby = new Lobby
        {
            LobbyId = lobbyId,
            Players = new List<Player> { new Player { ConnectionId = connId } },
            Match = new LobbyMatch { Questions = new List<Question>() }
        };
        _storeMock.Setup(s => s.Get(lobbyId)).Returns(fakeLobby);

        var json = JsonDocument.Parse("5").RootElement;
        await _service.AnswerQuestion(connId, lobbyId, json);

        _notifierMock.Verify(n => n.NotifyClient(connId, "Error", "No active question"), Times.Once);
    }

    [Fact]
    public async Task AnswerQuestion_PlayerAlreadyAnswered_NotifiesError()
    {
        string connId = "c1", lobbyId = "l1";
        var fakeLobby = new Lobby
        {
            LobbyId = lobbyId,
            Players = new List<Player> { new Player { ConnectionId = connId, HasAnsweredCurrent = true } },
            Match = new LobbyMatch { Questions = new List<Question> { new MultipleChoiceQuestion() } }
        };
        _storeMock.Setup(s => s.Get(lobbyId)).Returns(fakeLobby);

        var json = JsonDocument.Parse("0").RootElement;
        await _service.AnswerQuestion(connId, lobbyId, json);

        _notifierMock.Verify(n => n.NotifyClient(connId, "Error", "You already answered this question"), Times.Once);
    }

    [Fact]
    public async Task AnswerQuestion_CorrectAnswer_AddsScore_AndNotifiesAccepted()
    {
        string connId = "c1", lobbyId = "l1";
        var fakeQuestion = new TrueFalseQuestion
        {
            Points = 100,
            CorrectAnswer = true
        };
        var player = new Player { ConnectionId = connId };
        var fakeLobby = new Lobby
        {
            LobbyId = lobbyId,
            Players = new List<Player> { player },
            Match = new LobbyMatch
            {
                CurrentQuestionIndex = 0,
                Questions = new List<Question> { fakeQuestion }
            }
        };

        _storeMock.Setup(s => s.Get(lobbyId)).Returns(fakeLobby);
        _notifierMock.Setup(n => n.NotifyClient(connId, "AnswerAccepted", It.IsAny<ScoreEntry>()))
            .Returns(Task.CompletedTask);

        var json = JsonDocument.Parse("true").RootElement;
        await _service.AnswerQuestion(connId, lobbyId, json);

        _notifierMock.Verify(n => n.NotifyClient(connId, "AnswerAccepted", It.IsAny<ScoreEntry>()), Times.Once);
        Assert.True(player.HasAnsweredCurrent);
        Assert.Single(player.QuestionScores);
    }

    [Fact]
    public async Task AnswerQuestion_IsCorrectThrows_NotifiesError()
    {
        string connId = "c1", lobbyId = "l1";
        var fakeQuestion = new MultipleChoiceQuestion
        {
            Points = 100,
            CorrectAnswerIndex = 0
        };
        var player = new Player { ConnectionId = connId };
        var fakeLobby = new Lobby
        {
            LobbyId = lobbyId,
            Players = new List<Player> { player },
            Match = new LobbyMatch
            {
                CurrentQuestionIndex = 0,
                Questions = new List<Question> { fakeQuestion }
            }
        };

        _storeMock.Setup(s => s.Get(lobbyId)).Returns(fakeLobby);
        _notifierMock.Setup(n => n.NotifyClient(connId, "Error", It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var json = JsonDocument.Parse("\"invalid\"").RootElement;
        await _service.AnswerQuestion(connId, lobbyId, json);

        _notifierMock.Verify(n => n.NotifyClient(connId, "Error", It.IsAny<string>()), Times.Once);
    }
}
