import { useEffect, useState } from "react";

/**
 * View for waiting in a lobby before the match starts.
 */
export default function WaitingView({ connection, lobbyId, lobbyState, playerName, players, isHost }) {
    // ***********************************************
    // Local state
    // ***********************************************
    const [numberOfQuestions, setNumberOfQuestions] = useState(10);
    const [theme, setTheme] = useState("");
    const [questionTimer, setQuestionTimer] = useState(10);

    // ***********************************************
    // Functions that will be called from the backend by SignalR
    // ***********************************************
    const startMatch = async (lobbyId) => {
        if (connection) {
            await connection.invoke("StartVoting", lobbyId);
        }
    };

    const updateSettings = async () => {
        if (connection) {
            await connection.invoke("UpdateLobbySettings", lobbyId, numberOfQuestions, theme, questionTimer);
        }
    };

    useEffect(() => {
        if (isHost && connection) {
            connection.on("LobbySettingsUpdated", (num, th, timer) => {
                setNumberOfQuestions(num);
                setTheme(th);
                setQuestionTimer(timer);
            });
        }
    }, [connection, isHost]);

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

            {isHost && (
                <div>
                    <h3>Lobby Customization</h3>
                    <input 
                        type="text" 
                        placeholder="Theme"
                        value={theme} 
                        onChange={e => setTheme(e.target.value)}
                    />
                    <br />
                    Number of Questions:
                    <input 
                        type="number" 
                        min={1} 
                        value={numberOfQuestions} 
                        onChange={e => setNumberOfQuestions(Number(e.target.value))}
                    />
                    <br />
                    Timer per Question (seconds):
                    <input 
                        type="number" 
                        min={1} 
                        value={questionTimer} 
                        onChange={e => setQuestionTimer(Number(e.target.value))}
                    />
                    <br />
                    <button onClick={updateSettings}>Save Settings</button>
                    <button onClick={() => startMatch(lobbyId)}>Start Match</button>
                </div>
            )}
        </div>
    );
}
