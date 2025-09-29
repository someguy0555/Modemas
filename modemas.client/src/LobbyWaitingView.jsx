import { useEffect, useState } from "react";

/**
 * View for waiting in a lobby before the match starts.
 */
export default function LobbyWaitingView({ connection, lobbyId, lobbyState, playerName: initialPlayerName, players: initialPlayers, isHost }) {
    const [numberOfQuestions, setNumberOfQuestions] = useState(10);
    const [theme, setTheme] = useState("");
    const [questionTimer, setQuestionTimer] = useState(10);
    const [players, setPlayers] = useState(initialPlayers || []);
    const [playerName, setPlayerName] = useState(initialPlayerName);

    useEffect(() => {
        setPlayers(initialPlayers || []);
    }, [initialPlayers]);

    // Ensure playerName is set for host on mount if missing
    useEffect(() => {
        if (isHost && !playerName && players.length > 0) {
            setPlayerName(players[0]); // Host is always first in the list
        }
    }, [isHost, playerName, players]);

    useEffect(() => {
        if (connection) {
            const handler = (playersList) => {
                setPlayers([...playersList]);
            };
            connection.on("LobbyPlayersUpdated", handler);
            // Also refresh player list after match ends
            connection.on("MatchEnded", () => {
                // Optionally, you could re-request the player list here if needed
                // For now, just keep the handler active so any update is reflected
            });
            return () => {
                connection.off("LobbyPlayersUpdated", handler);
                connection.off("MatchEnded");
            };
        }
    }, [connection]);

    useEffect(() => {
        if (isHost && connection) {
            connection.on("LobbySettingsUpdated", (num, th, timer) => {
                setNumberOfQuestions(num);
                setTheme(th);
                setQuestionTimer(timer);
            });
        }
    }, [connection, isHost]);

    const startMatch = async (lobbyId) => {
        if (connection) {
            await connection.invoke("StartMatch", lobbyId);
        }
    };

    const updateSettings = async () => {
        if (connection) {
            await connection.invoke("UpdateLobbySettings", lobbyId, numberOfQuestions, theme, questionTimer);
        }
    };

    // Helper to display lobby state as a user-friendly string
    function getLobbyStateText(state) {
        if (state === 0) return "Waiting for host to start";
        // Add more states as needed
        return state;
    }

    return (
        <div>
            <div style={{ position: 'fixed', top: 16, left: 16, zIndex: 1000 }}>
                <div style={{ background: '#494949', color: '#fff', borderRadius: '8px', boxShadow: '0 2px 8px rgba(0,0,0,0.08)', padding: '12px 20px', fontWeight: 'bold', marginBottom: '12px', minWidth: '120px' }}>
                    Players: {players.length}
                </div>
                <div style={{ background: '#494949', color: '#fff', borderRadius: '8px', boxShadow: '0 2px 8px rgba(0,0,0,0.08)', padding: '12px 20px', minWidth: '120px' }}>
                    <div style={{ fontWeight: 'bold', marginBottom: '8px' }}>Players in Lobby:</div>
                    <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
                        {players.map((p, i) => (
                            <li key={i} style={{ padding: '2px 0' }}>{p}</li>
                        ))}
                    </ul>
                </div>
            </div>
            <p>Lobby ID: {lobbyId}</p>
            <p>Lobby State: {getLobbyStateText(lobbyState)}</p>
            <p>Player name: {playerName || (isHost && players.length > 0 ? players[0] : "")}</p>

            {isHost && (
                <>
                    <h2>Lobby Customization</h2>
                    <label>Theme</label>
                    <input value={theme} onChange={e => setTheme(e.target.value)} />
                    <br />
                    <label>Number of Questions:</label>
                    <input type="number" value={numberOfQuestions} onChange={e => setNumberOfQuestions(Number(e.target.value))} />
                    <br />
                    <label>Timer per Question (seconds):</label>
                    <input type="number" value={questionTimer} onChange={e => setQuestionTimer(Number(e.target.value))} />
                    <br />
                    <button onClick={updateSettings}>Save Settings</button>
                    <button onClick={() => startMatch(lobbyId)}>Start Match</button>
                </>
            )}
        </div>
    );
}
