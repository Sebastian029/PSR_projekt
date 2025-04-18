"use client"

import { useState } from "react"
import { useNavigate } from "react-router-dom"

const SettingTwo = () => {
    const navigate = useNavigate()
    const [selected, setSelected] = useState("")
    const [selected2, setSelected2] = useState("")
    const [performanceTest, setPerformanceTest] = useState(false)
    const [error, setError] = useState("")

    // Styles object to keep all styling in one place
    const styles = {
        pageWrapper: {
            width: "100vw",
            height: "100vh",
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            backgroundColor: "#f0e9e2",
            fontFamily: "Arial, sans-serif",
        },
        container: {
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            padding: "30px",
            backgroundColor: "#f5f5f5",
            borderRadius: "8px",
            boxShadow: "0 4px 12px rgba(0, 0, 0, 0.15)",
            maxWidth: "500px",
            width: "90%",
        },
        title: {
            color: "#5D4037",
            marginBottom: "20px",
            fontSize: "28px",
            fontWeight: "bold",
            textAlign: "center",
        },
        errorMessage: {
            backgroundColor: "#FFEBEE",
            color: "#D32F2F",
            padding: "10px 15px",
            borderRadius: "4px",
            marginBottom: "15px",
            width: "100%",
            textAlign: "center",
            fontWeight: "500",
        },
        optionsContainer: {
            display: "flex",
            flexDirection: "column",
            gap: "20px",
            width: "100%",
            marginBottom: "25px",
        },
        optionGroup: {
            display: "flex",
            flexDirection: "column",
            gap: "8px",
            width: "100%",
        },
        optionLabel: {
            fontSize: "18px",
            fontWeight: "500",
            color: "#5D4037",
            display: "flex",
            alignItems: "center",
        },
        required: {
            color: "#D32F2F",
            marginLeft: "4px",
        },
        select: (isSelected) => ({
            padding: "12px 15px",
            borderRadius: "4px",
            border: isSelected ? "2px solid #8D6E63" : "2px solid #BDBDBD",
            backgroundColor: "#FFFFFF",
            fontSize: "16px",
            width: "100%",
            cursor: "pointer",
            outline: "none",
            color: isSelected ? "#5D4037" : "#757575",
            transition: "all 0.2s ease",
        }),
        toggleButton: (isActive) => ({
            padding: "12px 15px",
            borderRadius: "4px",
            border: "none",
            backgroundColor: isActive ? "#8D6E63" : "#EFEBE9",
            color: isActive ? "#FFFFFF" : "#5D4037",
            fontSize: "16px",
            fontWeight: "500",
            cursor: "pointer",
            width: "100%",
            textAlign: "left",
            transition: "all 0.2s ease",
            marginBottom: "10px",
        }),
        buttonsContainer: {
            display: "flex",
            justifyContent: "space-between",
            width: "100%",
            marginTop: "10px",
            gap: "15px",
        },
        button: (isDisabled) => ({
            padding: "12px 20px",
            borderRadius: "4px",
            border: "none",
            backgroundColor: isDisabled ? "#BDBDBD" : "#795548",
            color: isDisabled ? "#757575" : "#FFFFFF",
            fontSize: "16px",
            fontWeight: "bold",
            cursor: isDisabled ? "not-allowed" : "pointer",
            flex: 1,
            transition: "all 0.2s ease",
            opacity: isDisabled ? 0.7 : 1,
        }),
        backButton: {
            padding: "12px 20px",
            borderRadius: "4px",
            border: "none",
            backgroundColor: "#5D4037",
            color: "#FFFFFF",
            fontSize: "16px",
            fontWeight: "bold",
            cursor: "pointer",
            flex: 1,
            transition: "all 0.2s ease",
        },
    }

    const togglePerformanceTest = () => {
        setPerformanceTest((prev) => !prev)
    }

    const handlePlayClick = () => {
        if (!selected || !selected2) {
            setError("Please select both Depth and Granulation options")
            return
        }
        setError("")
        navigate("/game", {
            state: {
                depth: selected,
                granulation: selected2,
                isPerformanceTest: performanceTest,
                isPlayerMode: false,
            },
        })
    }

    return (
        <div style={styles.pageWrapper}>
            <div style={styles.container}>
                <h1 style={styles.title}>Settings</h1>
                {error && <div style={styles.errorMessage}>{error}</div>}

                <div style={styles.optionsContainer}>
                    <button style={styles.toggleButton(performanceTest)} onClick={togglePerformanceTest}>
                        Performance Test: {performanceTest ? "ON" : "OFF"}
                    </button>

                    <div style={styles.optionGroup}>
                        <div style={styles.optionLabel}>
                            Depth: <span style={styles.required}>*</span>
                        </div>
                        <select
                            style={styles.select(selected !== "")}
                            value={selected}
                            onChange={(e) => {
                                setSelected(e.target.value);
                                setError("");
                            }}
                        >
                            <option value="">Choose option:</option>
                            {Array.from({ length: 10 }, (_, i) => (
                                <option key={i + 1} value={i + 1}>
                                    {i + 1}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div style={styles.optionGroup}>
                        <div style={styles.optionLabel}>
                            Granulation: <span style={styles.required}>*</span>
                        </div>
                        <select
                            style={styles.select(selected2 !== "")}
                            value={selected2}
                            onChange={(e) => {
                                setSelected2(e.target.value)
                                setError("")
                            }}
                        >
                            <option value="">Choose option:</option>
                            <option value="1">1</option>
                            <option value="2">2</option>
                            <option value="3">3</option>
                        </select>
                    </div>
                </div>

                <div style={styles.buttonsContainer}>
                    <button onClick={() => navigate("/")} style={styles.backButton}>
                        Back to Menu
                    </button>
                    <button
                        onClick={handlePlayClick}
                        style={styles.button(!selected || !selected2)}
                        disabled={!selected || !selected2}
                    >
                        Play
                    </button>
                </div>
            </div>
        </div>
    )
}

export default SettingTwo
