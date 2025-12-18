import { useEffect, useState } from "react";

/**
 * View for displaying the end of the match.
 */
export default function MatchEndView({ setGlobalError, connection, durationInSeconds, playerResults }) {
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

            {/* Podium for top 3 players */}
            <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'flex-end', margin: '2em 0' }}>
                {/* Second place */}
                <div style={{
                    order: 1,
                    background: '#C0C0C0',
                    color: '#222',
                    width: '100px',
                    height: sortedResults[1] ? '120px' : '0',
                    margin: '0 10px',
                    borderRadius: '10px 10px 0 0',
                    display: sortedResults[1] ? 'flex' : 'none',
                    flexDirection: 'column',
                    justifyContent: 'flex-end',
                    alignItems: 'center',
                    fontWeight: 'bold',
                    fontSize: '1.1em',
                    boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
                }}>
                    {sortedResults[1] && (
                        <>
                            <span style={{ marginBottom: '8px' }}>2</span>
                            <span>{sortedResults[1].name}</span>
                        </>
                    )}
                </div>
                {/* First place */}
                <div style={{
                    order: 2,
                    background: '#FFD700',
                    color: '#222',
                    width: '120px',
                    height: sortedResults[0] ? '160px' : '0',
                    margin: '0 10px',
                    borderRadius: '10px 10px 0 0',
                    display: sortedResults[0] ? 'flex' : 'none',
                    flexDirection: 'column',
                    justifyContent: 'flex-end',
                    alignItems: 'center',
                    fontWeight: 'bold',
                    fontSize: '1.3em',
                    boxShadow: '0 4px 12px rgba(0,0,0,0.15)'
                }}>
                    {sortedResults[0] && (
                        <>
                            <span style={{ marginBottom: '8px' }}>1</span>
                            <span>{sortedResults[0].name}</span>
                        </>
                    )}
                </div>
                {/* Third place */}
                <div style={{
                    order: 3,
                    background: '#CD7F32',
                    color: '#222',
                    width: '100px',
                    height: sortedResults[2] ? '100px' : '0',
                    margin: '0 10px',
                    borderRadius: '10px 10px 0 0',
                    display: sortedResults[2] ? 'flex' : 'none',
                    flexDirection: 'column',
                    justifyContent: 'flex-end',
                    alignItems: 'center',
                    fontWeight: 'bold',
                    fontSize: '1em',
                    boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
                }}>
                    {sortedResults[2] && (
                        <>
                            <span style={{ marginBottom: '8px' }}>3</span>
                            <span>{sortedResults[2].name}</span>
                        </>
                    )}
                </div>
            </div>

            {/* Leaderboard for the rest */}
            <ul>
                {sortedResults.slice(3).map((p, index) => (
                    <li key={index + 3}>
                        {index + 4}. {p.name}
                    </li>
                ))}
            </ul>
        </div>
    );
}
