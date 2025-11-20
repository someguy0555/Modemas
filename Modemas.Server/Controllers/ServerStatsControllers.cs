using Microsoft.AspNetCore.Mvc;

using Modemas.Server.Models;
using Modemas.Server.Interfaces;

namespace Modemas.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServerStatsController : ControllerBase
{
    private readonly ILobbyStore _lobbyStore;
    private readonly IStatisticsCalculator<Player, int> _playerStatsCalculator;

    public ServerStatsController(
        ILobbyStore lobbyStore,
        IStatisticsCalculator<Player, int> playerStatsCalculator)
    {
        _lobbyStore = lobbyStore;
        _playerStatsCalculator = playerStatsCalculator;
    }

    [HttpGet]
    public ActionResult<ServerStats> GetServerStats()
    {
        var lobbies = _lobbyStore.GetAll().ToList();

        if (!lobbies.Any())
        {
            return Ok(new ServerStats(
                TotalLobbies: 0,
                ActiveLobbies: 0,
                WaitingLobbies: 0,
                TotalPlayers: 0,
                AveragePlayersPerLobby: 0,
                ActiveTopics: Enumerable.Empty<string>()
            ));
        }

        var totalLobbies = lobbies.Count;
        var activeLobbies = lobbies.Count(l => l.State == LobbyState.Started);
        var waitingLobbies = totalLobbies - activeLobbies;

        var allPlayers = lobbies.SelectMany(l => l.Players).ToList();
        var totalPlayers = allPlayers.Count;
        var averagePlayers = lobbies.Average(l => l.Players.Count);

        var activeTopics = lobbies
            .Where(l => !string.IsNullOrWhiteSpace(l.LobbySettings?.Topic))
            .Select(l => l.LobbySettings!.Topic!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t);

        var totalScore = _playerStatsCalculator.CalculateTotal(allPlayers    , p => p.TotalPoints);
        var averageScore = _playerStatsCalculator.CalculateAverage(allPlayers, p => p.TotalPoints);
        var topPlayer = _playerStatsCalculator.FindTopPerformer(allPlayers   , p => p.TotalPoints);

        var stats = new ServerStats(
            TotalLobbies: totalLobbies,
            ActiveLobbies: activeLobbies,
            WaitingLobbies: waitingLobbies,
            TotalPlayers: totalPlayers,
            AveragePlayersPerLobby: Math.Round(averagePlayers, 2),
            ActiveTopics: activeTopics
        );

        return Ok(stats);
    }
}
