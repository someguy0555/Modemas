import { useEffect, useState } from "react";

/**
 * View for waiting in a lobby before the match starts.
 */

const topics = [
    'Math',
    'Science',
    'History',
    'Geography'
];
export default function LobbyWaitingView({ connection, lobbyId, lobbyState, playerName, players, isHost }) {
    const [selectedTopic, setSelectedTopic] = useState('');
    const [starting, setStarting] = useState(false);
    const [error, setError] = useState('');
    const [matchStarted, setMatchStarted] = useState(false);
    
    const [numberOfQuestions, setNumberOfQuestions] = useState(10);
    const [theme, setTheme] = useState("");
    const [questionTimer, setQuestionTimer] = useState(10);

    useEffect(() => {
        if (!connection) return;
        const handleMatchStarted = (state, topic) => {
            setMatchStarted(true);
            setStarting(false);
            setError('');
        };
        const handleError = (msg) => {
            setError(msg);
            setStarting(false);
        };
        connection.on("LobbyMatchStarted", handleMatchStarted);
        connection.on("Error", handleError);
        return () => {
            connection.off("LobbyMatchStarted", handleMatchStarted);
            connection.off("Error", handleError);
        };
    }, [connection]);
    const startMatch = async (lobbyId) => {
        if (connection && selectedTopic) {
            setStarting(true);
            setError('');
            try {
                await connection.invoke("StartMatch", lobbyId, selectedTopic);
            } catch (e) {
                setError("Failed to start match.");
                setStarting(false);
            }
        } else {
            setError("Please select a topic before starting the match.");
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
                </div>
            )}

            {isHost && !matchStarted && (
                <div>
                    <h3>Select topic:</h3>
                    <select
                        value={selectedTopic}
                        onChange={e => setSelectedTopic(e.target.value)}
                        disabled={starting}
                    >
                        <option value="" disabled>Select topic</option>
                        {topics.map(topic => (
                            <option key={topic} value={topic}>{topic}</option>
                        ))}
                    </select>
                    <button onClick={() => startMatch(lobbyId)} disabled={starting || !selectedTopic}>Start Match</button>
                    {error && <p style={{ color: 'red' }}>{error}</p>}
                </div>
            )}
            
        </div>
    );
}
