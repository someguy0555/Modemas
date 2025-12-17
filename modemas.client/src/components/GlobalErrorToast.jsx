import { useEffect } from "react";
import "./GlobalErrorToast.css";

export default function GlobalErrorToast({ message, duration = 4000, onClose }) {
    useEffect(() => {
        if (!message) return;

        const timer = setTimeout(() => {
            onClose();
        }, duration);

        return () => clearTimeout(timer);
    }, [message, duration, onClose]);

    if (!message) return null;

    return (
        <div className="global-error-toast">
            {message}
        </div>
    );
}
