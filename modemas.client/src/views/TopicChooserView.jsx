import "./css/TopicChooserView.css";

export default function TopicChooserView({ setGlobalError }) {
    return (
        <div className="topic-chooser-loading">
            <h2>Loading, please wait...</h2>
            <p>Your questions will be ready shortly.</p>
        </div>
    );
}
