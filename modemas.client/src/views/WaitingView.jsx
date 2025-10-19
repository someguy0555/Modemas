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
    const [topics, setTopics] = useState([]); // fetched from backend
    const [generateNew, setGenerateNew] = useState(false);
    const [newTopicName, setNewTopicName] = useState("");

    // ***********************************************
    // Load existing topics from backend
    // ***********************************************
    useEffect(() => {
        async function fetchTopics() {
            try {
                const res = await fetch("/api/questions/topics");
                if (!res.ok) throw new Error("Failed to fetch topics");
                const data = await res.json();
                setTopics(data || []);
            } catch (err) {
                console.error("Error loading topics:", err);
            }
        }
        fetchTopics();
    }, []);

    // ***********************************************
    // Functions called from backend via SignalR
    // ***********************************************
    const startMatch = async () => {
        if (connection) {
            await connection.invoke("StartVoting", lobbyId);
        }
    };

    const updateSettings = async () => {
        if (!connection) return;

        const selectedTopic = generateNew ? newTopicName.trim() : topic;
        if (!selectedTopic) {
            alert("Please select or enter a topic first!");
            return;
        }

        await connection.invoke("UpdateLobbySettings", lobbyId, numberOfQuestions, questionTimer, selectedTopic);
    };

    // New: kick a player (host action)
    const kickPlayer = async (targetPlayer) => {
        if (!connection) {
            console.warn("No connection available to kick player.");
            return;
        }
        if (!targetPlayer) return;
        if (!confirm(`Kick player '${targetPlayer}' from the lobby?`)) return;

        try {
            await connection.invoke("KickPlayer", lobbyId, targetPlayer);
            console.log(`KickPlayer invoked for ${targetPlayer}`);
        } catch (err) {
            console.error("Failed to invoke KickPlayer:", err);
            alert("Failed to kick player. See console for details.");
        }
    };

    // Listen for lobby updates from the server
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
                <div className="main-column">
                    <div className="lobby-info">
                        <p className="lobby-meta">Lobby ID: <strong>{lobbyId}</strong></p>
                        <p className="lobby-meta">Lobby State: <strong>{lobbyState}</strong></p>
                        <p className="lobby-meta">Player name: <strong>{playerName}</strong></p>
                    </div>

                    {isHost && (
                        <div className="host-controls card">
                            <h3>Lobby Customization</h3>

                            {/* --- Topic selection --- */}
                            <label className="label">Topic:</label>
                            {!generateNew ? (
                                <select
                                    className="topic-select"
                                    value={topic}
                                    onChange={(e) => setTopic(e.target.value)}
                                >
                                    <option value="">-- Select existing topic --</option>
                                    {topics.map((t) => (
                                        <option key={t} value={t}>
                                            {t}
                                        </option>
                                    ))}
                                </select>
                            ) : (
                                <input
                                    type="text"
                                    className="settings-input"
                                    placeholder="Enter new topic name..."
                                    value={newTopicName}
                                    onChange={(e) => setNewTopicName(e.target.value)}
                                />
                            )}

                            <div style={{ margin: "0.5em 0" }}>
                                <label>
                                    <input
                                        type="checkbox"
                                        checked={generateNew}
                                        onChange={(e) => {
                                            setGenerateNew(e.target.checked);
                                            setTopic("");
                                            setNewTopicName("");
                                        }}
                                    />{" "}
                                    Generate new topic
                                </label>
                            </div>

                            {/* --- Question + timer settings --- */}
                            <label className="label">Number of Questions:</label>
                            <input
                                className="settings-input"
                                type="number"
                                min={1}
                                value={numberOfQuestions}
                                onChange={(e) => setNumberOfQuestions(Number(e.target.value))}
                            />
                            <label className="label">Timer per Question (seconds):</label>
                            <input
                                className="settings-input"
                                type="number"
                                min={1}
                                value={questionTimer}
                                onChange={(e) => setQuestionTimer(Number(e.target.value))}
                            />
                            <div className="actions">
                                <button className="btn btn-primary" onClick={updateSettings}>
                                    Save Settings
                                </button>
                                <button className="btn btn-secondary" onClick={startMatch}>
                                    Start Match
                                </button>
                            </div>
                        </div>
                    )}
                </div>

                {/* --- Player list --- */}
                <div className="players-column">
                    <h2>Players:</h2>
                    <ul className="players-list">
                        {players.map((p, i) => (
                            <li key={i} className="player-item">
                                <span>{p}</span>
                                {isHost && p !== playerName && (
                                    <button
                                        className="btn btn-danger"
                                        style={{ float: "right" }}
                                        onClick={() => kickPlayer(p)}
                                        title={`Kick ${p}`}
                                    >
                                        Kick
                                    </button>
                                )}
                            </li>
                        ))}
                    </ul>
                </div>
            </div>
        </div>
    );
}
