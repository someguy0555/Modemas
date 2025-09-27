import { useEffect, useState } from "react";

export default function LobbyJoinView({ onCreateLobby, onJoinLobby }) {
    const [playerName, setPlayerName] = useState("");
    const [lobbyId, setLobbyId] = useState("");

    return (
        <div>
            <button onClick={() => onCreateLobby()}>Create Lobby</button>
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
            <button onClick={() => onJoinLobby(lobbyId, playerName)}>Join Lobby</button>
        </div>
    );
}
