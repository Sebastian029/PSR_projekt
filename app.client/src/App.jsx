import { BrowserRouter, Routes, Route } from "react-router-dom";


import Forecasts from './pages/Forecasts';
import Board from './pages/Board';

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/fr" element={<Forecasts />} />
                <Route path="/" element={<Board />} /> 
            </Routes>
        </BrowserRouter>
    )
}

export default App;