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

    return (
        <div>
            <button onClick={
                () => {
                    setGlobalPlayerName(playerName);
                    console.log(`${playerName}.`);
                    createLobby(playerName)
                }
            }>Create Lobby</button>
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
            <button onClick={
                () => {
                    setGlobalPlayerName(playerName);
                    setGlobalLobbyId(lobbyId);
                    console.log(`${playerName} and ${lobbyId}.`);
                    joinLobby(lobbyId, playerName)
                }
            }>Join Lobby</button>
        </div>
    );
}
