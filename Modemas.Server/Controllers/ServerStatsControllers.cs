using Microsoft.AspNetCore.Mvc;

using Modemas.Server.Models;
using Modemas.Server.Services;

namespace Modemas.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServerStatsController : ControllerBase
{
    private readonly LobbyStore _lobbyStore;

    public ServerStatsController(LobbyStore lobbyStore)
    {
        _lobbyStore = lobbyStore;
    }

    [HttpGet]
    public ActionResult<ServerStats> GetServerStats()
    {
        var lobbies = _lobbyStore.ToList();

        if (lobbies.Count == 0)
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
        var totalPlayers = lobbies.Sum(l => l.Players.Count);
        var averagePlayers = lobbies.Average(l => l.Players.Count);
        var activeTopics = lobbies
            .Where(l => !string.IsNullOrWhiteSpace(l.LobbySettings?.Topic))
            .Select(l => l.LobbySettings!.Topic!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t);

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
