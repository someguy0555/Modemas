import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import "./App.css";

function App() {
    // Program elements
    const [connection, setConnection] = useState(null);
    const [lobbyId, setLobbyId] = useState(null);
    const [playerName, setPlayerName] = useState(null);
    const [players, setPlayers] = useState([]); // Needs to be replaced this with a lobby object.
    const [lobbyState, setLobbyState] = useState(null);

    // UI elements
    const [inputPlayerName, setInputPlayerName] = useState("");
    const [inputLobbyId, setInputLobbyId] = useState("");

    useEffect(() => {
        const connect = async () => {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/lobbyhub")
                .withAutomaticReconnect()
                .build();

            // 'connection.on(...)' basically waits for the 'event' with the name of the first argument's string,
            // and upon receiving it, executes the function (the second argument).
            connection.on("LobbyCreated", (lobbyId, lobbyState) => {
                console.log("Lobby created with ID:", lobbyId);
                setLobbyId(lobbyId);
                setLobbyState(lobbyState);
            });

            connection.on("LobbyJoined", (lobbyId, playerName, players, lobbyState) => {
                setLobbyId(lobbyId);
                setPlayerName(playerName);
                setPlayers(players);
                setLobbyState(lobbyState);
                console.log("Player joined:", playerName);
            });

            connection.on("LobbyAddPlayer", (playerName) => {
                setPlayers((prev) => {
                    if (prev.includes(playerName)) return prev;
                    return [...prev, playerName];
                });
                console.add("Added player ", playerName, " to lobby")
            });

            connection.on("LobbyMatchStarted", (lobbyState) => {
                setLobbyState(lobbyState);
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

    const startMatch = async (lobbyId) => {
        if (connection) {
            await connection.invoke("StartMatch", lobbyId);
        }
    };

    // This UI is sort of temporary, currently work in progress.
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
                    <p>Lobby State: {lobbyState}</p>
                    {playerName && (
                        <p>Player name: {playerName}</p>
                    )}
                    <h2>Players:</h2>
                    <ul>
                        {players.map((p, i) => (
                            <li key={i}>{p}</li>
                        ))}
                    </ul>
                    <button onClick={() => startMatch(lobbyId)}>Start Match</button>
                </>
            )}
        </div>
    );
}

export default App;
