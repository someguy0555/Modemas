import { useEffect, useState } from "react";

/**
 * View for creating a new lobby or joining an existing one.
 */
export default function MainMenuView({ connection, setGlobalPlayerName, setGlobalLobbyId }) {
    // ***********************************************
    // Local state
    // ***********************************************
    const [playerName, setPlayerName] = useState("");
    const [lobbyId, setLobbyId] = useState("");

    // ***********************************************
    // Server stats state
    // ***********************************************
    const [serverStats, setServerStats] = useState(null);

    // ***********************************************
    // Functions that will be called from the backend by SignalR
    // ***********************************************
    const createLobby = async (hostName) => {
        if (connection && hostName) {
            await connection.invoke("CreateLobby", hostName);
        }
    };

    const joinLobby = async (lobbyId, playerName) => {
        if (connection && lobbyId && playerName) {
            await connection.invoke("JoinLobby", lobbyId, playerName);
        }
    };

    // ***********************************************
    // Fetch server stats from API
    // ***********************************************
    const fetchServerStats = async () => {
        try {
            const response = await fetch("/api/serverstats");
            if (!response.ok) throw new Error("Failed to fetch server stats");
            const data = await response.json();
            setServerStats(data);
        } catch (err) {
            console.error("Error fetching server stats:", err);
        }
    };

    // ***********************************************
    // Fetch stats when the component mounts
    // ***********************************************
    useEffect(() => {
        fetchServerStats();

        const interval = setInterval(fetchServerStats, 30000);
        return () => clearInterval(interval);
    }, []);

    return (
        <div>
            <h2>Main Menu</h2>

            {/* Player Name Input */}
            <input
                type="text"
                placeholder="Enter your name"
                value={playerName}
                onChange={(e) => setPlayerName(e.target.value)}
            />
            <br />

            {/* Lobby ID Input */}
            <input
                type="text"
                placeholder="Enter Lobby ID"
                value={lobbyId}
                onChange={(e) => setLobbyId(e.target.value)}
            />
            <br />

            {/* Buttons */}
            <button
                onClick={() => {
                    setGlobalPlayerName(playerName);
                    console.log(`Creating lobby as ${playerName}.`);
                    createLobby(playerName);
                }}
            >
                Create Lobby
            </button>
            <button
                onClick={() => {
                    setGlobalPlayerName(playerName);
                    setGlobalLobbyId(lobbyId);
                    console.log(`Joining lobby ${lobbyId} as ${playerName}.`);
                    joinLobby(lobbyId, playerName);
                }}
            >
                Join Lobby
            </button>

            {/* Server Stats Display */}
            {serverStats && (
                <div style={{ marginTop: "20px" }}>
                    <h3>Server Stats</h3>
                    <p>Total Lobbies: {serverStats.totalLobbies}</p>
                    <p>Active Lobbies: {serverStats.activeLobbies}</p>
                    <p>Total Players: {serverStats.totalPlayers}</p>
                    <p>Average Players per Lobby: {serverStats.averagePlayersPerLobby}</p>
                    <p>Active Topics: {serverStats.activeTopics.join(", ")}</p>
                </div>
            )}
        </div>
    );
}
