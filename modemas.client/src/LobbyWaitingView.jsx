import React, { useState, useEffect } from 'react';

/**
 * View for waiting in a lobby before the match starts.
 */
const topics = [
    'Math',
    'Science',
    'History',
    'Geography'
    // ...add more topics as needed...
];

export default function LobbyWaitingView({ connection, lobbyId, lobbyState, playerName, players, isHost }) {
    const [selectedTopic, setSelectedTopic] = useState('');
    const [starting, setStarting] = useState(false);
    const [error, setError] = useState('');
    const [matchStarted, setMatchStarted] = useState(false);

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
        setStarting(true);
        setError('');
        try {
            await connection.invoke("StartMatch", lobbyId, selectedTopic);
        } catch (e) {
            setError("Failed to start match.");
            setStarting(false);
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
                    <button
                        onClick={() => startMatch(lobbyId)}
                        disabled={!selectedTopic || starting}
                    >
                        {starting ? "Starting..." : "Start Match"}
                    </button>
                    {error && <p style={{ color: 'red' }}>{error}</p>}
                </div>
            )}
        </div>
    );
}
