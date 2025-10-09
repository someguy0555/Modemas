import { useState, useEffect } from "react";

/**
 * View for displaying the current question and choices during a match.
 */
export default function MatchView({ connection, lobbyId, question, answered, setAnswered }) {
    const [timeLeft, setTimeLeft] = useState(question?.timeLimit || 0);
    const [questionNumber, setQuestionNumber] = useState(0);
    const [answerResult, setAnswerResult] = useState(null);
    const [showResult, setShowResult] = useState(false);

    useEffect(() => {
        if (!connection) return;

        const handleNewQuestion = (newQuestion, timeLimit) => {
            setTimeLeft(timeLimit || newQuestion?.timeLimit || 0);
            setQuestionNumber(prev => prev + 1);
            setAnswerResult(null);
            setShowResult(false);
        };
        connection.on("NewQuestion", handleNewQuestion);
        const handleAnswerAccepted = (points, isCorrect) => {
            setAnswerResult(isCorrect);
            setShowResult(true);
            setTimeout(() => {
                setShowResult(false);
            }, 3000);
        };
        connection.on("AnswerAccepted", handleAnswerAccepted);
        return () => {
            connection.off("NewQuestion", handleNewQuestion);
            connection.off("AnswerAccepted", handleAnswerAccepted);
        };
    }, [connection]);

    useEffect(() => {
        if (timeLeft > 0) {
            const timer = setInterval(() => {
                setTimeLeft(prev => (prev > 0 ? prev - 1 : 0));
            }, 1000);
            return () => clearInterval(timer);
        }
    }, [timeLeft]);

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
                    {infoDisplay}
                    {resultDisplay}
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
                    {infoDisplay}
                    {resultDisplay}
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
                    {infoDisplay}
                    {resultDisplay}
                    <h2>{text}</h2>
                    <button onClick={() => answerQuestion(true)} disabled={answered}>True</button>
                    <button onClick={() => answerQuestion(false)} disabled={answered}>False</button>
                </div>
            );

        default:
            return <p>Unknown question type: {type}</p>;
    }
}
