import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import "./App.css";

function App() {
    const [connection, setConnection] = useState(null);
    const [players, setPlayers] = useState([]);
    const [lobbyId, setLobbyId] = useState(null);
    const [playerName, setPlayerName] = useState(null);

    // UI elements
    const [inputPlayerName, setInputPlayerName] = useState("");
    const [inputLobbyId, setInputLobbyId] = useState("");

    useEffect(() => {
        const connect = async () => {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/lobbyhub")
                .withAutomaticReconnect()
                .build();

            connection.on("LobbyCreated", (lobbyId) => {
                console.log("Lobby created with ID:", lobbyId);
                setLobbyId(lobbyId);
            });

            connection.on("PlayerJoined", (playerName, lobbyId) => {
                console.log("Player joined:", playerName);
                setPlayerName(playerName); // Not sure about this
                setLobbyId(lobbyId); // Or this
                setPlayers((prev) => {
                    if (prev.includes(playerName)) return prev;
                    return [...prev, playerName];
                });
            });

            connection.on("Error", (errorMsg) => {
                console.log(errorMsg);
            });

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

    const createLobby = async () => {
        if (connection) {
            await connection.invoke("CreateLobby");
        }
    };

    const joinLobby = async (lobbyId, playerName) => {
        if (connection && lobbyId) {
            await connection.invoke("JoinLobby", lobbyId, playerName);
        }
    };

    return (
        <div>
            <h1>Lobby Demo</h1>

            {!lobbyId && (
                <>
                    <button onClick={createLobby}>Create Lobby</button>
                    <input
                        type="text"
                        placeholder="Enter your name"
                        value={inputPlayerName}
                        onChange={(e) => setInputPlayerName(e.target.value)}
                    />
                    <input
                        type="text"
                        placeholder="Enter Lobby ID"
                        value={inputLobbyId}
                        onChange={(e) => setInputLobbyId(e.target.value)}
                    />
                    <button onClick={() => joinLobby(inputLobbyId, inputPlayerName)}>Join Lobby</button>
                </>
            )}

            {lobbyId && (
                <>
                    <p>Lobby ID: {lobbyId}</p>
                    {!playerName && (
                        <p>Player name: {playerName}</p>
                    )}
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
