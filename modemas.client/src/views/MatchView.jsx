import { useState } from "react";

/**
 * View for displaying the current question and choices during a match.
 */
export default function MatchView({ connection, lobbyId, question }) {
    const [answered, setAnswered] = useState(false);

    const answerQuestion = async (answer) => {
        if (connection && !answered) {
            try {
                await connection.invoke("AnswerQuestion", lobbyId, answer);
                setAnswered(true);
            } catch (err) {
                console.error(err);
            }
        }
    };

    if (!question) return <p>Waiting for question...</p>;

    const { type, text } = question;

    // Determine question type
    switch (type) {
        case "MultipleChoice":
            return (
                <div>
                    <h2>{text}</h2>
                    <ul>
                        {question.choices?.map((choice, i) => (
                            <li key={i}>
                                <button onClick={() => answerQuestion(i)} disabled={answered}>
                                    {choice}
                                </button>
                            </li>
                        ))}
                    </ul>
                </div>
            );

        case "MultipleAnswer":
            return (
                <div>
                    <h2>{text}</h2>
                    <ul>
                        {question.choices?.map((choice, i) => (
                            <li key={i}>
                                <button onClick={() => answerQuestion([i])} disabled={answered}>
                                    {choice}
                                </button>
                            </li>
                        ))}
                    </ul>
                    <p>Currently only supports selecting one answer at a time in this UI.</p>
                </div>
            );

        case "TrueFalse":
            return (
                <div>
                    <h2>{text}</h2>
                    <button onClick={() => answerQuestion(true)} disabled={answered}>True</button>
                    <button onClick={() => answerQuestion(false)} disabled={answered}>False</button>
                </div>
            );

        default:
            return <p>Unknown question type: {type}</p>;
    }
}
