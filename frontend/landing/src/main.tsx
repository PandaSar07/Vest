import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

const mountNode = document.getElementById('landing-root') ?? document.getElementById('root')

if (mountNode) {
  createRoot(mountNode).render(
    <StrictMode>
      <App />
    </StrictMode>,
  )
}
