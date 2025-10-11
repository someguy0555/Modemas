import { useEffect, useState } from "react";

/**
 * View for waiting in a lobby before the match starts.
 */
export default function WaitingView({ connection, lobbyId, lobbyState, playerName, players, isHost }) {
    // ***********************************************
    // Local state
    // ***********************************************
    const [numberOfQuestions, setNumberOfQuestions] = useState(10);
    const [topic, setTopic] = useState("");
    const [questionTimer, setQuestionTimer] = useState(10);

    // Available topics (matches files in Modemas.Server/)
    const topics = ["Math", "Science", "History", "Geography"];

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
            // send topic instead of theme
            await connection.invoke("UpdateLobbySettings", lobbyId, numberOfQuestions, topic, questionTimer);
        }
    };

    useEffect(() => {
        if (isHost && connection) {
            connection.on("LobbySettingsUpdated", (num, tp, timer) => {
                setNumberOfQuestions(num);
                setTopic(tp);
                setQuestionTimer(timer);
            });
        }
    }, [connection, isHost]);

    return (
        <div className="waiting-view">
            <div className="waiting-container">
                <div className="lobby-info">
                    <p className="lobby-meta">Lobby ID: <strong>{lobbyId}</strong></p>
                    <p className="lobby-meta">Lobby State: <strong>{lobbyState}</strong></p>
                    <p className="lobby-meta">Player name: <strong>{playerName}</strong></p>
                </div>

                <div className="players-column">
                    <h2>Players:</h2>
                    <ul className="players-list">
                        {players.map((p, i) => (
                            <li key={i} className="player-item">{p}</li>
                        ))}
                    </ul>
                </div>

                {isHost && (
                    <div className="host-controls card">
                        <h3>Lobby Customization</h3>

                        <label className="label">Topic:</label>
                        <br />
                        <select className="topic-select" value={topic} onChange={e => setTopic(e.target.value)}>
                            <option value="">-- Select topic --</option>
                            {topics.map((t) => (
                                <option key={t} value={t}>{t}</option>
                            ))}
                        </select>
                        <br />

                        <label className="label">Number of Questions:</label>
                        <input 
                            className="settings-input"
                            type="number" 
                            min={1} 
                            value={numberOfQuestions} 
                            onChange={e => setNumberOfQuestions(Number(e.target.value))}
                        />
                        <br />
                        <label className="label">Timer per Question (seconds):</label>
                        <input 
                            className="settings-input"
                            type="number" 
                            min={1} 
                            value={questionTimer} 
                            onChange={e => setQuestionTimer(Number(e.target.value))}
                        />
                        <br />
                        <div className="actions">
                            <button className="btn btn-primary" onClick={updateSettings}>Save Settings</button>
                            <button className="btn btn-secondary" onClick={() => startMatch(lobbyId)}>Start Match</button>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}
