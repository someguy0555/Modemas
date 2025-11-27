import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import MainMenuView from "./views/MainMenuView.jsx";
import WaitingView from "./views/WaitingView.jsx";
import TopicChooserView from "./views/TopicChooserView.jsx";
import MatchView from "./views/MatchView.jsx";
import MatchEndView from "./views/MatchEndView.jsx";
import "./App.css";

function App() {
    // Program elements
    const [connection, setConnection] = useState(null);
    const [lobbyId, setLobbyId] = useState(null);
    const [playerName, setPlayerName] = useState(null);
    const [players, setPlayers] = useState([]);
    const [lobbyState, setLobbyState] = useState("Idle");
    const [isHost, setIsHost] = useState(false);
    const [matchEndDurationInSeconds, setMatchEndDurationInSeconds] = useState(null);

    // MatchView
    const [question, setQuestion] = useState(null);
    const [answered, setAnswered] = useState(false);
    const [isCorrect, setIsCorrect] = useState(null);
    const [points, setPoints] = useState(null);

    // MatchEndView
    const [playerResults, setPlayerResults] = useState(null);

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
            setLobbyState("Waiting");
            setIsHost(true);
            console.log(`LobbyCreated: localLobbyId = ${localLobbyId}`)
        });
        newConnection.on("LobbyJoined", (localLobbyId, localPlayerName, localPlayers, localLobbyState) => {
            console.log(`LobbyJoined: localLobbyId = ${localLobbyId}, localPlayerName = ${localPlayerName}, localPlayers = ${localPlayers}, localLobbyState = ${localLobbyState}`)
            setLobbyId(localLobbyId);
            setPlayerName(localPlayerName);
            setPlayers(localPlayers);
            setLobbyState(localLobbyState);
        });
        newConnection.on("LobbyAddPlayer", (playerName) => {
            setPlayers((prev) => {
                if (prev.includes(playerName)) return prev;
                return [...prev, playerName];
            });
            console.log(`LobbyAddPlayer to lobby ${lobbyId}: playerName = ${playerName}`);
        });
        newConnection.on("LobbyRemovePlayer", (removedPlayerName) => {
            setPlayers((prev) => prev.filter(p => p !== removedPlayerName));
            console.log(`LobbyRemovePlayer from lobby ${lobbyId}: removedPlayerName = ${removedPlayerName}`);
        });
        newConnection.on("VotingStarted", (localLobbyId) => {
            console.log(`VotingStarted: localLobbyId = ${localLobbyId}`);
            setLobbyState("Voting");
        });
        newConnection.on("VotingEnded", (localLobbyId) => {
            console.log(`VotingEnded: localLobbyId = ${localLobbyId}`);
            setLobbyState("Started");
            console.log(`Match started in lobby ${localLobbyId}.`);
        });
        newConnection.on("LobbyMatchStarted", (localLobbyId) => {
            console.log(`LobbyMatchStarted: localLobbyId = ${localLobbyId}`);
            setLobbyState("Started");
            console.log(`Match started in lobby ${lobbyId}.`);
        });
        newConnection.on("KickedFromLobby", async (message) => {
            await newConnection.stop();
            console.log("You were kicked out of the room: " + message);
            setLobbyId(null);
            setPlayerName(null);
            setPlayers([]);
            setLobbyState("Idle");
            setIsHost(false);
            await connectToHub();
        });
        newConnection.on("NewQuestion", (question) => {
            console.log("NewQuestion: question = ", question);
            setQuestion(question);
            setAnswered(false);
            setPoints(null);
        });
        newConnection.on("AnswerAccepted", (entry) => {
            console.log("AnswerAccepted: entry = ", entry);
            setIsCorrect(entry.isCorrect);
            setPoints(entry.points);
        });
        newConnection.on("QuestionTimeout", (QuestionTimeoutMessage) => {
            console.log(`QuestionTimeout in lobby ${lobbyId}: message = ${QuestionTimeoutMessage}`);
        });
        newConnection.on("MatchEndStarted", (localLobbyId, durationInSeconds, localPlayerResults) => {
            console.log(`MatchEndStarted: localLobbyId = ${localLobbyId}, duration = ${durationInSeconds}, results = ${localPlayerResults}`);
            setMatchEndDurationInSeconds(durationInSeconds);
            setPlayerResults(localPlayerResults)
            setLobbyState("Closed");
            setQuestion(null);
        });
        newConnection.on("MatchEndEnded", (localLobbyId) => {
            console.log(`MatchEndEnded: localLobbyId = ${localLobbyId}`);
            setLobbyState("Waiting");
            console.log(`Returning to lobby ${lobbyId}.`);
            setPlayerResults(null);
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
        case "Idle":
            view = (
                <MainMenuView
                    connection={connection}
                    setGlobalPlayerName={setPlayerName}
                    setGlobalLobbyId={setLobbyId}
                />
            );
            break;
        case "Waiting":
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
        case "Voting":
            view = (
                <TopicChooserView
                    connection={connection}
                />
            );
            break;
        case "Started":
            view = (
                <MatchView
                    connection={connection}
                    lobbyId={lobbyId}
                    question={question}
                    answered={answered}
                    setAnswered={setAnswered}
                    isCorrect={isCorrect}
                    setIsCorrect={setIsCorrect}
                />
            );
            break;
        case "Closed":
            view = (
                <MatchEndView
                    connection={connection}
                    durationInSeconds={matchEndDurationInSeconds}
                    playerResults={playerResults}
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
