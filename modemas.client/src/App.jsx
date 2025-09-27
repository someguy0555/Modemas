import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import LobbyJoinView from "./LobbyJoinView";
import LobbyWaitingView from "./LobbyWaitingView";
import "./App.css";


const LobbyState = {
    Waiting: 0,
    Started: 1,
     Closed: 2,
}

function App() {
    // Program elements
    const [connection, setConnection] = useState(null);
    const [lobbyId, setLobbyId] = useState(null);
    const [playerName, setPlayerName] = useState(null);
    const [players, setPlayers] = useState([]);
    const [lobbyState, setLobbyState] = useState(null);
    const [isHost, setIsHost] = useState(false);

    // UI elements
    const [inputPlayerName, setInputPlayerName] = useState("");
    const [inputLobbyId, setInputLobbyId] = useState("");

    // Helper to connect to SignalR hub
    const connectToHub = async () => {
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl("/lobbyhub")
            .withAutomaticReconnect()
            .build();

        newConnection.on("LobbyCreated", (lobbyId, lobbyState) => {
            setLobbyId(lobbyId);
            setLobbyState(lobbyState);
            setIsHost(true);
            console.log(lobbyState)
        });
        newConnection.on("LobbyJoined", (lobbyId, playerName, players, lobbyState) => {
            setLobbyId(lobbyId);
            setPlayerName(playerName);
            setPlayers(players);
            setLobbyState(lobbyState);
            setIsHost(false);
            console.log(lobbyState)
        });
        newConnection.on("LobbyAddPlayer", (playerName) => {
            setPlayers((prev) => {
                if (prev.includes(playerName)) return prev;
                return [...prev, playerName];
            });
        });
        newConnection.on("LobbyMatchStarted", (lobbyState) => {
            setLobbyState(lobbyState);
        });
        newConnection.on("Error", (errorMsg) => {
            console.log(errorMsg);
        });
        newConnection.on("KickedFromLobby", async (message) => {
            await newConnection.stop();
            alert("You were kicked out of the room: " + message);
            setLobbyId(null);
            setPlayerName(null);
            setPlayers([]);
            setLobbyState(null);
            setIsHost(false);
            await connectToHub();
        });
        try {
            await newConnection.start();
            setConnection(newConnection);
        } catch (err) {
            console.error("Connection failed: ", err);
        }
    };

    useEffect(() => {
        connectToHub();
    }, []);

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

    const startMatch = async (lobbyId) => {
        if (connection) {
            await connection.invoke("StartMatch", lobbyId);
        }
    };

    // Picks which view to render
    let view;
    if (!lobbyId) {
        view = (
            <LobbyJoinView
                onCreateLobby={createLobby}
                onJoinLobby={joinLobby}
                inputPlayerName={inputPlayerName}
                setInputPlayerName={setInputPlayerName}
                inputLobbyId={inputLobbyId}
                setInputLobbyId={setInputLobbyId}
            />
        );
    } else if (lobbyState === LobbyState.Waiting || lobbyState === null) {
        view = (
            <LobbyWaitingView
                lobbyId={lobbyId}
                lobbyState={lobbyState}
                playerName={playerName}
                players={players}
                isHost={isHost}
                onStartMatch={() => startMatch(lobbyId)}
            />
        );
    } else if (lobbyState === LobbyState.Started) {
        view = <div>Game Started! (Placeholder for future GameView)</div>;
    } else if () {
    }

    return (
        <div className="App">
            <h1>Lobby Demo</h1>
            {view}
        </div>
    );
}

export default App;
