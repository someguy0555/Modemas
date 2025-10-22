import { useState, useEffect } from "react";

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

    const resultDisplay = answered ? (
        <div style={{ color: isCorrect ? "green" : "red" }}>
            {isCorrect ? "Correct!" : "Incorrect!"}
        </div>
    ) : null;

    return (
        <div>
            <div>Time Left: {timeLeft ?? "..."}</div>
            {resultDisplay}
            <h2>{text}</h2>

            {type === "MultipleChoice" && (
                <ul>
                    {choices?.map((choice, i) => (
                        <li key={i}>
                            <button onClick={() => answerQuestion(i)} disabled={answered}>
                                {choice}
                            </button>
                        </li>
                    ))}
                </ul>
            )}

            {type === "MultipleAnswer" && (
                <>
                    <ul>
                        {choices?.map((choice, i) => (
                            <li key={i}>
                                <button
                                    onClick={() => !answered && toggleSelection(i)}
                                    disabled={answered}
                                >
                                    {selectedIndices.includes(i) ? `[x] ${choice}` : choice}
                                </button>
                            </li>
                        ))}
                    </ul>
                    <button onClick={handleSubmit} disabled={answered || selectedIndices.length === 0}>
                        Submit Answers
                    </button>
                </>
            )}

            {type === "TrueFalse" && (
                <div>
                    <button onClick={() => answerQuestion(true)} disabled={answered}>
                        True
                    </button>
                    <button onClick={() => answerQuestion(false)} disabled={answered}>
                        False
                    </button>
                </div>
            )}
        </div>
    );
}
