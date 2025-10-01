import React, { useState, useEffect } from 'react';

/**
 * View for displaying the current question and choices during a match.
 */
export default function LobbyStartedView({ connection, lobbyId, question }) {
    const [error, setError] = useState('');
    const [answerFeedback, setAnswerFeedback] = useState('');
    const [answered, setAnswered] = useState(false);

    useEffect(() => {
        if (!connection) return;
        const handleAnswerReceived = (questionIndex, answerIndex) => {
            setAnswerFeedback(`Your answer for question ${questionIndex + 1} was received!`);
            setAnswered(true);
        };
        const handlePlayerAnswered = (playerName, questionIndex, answerIndex) => {
            console.log("PlayerAnswered", playerName, questionIndex, answerIndex);
        };
        const handleError = (msg) => {
            setError(msg);
            console.log("Error", msg);
        };
        connection.on("AnswerReceived", handleAnswerReceived);
        connection.on("PlayerAnswered", handlePlayerAnswered);
        connection.on("Error", handleError);
        connection.on("NewQuestion", () => {
            setAnswered(false);
            setAnswerFeedback('');
            setError('');
        });
        return () => {
            connection.off("AnswerReceived", handleAnswerReceived);
            connection.off("PlayerAnswered", handlePlayerAnswered);
            connection.off("Error", handleError);
            connection.off("NewQuestion", () => {
                setAnswered(false);
                setAnswerFeedback('');
                setError('');
            });
        };
    }, [connection]);

    const answerQuestion = async (anwserIndex) => {
        setError('');
        console.log("Attempting to answer", anwserIndex);
        if (connection && anwserIndex != null && !answered) {
            try {
                await connection.invoke("AnswerQuestion", lobbyId, anwserIndex);
            } catch (e) {
                setError("Failed to submit answer.");
                console.log("Failed to submit answer.", e);
            }
        }
    };

    if (!question) {
        return <p>Waiting for question...</p>
    }
    console.log("Rendering question", question)

    return (
        <div>
            <h2>{question.text}</h2>
            <ul>
                {question.choices?.map((choice, i) => (
                    <li key={i}>
                        <button
                            onClick={() => answerQuestion(i)}
                            disabled={answered}
                        >
                            {choice}
                        </button>
                    </li>
                ))}
            </ul>
            {answerFeedback && <p style={{ color: 'green' }}>{answerFeedback}</p>}
            {error && <p style={{ color: 'red' }}>{error}</p>}
        </div>
    );
}
