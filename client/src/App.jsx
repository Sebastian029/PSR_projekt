import { BrowserRouter, Routes, Route } from "react-router-dom";


import GameBoard from './pages/Board';
import Menu from './pages/Menu'
import SettingSingle from './pages/SettingSingle'
import SettingTwo from './pages/SettingTwo'

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/game" element={<GameBoard />} />
                <Route path="/settingSingle" element={<SettingSingle />} />
                <Route path="/settingTwo" element={<SettingTwo />} />
                <Route path="/" element={<Menu />} />
            </Routes>
        </BrowserRouter>
    )
}

export default App;