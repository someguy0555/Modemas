import { useState, useEffect } from "react";
import "./css/MatchView.css";

export default function MatchView({ connection, lobbyId, question, answered, setAnswered, isCorrect, setIsCorrect }) {
    const [selectedIndices, setSelectedIndices] = useState([]);
    const [timeLeft, setTimeLeft] = useState(null);

    // Send answer to backend
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

    // Countdown timer
    useEffect(() => {
        if (!question || question.timeLimit == null) return;

        setTimeLeft(question.timeLimit);
        const interval = setInterval(() => {
            setTimeLeft((prev) => {
                if (prev === null || prev <= 1) {
                    clearInterval(interval);
                    return 0;
                }
                return prev - 1;
            });
        }, 1000);

        return () => {
            clearInterval(interval);
            setIsCorrect(null);
        };
    }, [question]);

    // MultipleAnswer selection toggling
    const toggleSelection = (i) => {
        setSelectedIndices((prev) =>
            prev.includes(i) ? prev.filter((x) => x !== i) : [...prev, i]
        );
    };

    const handleSubmit = () => {
        if (selectedIndices.length === 0) return;
        answerQuestion(selectedIndices);
    };

    if (!question) return <p>Waiting for question...</p>;

    const { type, text, choices } = question;

    // Result display
    const resultDisplay = answered ? (
        <div className="result-message" style={{ color: isCorrect ? "green" : "red" }}>
            {isCorrect ? "Correct!" : "Incorrect!"}
        </div>
    ) : null;

    // Timer style
    const timerClass = timeLeft !== null && timeLeft <= 5 ? "timer warning" : "timer";

    return (
        <div className="match-view-container">
            <div className={timerClass}>{timeLeft ?? "..."}</div>
            {resultDisplay}
            <h2>{text}</h2>

            {type === "MultipleChoice" && (
                <ul className="choices-list">
                    {choices?.map((choice, i) => (
                        <li key={i}>
                            <button className="choice-btn" onClick={() => answerQuestion(i)} disabled={answered}>
                                {choice}
                            </button>
                        </li>
                    ))}
                </ul>
            )}

            {type === "MultipleAnswer" && (
                <>
                    <ul className="choices-list">
                        {choices?.map((choice, i) => (
                            <li key={i}>
                                <button
                                    className={`choice-btn ${selectedIndices.includes(i) ? "selected" : ""}`}
                                    onClick={() => !answered && toggleSelection(i)}
                                    disabled={answered}
                                >
                                    {choice}
                                </button>
                            </li>
                        ))}
                    </ul>
                    <button
                        className="choice-btn submit-btn"
                        onClick={handleSubmit}
                        disabled={answered || selectedIndices.length === 0}
                    >
                        Submit Answers
                    </button>
                </>
            )}

            {type === "TrueFalse" && (
                <div className="tf-buttons">
                    <button className="choice-btn" onClick={() => answerQuestion(true)} disabled={answered}>
                        True
                    </button>
                    <button className="choice-btn" onClick={() => answerQuestion(false)} disabled={answered}>
                        False
                    </button>
                </div>
            )}
        </div>
    );
}
