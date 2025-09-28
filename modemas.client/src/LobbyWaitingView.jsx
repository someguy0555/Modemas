/**
 * View for waiting in a lobby before the match starts.
 */
export default function LobbyWaitingView({ connection, lobbyId, lobbyState, playerName, players, isHost }) {
    const startMatch = async (lobbyId) => {
        if (connection) {
            await connection.invoke("StartMatch", lobbyId);
        }
    };

    return (
        <div>
            <p>Lobby ID: {lobbyId}</p>
            <p>Lobby State: {lobbyState}</p>
            <p>Player name: {playerName}</p>

            <h2>Players:</h2>
            <ul>
                {players.map((p, i) => (
                    <li key={i}>{p}</li>
                ))}
            </ul>

            {isHost && <button onClick={() => startMatch(lobbyId)}>Start Match</button>}
        </div>
    );
}
