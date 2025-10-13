import { useState } from "react";

export default function TopicChooserView({ connection }) {
    const [topicText, setTopicText] = useState("");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [questions, setQuestions] = useState([]);

    const generateQuestions = async (topic) => {
        setLoading(true);
        setError(null);

        try {
            const response = await fetch("/api/questions/generate", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    topic,
                    numQuestions: 5,
                }),
            });

            if (!response.ok) {
                throw new Error(`Server responded with ${response.status}`);
            }

            const data = await response.json();
            setQuestions(data);

            console.log("Generated questions:", data);

            // Optional: notify backend via SignalR
            if (connection) {
                await connection.invoke("ChooseTopic", topic);
            }
        } catch (err) {
            console.error("Error generating questions:", err);
            setError(err.message);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div>
            <h2>Choose a Topic</h2>
            <div>
                <input
                    type="text"
                    value={topicText}
                    onChange={(e) => setTopicText(e.target.value)}
                    placeholder="Enter topic (e.g., World History)"
                />
                <button
                    onClick={() => generateQuestions(topicText)}
                    disabled={!topicText || loading}
                >
                    {loading ? "Generating..." : "Generate"}
                </button>
            </div>

            {error && <p style={{ color: "red" }}>Error: {error}</p>}

            {questions.length > 0 && (
                <div>
                    <h3>Generated Questions:</h3>
                    <ul>
                        {questions.map((q, i) => (
                            <li key={i}>
                                <strong>{q.text}</strong> ({q.type})
                            </li>
                        ))}
                    </ul>
                </div>
            )}
        </div>
    );
}
