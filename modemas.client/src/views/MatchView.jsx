import { useState, useEffect } from "react";

/**
 * View for displaying the current question and choices during a match.
 */
export default function MatchView({ connection, lobbyId, question, answered, setAnswered }) {
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

    // Handle countdown timer
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

        return () => clearInterval(interval);
    }, [question]);

    // Handle selection toggling for multiple answer questions
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

    // Timer and question number display
    const infoDisplay = (
        <div style={{marginBottom: "1em"}}>
            <strong>Question {questionNumber}</strong><br />
            <strong>Time left: {timeLeft}</strong>
        </div>
    );

    // Result notification
    const resultDisplay = showResult ? (
        <div style={{marginBottom: "1em", color: answerResult ? "green" : "red"}}>
            {answerResult === true ? "Correct!" : "Incorrect!"}
        </div>
    ) : null;

    // Determine question type
    switch (type) {
        case "MultipleChoice":
            return (
                <div>
                    <p>Time left: {timeLeft ?? "..."}</p>
                    <h2>{text}</h2>
                    <ul>
                        {choices?.map((choice, i) => (
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
                    <p>Time left: {timeLeft ?? "..."}</p>
                    <h2>{text}</h2>
                    <ul>
                        {choices?.map((choice, i) => (
                            <li key={i}>
                                <button
                                    onClick={() => !answered && toggleSelection(i)}
                                    disabled={answered}
                                >
                                    {selectedIndices.includes(i) ? "[x]" : "[ ]"} {choice}
                                </button>
                            </li>
                        ))}
                    </ul>
                    <button onClick={handleSubmit} disabled={answered || selectedIndices.length === 0}>
                        Submit Answers
                    </button>
                </div>
            );

        case "TrueFalse":
            return (
                <div>
                    <p>Time left: {timeLeft ?? "..."}</p>
                    <h2>{text}</h2>
                    <button onClick={() => answerQuestion(true)} disabled={answered}>
                        True
                    </button>
                    <button onClick={() => answerQuestion(false)} disabled={answered}>
                        False
                    </button>
                </div>
            );

        default:
            return <p>Unknown question type: {type}</p>;
    }
}
