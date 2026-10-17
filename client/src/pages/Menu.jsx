"use client"

import { useNavigate } from "react-router-dom"

const Menu = () => {
    const navigate = useNavigate()

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
            padding: "40px",
            backgroundColor: "#f5f5f5",
            borderRadius: "8px",
            boxShadow: "0 6px 16px rgba(0, 0, 0, 0.15)",
            maxWidth: "500px",
            width: "90%",
        },
        title: {
            color: "#5D4037",
            marginBottom: "30px",
            fontSize: "32px",
            fontWeight: "bold",
            textAlign: "center",
            textShadow: "0 1px 2px rgba(0,0,0,0.1)",
        },
        buttonsContainer: {
            display: "flex",
            flexDirection: "column",
            gap: "16px",
            width: "100%",
        },
        button: (variant) => {
            const baseStyle = {
                padding: "16px 20px",
                borderRadius: "6px",
                border: "none",
                fontSize: "18px",
                fontWeight: "bold",
                cursor: "pointer",
                width: "100%",
                textAlign: "center",
                transition: "all 0.2s ease",
                boxShadow: "0 2px 4px rgba(0,0,0,0.15)",
            }

            if (variant === "start") {
                return {
                    ...baseStyle,
                    backgroundColor: "#8D6E63",
                    color: "#FFFFFF",
                    ":hover": {
                        backgroundColor: "#795548",
                    },
                }
            } else if (variant === "settings") {
                return {
                    ...baseStyle,
                    backgroundColor: "#A1887F",
                    color: "#FFFFFF",
                    ":hover": {
                        backgroundColor: "#8D6E63",
                    },
                }
            } else if (variant === "exit") {
                return {
                    ...baseStyle,
                    backgroundColor: "#EFEBE9",
                    color: "#5D4037",
                    ":hover": {
                        backgroundColor: "#D7CCC8",
                    },
                }
            }

            return baseStyle
        },
        logo: {
            width: "120px",
            height: "120px",
            marginBottom: "20px",
            backgroundColor: "#8D6E63",
            borderRadius: "50%",
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            boxShadow: "0 4px 8px rgba(0,0,0,0.2)",
        },
        logoInner: {
            width: "100px",
            height: "100px",
            backgroundColor: "#D7CCC8",
            borderRadius: "50%",
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            position: "relative",
        },
        logoPiece1: {
            position: "absolute",
            width: "40px",
            height: "40px",
            backgroundColor: "#3E2723",
            borderRadius: "50%",
            border: "2px solid #1A120B",
            top: "20px",
            left: "20px",
        },
        logoPiece2: {
            position: "absolute",
            width: "40px",
            height: "40px",
            backgroundColor: "#FFFDE7",
            borderRadius: "50%",
            border: "2px solid #BFA094",
            bottom: "20px",
            right: "20px",
        },
    }

    return (
        <div style={styles.pageWrapper}>
            <div style={styles.container}>
                {/* Checkers Logo */}
                <div style={styles.logo}>
                    <div style={styles.logoInner}>
                        <div style={styles.logoPiece1}></div>
                        <div style={styles.logoPiece2}></div>
                    </div>
                </div>

                <h1 style={styles.title}>Checkers Game</h1>

                <div style={styles.buttonsContainer}>
                    <button
                        onClick={() => navigate("/settingSingle")}
                        style={styles.button("start")}
                        onMouseOver={(e) => {
                            e.currentTarget.style.backgroundColor = "#795548"
                            e.currentTarget.style.boxShadow = "0 4px 8px rgba(0,0,0,0.2)"
                        }}
                        onMouseOut={(e) => {
                            e.currentTarget.style.backgroundColor = "#8D6E63"
                            e.currentTarget.style.boxShadow = "0 2px 4px rgba(0,0,0,0.15)"
                        }}
                    >
                        Play with Computer
                    </button>

                    <button
                        onClick={() => navigate("/settingTwo")}
                        style={styles.button("settings")}
                        onMouseOver={(e) => {
                            e.currentTarget.style.backgroundColor = "#8D6E63"
                            e.currentTarget.style.boxShadow = "0 4px 8px rgba(0,0,0,0.2)"
                        }}
                        onMouseOut={(e) => {
                            e.currentTarget.style.backgroundColor = "#A1887F"
                            e.currentTarget.style.boxShadow = "0 2px 4px rgba(0,0,0,0.15)"
                        }}
                    >
                        Join as Spectator
                    </button>
                </div>
            </div>
        </div>
    )
}

export default Menu
