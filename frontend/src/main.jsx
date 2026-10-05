/*
 * ---------------------------------------------------------------------------
 * File        : main.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Entry point of the web application. Loads Bootstrap, then wraps
 *               the application in the router and the authentication provider.
 *
 * Order       : BrowserRouter must sit outside AuthProvider, because the
 *               provider's sign in uses navigation. Bootstrap's stylesheet is
 *               imported before the project's own so that custom rules can
 *               override it rather than being overridden.
 * ---------------------------------------------------------------------------
 */

import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';

// Bootstrap 5, as the assignment requires for the web interface.
import 'bootstrap/dist/css/bootstrap.min.css';
// The JavaScript bundle powers the collapsing navigation bar and modals.
import 'bootstrap/dist/js/bootstrap.bundle.min.js';

import './index.css';
import App from './App.jsx';
import { AuthProvider } from './auth/AuthContext.jsx';
import { ToastProvider } from './components/ToastProvider.jsx';

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <ToastProvider>
          <App />
        </ToastProvider>
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>,
);
