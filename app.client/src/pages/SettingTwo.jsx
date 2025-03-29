import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import "./Setting.css";

const SettingTwo = () => {
    const navigate = useNavigate();
    const [selected, setSelected] = useState("");
    const [selected2, setSelected2] = useState("");
    const [performanceTest, setPerformanceTest] = useState(false);
    const [error, setError] = useState("");

    const togglePerformanceTest = () => {
        setPerformanceTest(prev => !prev);
    };

    const handlePlayClick = () => {
        if (!selected || !selected2) {
            setError("Please select both Depth and Granulation options");
            return;
        }
        setError("");
        navigate("/game", {
            state: {
                depth: selected,
                granulation: selected2,
                isPerformanceTest: performanceTest
            }
        });
    };

    return (
        <div className="settings-container">
            <h1 className="settings-title">Settings</h1>
            {error && <div className="error-message">{error}</div>}
            <div className="settings-options">
                <button
                    className={`settings-option ${performanceTest ? 'active' : ''}`}
                    onClick={togglePerformanceTest}
                >
                    Performance Test: {performanceTest ? "ON" : "OFF"}
                </button>
                <div className="buttons2">
                    <div style={{ fontSize: '20px' }} >
                        Depth: <span className="required">*</span>
                    </div>
                    <select
                        className={`settings-option ${!selected ? 'unselected' : ''}`}
                        value={selected}
                        onChange={(e) => setSelected(e.target.value)}
                        required
                    >
                        <option value="">Choose option:</option>
                        <option value="1">1</option>
                        <option value="2">2</option>
                        <option value="3">3</option>
                    </select>
                </div>
                <div className="buttons2">
                    <div style={{ fontSize: '20px' }} >
                        Granulation: <span className="required">*</span>
                    </div>
                    <select
                        className={`settings-option ${!selected2 ? 'unselected' : ''}`}
                        value={selected2}
                        onChange={(e) => setSelected2(e.target.value)}
                        required
                    >
                        <option value="">Choose option:</option>
                        <option value="1">1</option>
                        <option value="2">2</option>
                        <option value="3">3</option>
                    </select>
                </div>
            </div>
            <div className="buttons">
                <button onClick={() => navigate("/")} className="back-button">Back to Menu</button>
                <button
                    onClick={handlePlayClick}
                    className={`back-button ${!selected || !selected2 ? 'disabled' : ''}`}
                    disabled={!selected || !selected2}
                >
                    Play
                </button>
            </div>
        </div>
    );
};

export default SettingTwo;