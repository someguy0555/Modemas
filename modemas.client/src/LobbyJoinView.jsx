import { useEffect, useState } from "react";

/**
 * View for creating a new lobby or joining an existing one.
 */
export default function LobbyJoinView({ connection }) {
    const [playerName, setPlayerName] = useState("");
    const [lobbyId, setLobbyId] = useState("");
    const [players, setPlayers] = useState([]);
    const [inLobby, setInLobby] = useState(false);

    // Debug: Log players state changes to verify UI reactivity
    useEffect(() => {
        console.log("React players state updated:", players);
    }, [players]);

    // Register LobbyPlayersUpdated handler once, always update players state
    useEffect(() => {
        if (connection) {
            const handler = (playersList) => {
                setPlayers([...playersList]); // Force new array reference for React
            };
            connection.on("LobbyPlayersUpdated", handler);
            return () => {
                connection.off("LobbyPlayersUpdated", handler);
            };
        }
    }, [connection]);

    // Only update lobbyId and inLobby from create/join events
    useEffect(() => {
        if (connection) {
            const createdHandler = (lobbyId) => {
                setLobbyId(lobbyId);
                setInLobby(true);
            };
            const joinedHandler = (lobbyId) => {
                setLobbyId(lobbyId);
                setInLobby(true);
            };
            connection.on("LobbyCreated", createdHandler);
            connection.on("LobbyJoined", joinedHandler);
            return () => {
                connection.off("LobbyCreated", createdHandler);
                connection.off("LobbyJoined", joinedHandler);
            };
        }
    }, [connection]);

    const createLobby = async () => {
        if (connection && playerName) {
            await connection.invoke("CreateLobby", playerName);
        }
    };

    const joinLobby = async (lobbyId, playerName) => {
        if (connection && lobbyId && playerName) {
            await connection.invoke("JoinLobby", lobbyId, playerName);
        }
    };


    return (
        <div>
            {!inLobby ? (
                <>
                    <button onClick={() => createLobby()}>Create Lobby</button>
                    <input
                        type="text"
                        placeholder="Enter your name"
                        value={playerName}
                        onChange={(e) => setPlayerName(e.target.value)}
                    />
                    <input
                        type="text"
                        placeholder="Enter Lobby ID"
                        value={lobbyId}
                        onChange={(e) => setLobbyId(e.target.value)}
                    />
                    <button onClick={() => joinLobby(lobbyId, playerName)}>Join Lobby</button>
                </>
            ) : (
                <div style={{ position: 'fixed', top: 16, left: 16, zIndex: 1000 }}>
                    <div style={{ background: '#f0f0f0', borderRadius: '8px', boxShadow: '0 2px 8px rgba(0,0,0,0.08)', padding: '12px 20px', fontWeight: 'bold', marginBottom: '12px', minWidth: '120px' }}>
                        Players: {players.length}
                    </div>
                    <div style={{ background: '#fff', borderRadius: '8px', boxShadow: '0 2px 8px rgba(0,0,0,0.08)', padding: '12px 20px', minWidth: '120px' }}>
                        <div style={{ fontWeight: 'bold', marginBottom: '8px' }}>Players in Lobby:</div>
                        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
                            {players.map((name, idx) => (
                                <li key={idx} style={{ padding: '2px 0' }}>{name}</li>
                            ))}
                        </ul>
                    </div>
                </div>
            )}
        </div>
    );
}
