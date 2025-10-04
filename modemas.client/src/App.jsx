import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import MainMenuView from "./views/MainMenuView.jsx";
import WaitingView from "./views/WaitingView.jsx";
import TopicChooserView from "./views/TopicChooserView.jsx";
import MatchView from "./views/MatchView.jsx";
import MatchEndView from "./views/MatchEndView.jsx";
import "./App.css";


const LobbyState = {
    Idle: 0,
    Waiting: 1,
    Voting: 2,
    Started: 3,
    Closed: 4,
}

function App() {
    // Program elements
    const [connection, setConnection] = useState(null);
    const [lobbyId, setLobbyId] = useState(null);
    const [playerName, setPlayerName] = useState(null);
    const [players, setPlayers] = useState([]);
    const [lobbyState, setLobbyState] = useState(LobbyState.Idle);
    const [isHost, setIsHost] = useState(false);
    const [question, setQuestion] = useState(null);

    // Helper to connect to SignalR hub
    const connectToHub = async () => {
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl("/lobbyhub")
            .withAutomaticReconnect()
            .build();

        // ***********************************************
        // Put events received from the backend here:
        // ***********************************************
        newConnection.on("LobbyCreated", (localLobbyId) => {
            setLobbyId(localLobbyId);
            setLobbyState(LobbyState.Waiting);
            setIsHost(true);
            console.log(`Lobby ${localLobbyId} (${lobbyId}) (${lobbyState}) was created.`)
        });
        newConnection.on("LobbyJoined", (localLobbyId, localPlayerName, localPlayers, localLobbyState) => {
            setLobbyId(localLobbyId);
            setPlayerName(localPlayerName);
            setPlayers(localPlayers);
            setLobbyState(localLobbyState);
            console.log(`You joined lobby ${lobbyId}.`)
        });
        newConnection.on("LobbyAddPlayer", (playerName) => {
            setPlayers((prev) => {
                if (prev.includes(playerName)) return prev;
                return [...prev, playerName];
            });
            console.log(`Added player ${playerName} to lobby ${lobbyId}.`);
        });
        newConnection.on("VotingStarted", (localLobbyId) => {
            // if (lobbyId == localLobbyId) {
                setLobbyState(LobbyState.Voting);
                console.log(`Voting started in lobby ${lobbyId}.`);
            // } else console.log(`VotingStarted: Incorrect lobbyId ${localLobbyId} sent to lobby ${lobbyId}`);
        });
        newConnection.on("VotingEnded", (localLobbyId) => {
            // if (lobbyId == localLobbyId) {
                setLobbyState(LobbyState.Started);
                // connection.invoke("StartMatch", localLobbyId);
                console.log(`Match started in lobby ${localLobbyId}.`);
            // } else console.log(`VotingEnded: Incorrect lobbyId ${localLobbyId} sent to lobby ${lobbyId}`);
        });
        newConnection.on("LobbyMatchStarted", (localLobbyId) => {
            // if (lobbyId == localLobbyId) {
                setLobbyState(LobbyState.Started);
                console.log(`Match started in lobby ${lobbyId}.`);
            // } else console.log(`LobbyMatchStarted: Incorrect lobbyId ${localLobbyId} sent to lobby ${lobbyId}`);
        });
        newConnection.on("KickedFromLobby", async (message) => {
            await newConnection.stop();
            console.log("You were kicked out of the room: " + message);
            setLobbyId(null);
            setPlayerName(null);
            setPlayers([]);
            setLobbyState(null);
            setIsHost(false);
            await connectToHub();
        });
        newConnection.on("NewQuestion", (question) => {
            setQuestion(question);
            setTimeLeft(question.timeLimit);
            console.log(`Next question in lobby ${lobbyId}.`);
        });
        newConnection.on("QuestionTimeout", (QuestionTimeoutMessage) => {
            console.log(`Timeout in lobby ${lobbyId}: ${QuestionTimeoutMessage}`);
        });
        newConnection.on("MatchEndStarted", (localLobbyId, duration) => {
            // if (lobbyId == localLobbyId) {
                setLobbyState(LobbyState.Closed);
                setQuestion(null);
                console.log("Match ended in lobby ${lobbyId}!");
            // } else console.log(`MatchEndStarted: Incorrect lobbyId ${localLobbyId} sent to lobby ${lobbyId}`);
        });
        newConnection.on("MatchEndEnded", (localLobbyId) => {
            // if (lobbyId == localLobbyId) {
                setLobbyState(LobbyState.Waiting);
                console.log("Returning to lobby ${lobbyId}.");
            // } else console.log(`MatchEndEnded: Incorrect lobbyId ${localLobbyId} sent to lobby ${lobbyId}`);
        });
        newConnection.on("Error", (errorMsg) => {
            console.log(errorMsg);
        });

        try {
            await newConnection.start();
            setConnection(newConnection);
            console.log("Connection state:", newConnection.state);
        } catch (err) {
            console.error("Connection failed: ", err);
        }
    };

    useEffect(() => {
        connectToHub();
    }, []);

    // Picks which view to render
    let view;
    switch (lobbyState) {
        case LobbyState.Idle:
            view = (
                <MainMenuView
                    connection={connection}
                    setGlobalPlayerName={setPlayerName}
                    setGlobalLobbyId={setLobbyId}
                />
            );
            break;
        case LobbyState.Waiting:
            view = (
                <WaitingView
                    connection={connection}
                    lobbyId={lobbyId}
                    lobbyState={lobbyState}
                    playerName={playerName}
                    players={players}
                    isHost={isHost}
                />
            );
            break;
        case LobbyState.Voting:
            view = (
                <TopicChooserView
                    connection={connection}
                />
            );
            break;
        case LobbyState.Started:
            view = (
                <MatchView
                    connection={connection}
                    lobbyId={lobbyId}
                    question={question}
                />
            );
            break;
        case LobbyState.Closed:
            view = (
                <MatchEndView
                    connection={connection}
                />
            );
            break;
        default:
            view = (
                <div>
                    ErrorView
                </div>
            );
            break;
    }

    return (
        <div className="App">
            {view}
        </div>
    );
}

export default App;
