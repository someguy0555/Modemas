import { useEffect, useState } from "react";

export default function LobbyWaitingView({ lobbyId, lobbyState, playerName, players, isHost, onStartMatch }) {
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

            {isHost && <button onClick={onStartMatch}>Start Match</button>}
        </div>
    );
}
