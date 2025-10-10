import { useEffect, useState } from "react";

/**
 * View for choosing a topic.
 */
export default function TopicChooserView({ connection }) {
    // ***********************************************
    // Local state
    // ***********************************************
    const [topicText, setTopicText] = useState("");

    // ***********************************************
    // Functions that will be called from the backend by SignalR
    // ***********************************************
    const chooseTopic = async (topicText) => {
        if (connection) {
            await connection.invoke("ChooseTopic", topicText);
        }
    };

    return (
        <div>
            TopicChooser
        </div>
    );
}
