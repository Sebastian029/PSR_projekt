import { BrowserRouter, Routes, Route } from "react-router-dom";


import Forecasts from './pages/Forecasts';
import GameBoard from './pages/Board';

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/fr" element={<Forecasts />} />
                <Route path="/" element={<GameBoard />} /> 
            </Routes>
        </BrowserRouter>
    )
}

export default App;