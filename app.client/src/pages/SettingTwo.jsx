import React from "react";
import { useNavigate } from "react-router-dom";
import "./Setting.css";

const SettingTwo = () => {
    const navigate = useNavigate();

    return (
        <div className="settings-container">
            <h1 className="settings-title">Settings</h1>
            <div className="settings-options">
                <button className="settings-option">Sound: ON</button>
                <button className="settings-option">Graphics: High</button>
                <button className="settings-option">Controls: Default</button>
            </div>
            <div className="buttons">
                <button onClick={() => navigate("/")} className="back-button">Back to Menu</button>
                <button onClick={() => navigate("/game")} className="back-button">Play</button>
            </div>
        </div>
    );
};

export default SettingTwo;
