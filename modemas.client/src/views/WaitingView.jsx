import { useEffect, useState } from "react";
import "./css/WaitingView.css";
import logo from "../assets/logo.svg";

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

        // Build settings record to send
        const newSettings = {
            numberOfQuestions: !generateNew ? 10 : Number(numberOfQuestions),
            questionTimerInSeconds: Number(questionTimer),
            topic: selectedTopic
        };

        try {
            await connection.invoke("UpdateLobbySettings", lobbyId, newSettings);
            console.log("Lobby settings updated:", newSettings);
        } catch (err) {
            console.error("Failed to update lobby settings:", err);
        }
    };

    // Host: kick a player
    const kickPlayer = async (targetPlayer) => {
        if (!connection) return;
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


    // Leave lobby (for the current player)
    const leaveLobby = async () => {
        if (!connection) return;
        const ok = confirm("Leave the lobby and return to the main menu?");
        if (!ok) return;
        try {
            await connection.stop();
        } catch (err) {
            console.error("Error while stopping connection:", err);
        } finally {
            window.location.reload();
        }
    };

    // ***********************************************
    // Listen for lobby settings updates from the server
    // ***********************************************
    useEffect(() => {
        if (!connection) return;

        const handleLobbySettingsUpdated = (settings) => {
            console.log("LobbySettingsUpdated received:", settings);
            setNumberOfQuestions(settings.numberOfQuestions);
            setQuestionTimer(settings.questionTimerInSeconds);
            setTopic(settings.topic);
        };

        connection.on("LobbySettingsUpdated", handleLobbySettingsUpdated);

        return () => {
            connection.off("LobbySettingsUpdated", handleLobbySettingsUpdated);
        };
    }, [connection]);

    // ***********************************************
    // Render
    // ***********************************************
    return (
        <div className="waiting-view">
            {/* Fixed leave button in the upper left corner */}
            <div className="leave-button-fixed">
                <button className="btn btn-secondary" onClick={leaveLobby} title="Leave lobby">
                    ← Leave
                </button>
            </div>

            <div className="waiting-container">
                <div className="main-column">
                    <img src={logo} alt="Kaput Logo" className="waiting-logo" />
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
                            {generateNew && (
                                <>
                                    <label className="label">Number of Questions:</label>
                                    <input
                                        className="settings-input"
                                        type="number"
                                        min={1}
                                        value={numberOfQuestions}
                                        onChange={(e) => setNumberOfQuestions(Number(e.target.value))}
                                    />
                                </>
                            )}
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
