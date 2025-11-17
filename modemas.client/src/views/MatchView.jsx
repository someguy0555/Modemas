import { useState, useEffect } from "react";
import "./css/MatchView.css";

export default function MatchView({ connection, lobbyId, question, answered, setAnswered, isCorrect, setIsCorrect }) {
    const [selectedIndices, setSelectedIndices] = useState([]);
    const [timeLeft, setTimeLeft] = useState(null);

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

    useEffect(() => {
        if (!question || question.timeLimit == null) return;

        // Reset state for new question
        setSelectedIndices([]);
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
    const warningThreshold = Math.max(5, Math.floor((question?.timeLimit ?? 0) * 0.2));
    const timerClass = `timer${timeLeft !== null && timeLeft <= warningThreshold ? " warning" : ""}`;

    const showResult = answered && isCorrect !== null;
    const resultDisplay = showResult ? (
        <div
            className={`result-popup ${isCorrect ? "success" : "error"}`}
            role="status"
            aria-live="polite"
        >
            {isCorrect ? "Correct" : "Incorrect"}
        </div>
    ) : null;

    return (
        <div className="match-view-container">
            {timeLeft !== null && (
                <div className={timerClass}>
                    {timeLeft}
                </div>
            )}
            {resultDisplay}
            <h2>{text}</h2>

            {type === "MultipleChoice" && (
                <ul className="choices-list">
                    {choices?.map((choice, i) => (
                        <li key={i}>
                            <button
                                className="btn choice-btn"
                                onClick={() => answerQuestion(i)}
                                disabled={answered || timeLeft === 0}
                            >
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
                                    className={`btn choice-btn${selectedIndices.includes(i) ? " selected" : ""}`}
                                    onClick={() => !answered && timeLeft !== 0 && toggleSelection(i)}
                                    disabled={answered || timeLeft === 0}
                                >
                                    {choice}
                                </button>
                            </li>
                        ))}
                    </ul>
                    <button
                        className="btn submit-btn"
                        onClick={handleSubmit}
                        disabled={answered || timeLeft === 0 || selectedIndices.length === 0}
                    >
                        Submit Answers
                    </button>
                </>
            )}

            {type === "TrueFalse" && (
                <ul className="choices-list">
                    <li>
                        <button
                            className="btn choice-btn"
                            onClick={() => answerQuestion(true)}
                            disabled={answered || timeLeft === 0}
                        >
                            True
                        </button>
                    </li>
                    <li>
                        <button
                            className="btn choice-btn"
                            onClick={() => answerQuestion(false)}
                            disabled={answered || timeLeft === 0}
                        >
                            False
                        </button>
                    </li>
                </ul>
            )}

            {question === null && (
                <div>The question is null for whatever reason</div>
            )}
        </div>
    );
}
