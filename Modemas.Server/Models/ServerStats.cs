namespace Modemas.Server.Models;

public record ServerStats(
    int TotalLobbies,
    int ActiveLobbies,
    int WaitingLobbies,
    int TotalPlayers,
    double AveragePlayersPerLobby,
    IEnumerable<string> ActiveTopics
);
