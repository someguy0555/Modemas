import { useEffect, useState } from "react";

/**
 * View for displaying the end of the match.
 */
export default function MatchEndView({ connection, durationInSeconds, playerResults }) {
    // ***********************************************
    // Local state
    // ***********************************************
    const [timeLeft, setTimeLeft] = useState(durationInSeconds || 0);
    const [sortedResults, setSortedResults] = useState([]);

    // ***********************************************
    // Effects
    // ***********************************************

    useEffect(() => {
        if (timeLeft <= 0) return;
        const timer = setInterval(() => {
            setTimeLeft(prev => (prev > 0 ? prev - 1 : 0));
        }, 1000);
        return () => clearInterval(timer);
    }, [timeLeft]);

    useEffect(() => {
        if (playerResults && Array.isArray(playerResults)) {
            const sorted = [...playerResults].sort((a, b) => b.points - a.points);
            setSortedResults(sorted);
        }
    }, [playerResults]);

    return (
        <div>
            <h2>Match Results</h2>
            <p>Returning to lobby in {timeLeft} seconds...</p>

            <ul>
                {sortedResults.map((p, index) => (
                    <li key={index}>
                        {index + 1}. {p.name} — {p.points} pts
                    </li>
                ))}
            </ul>
        </div>
    );
}
