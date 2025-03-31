import React from "react";
import { useNavigate } from "react-router-dom";
import "./Menu.css";

const Menu = () => {
    const navigate = useNavigate();

    return (
        <div className="menu-container">
            <div className="menu-content">
                <h1 className="menu-title">Game Menu</h1>
                <div className="menu-buttons">
                    <button onClick={() => navigate("/settingSingle")} className="menu-button start">Play with Computer</button>
                    <button onClick={() => navigate("/settingTwo")} className="menu-button settings">Join as spectator</button>
                    <button onClick={() => window.location.href = "about:blank"} className="menu-button exit">Exit</button>
                </div>
            </div>
        </div>
    );
};

export default Menu;
