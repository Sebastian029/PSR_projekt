import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import "./Setting.css";

const SettingSingle = () => {
    const navigate = useNavigate();
    const [selected, setSelected] = useState("");

    return (
        <div className="settings-container">
            <h1 className="settings-title">Settings</h1>
            <div className="settings-options">
                <button className="settings-option">Sound: ON</button>
                <div>
                    Liczba ...
                </div>
                <select className="settings-option" value={selected} onChange={(e) => setSelected(e.target.value)}>
                    <option value="">Wybierz opcjê</option>
                    <option value="1">Opcja 1</option>
                    <option value="2">Opcja 2</option>
                    <option value="3">Opcja 3</option>
                </select>
                <button className="settings-option">Controls: Default</button>
            </div>
            <div className="buttons">
                <button onClick={() => navigate("/")} className="back-button">Back to Menu</button>
                <button onClick={() => navigate("/game")} className="back-button">Play</button>
            </div>
        </div>
    );
};

export default SettingSingle;
