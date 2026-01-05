import './App.css'

import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import Home from './pages/Home'
import ProductHistory from './pages/ProductHistory'

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/product/:id" element={<ProductHistory />} />
      </Routes>
    </Router>
  )
}

export default App