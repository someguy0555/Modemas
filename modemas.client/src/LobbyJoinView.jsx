export default function LobbyJoinView({ onCreateLobby, onJoinLobby, inputPlayerName, setInputPlayerName, inputLobbyId, setInputLobbyId }) {
    return (
        <div>
            <button onClick={onCreateLobby}>Create Lobby</button>
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
            <button onClick={() => onJoinLobby(inputLobbyId, inputPlayerName)}>Join Lobby</button>
        </div>
    );
}
