import { useEffect, useState } from "react";

export default function LobbyStartedView({ lobbyId, question }) {
    console.log(question)
    if (!question) {
        return <p>Waiting for question...</p>
    }
    return (
        <div>
            <h2>{question.text}</h2>
            <ul>
                {question.choices?.map((choice, i) => (
                    <li key={i}>{choice}</li>
                ))}
            </ul>
        </div>
    );
}
