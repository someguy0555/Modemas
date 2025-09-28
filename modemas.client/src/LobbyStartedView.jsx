/**
 * View for displaying the current question and choices during a match.
 */
export default function LobbyStartedView({ connection, lobbyId, question }) {
    const answerQuestion = async (anwserIndex) => {
        if (connection && anwserIndex != null) {
            await connection.invoke("AnswerQuestion", lobbyId, anwserIndex);
        }
    };

    if (!question) {
        return <p>Waiting for question...</p>
    }
    console.log(question)

    return (
        <div>
            <h2>{question.text}</h2>
            <ul>
                {question.choices?.map((choice, i) => (
                    <li key={i}>
                        <button onClick={
                            () => {
                                answerQuestion(i);
                            }
                        }>
                            {choice}
                        </button>
                    </li>
                ))}
            </ul>
        </div>
    );
}
