import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import "./App.css";

function App() {
    const [connection, setConnection] = useState(null);
    const [players, setPlayers] = useState([]);
    const [lobbyId, setLobbyId] = useState(null);

    // Run once on component load
    useEffect(() => {
        const connect = async () => {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/lobbyhub")
                .withAutomaticReconnect()
                .build();

            // Listen for server events
            connection.on("LobbyCreated", (id) => {
                console.log("Lobby created with ID:", id);
                setLobbyId(id);
            });

            connection.on("PlayerJoined", (playerName) => {
                console.log("Player joined:", playerName);
                setPlayers((prev) => [...prev, playerName]);
            });

            // Start connection
            try {
                await connection.start();
                console.log("Connected to SignalR hub");
                setConnection(connection);
            } catch (err) {
                console.error("Connection failed: ", err);
            }
        };

        connect();
    }, []);

    // Example: host creates lobby
    const createLobby = async () => {
        if (connection) {
            await connection.invoke("CreateLobby");
        }
    };

    // Example: player joins lobby
    const joinLobby = async (name) => {
        if (connection && lobbyId) {
            await connection.invoke("JoinLobby", lobbyId, name);
        }
    };

    return (
        <div>
            <h1>Lobby Demo</h1>

            {!lobbyId && (
                <button onClick={createLobby}>Create Lobby</button>
            )}

            {lobbyId && (
                <>
                    <p>Lobby ID: {lobbyId}</p>
                    <button onClick={() => joinLobby("Alice")}>Join as Alice</button>
                    <button onClick={() => joinLobby("Bob")}>Join as Bob</button>
                    <h2>Players:</h2>
                    <ul>
                        {players.map((p, i) => (
                            <li key={i}>{p}</li>
                        ))}
                    </ul>
                </>
            )}
        </div>
    );
}

export default App;
