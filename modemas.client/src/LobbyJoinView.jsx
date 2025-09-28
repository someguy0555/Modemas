import { useEffect, useState } from "react";

/**
 * View for creating a new lobby or joining an existing one.
 */
export default function LobbyJoinView({ connection }) {
    const [playerName, setPlayerName] = useState("");
    const [lobbyId, setLobbyId] = useState("");

    const createLobby = async () => {
        if (connection) {
            await connection.invoke("CreateLobby");
        }
    };

    const joinLobby = async (lobbyId, playerName) => {
        if (connection && lobbyId && playerName) {
            await connection.invoke("JoinLobby", lobbyId, playerName);
        }
    };


    return (
        <div>
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
        </div>
    );
}
