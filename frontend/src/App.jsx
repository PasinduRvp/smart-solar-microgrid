/*
 * ---------------------------------------------------------------------------
 * File        : App.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : The application's route table. Every address the web
 *               application answers is listed here, together with who may open
 *               it, so the whole navigation structure can be read in one place.
 *
 * Structure   : /login sits outside the layout because it has no navigation
 *               bar. Everything else is nested inside AppLayout, which draws
 *               the frame once and renders the current page into it.
 * ---------------------------------------------------------------------------
 */

import { Route, Routes } from 'react-router-dom';
import AppLayout from './components/AppLayout';
import ProtectedRoute from './routes/ProtectedRoute';
import { ROLES } from './config';

import HomePage from './pages/HomePage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import ProfilePage from './pages/ProfilePage';
import StationsPage from './pages/StationsPage';
import StationDetailPage from './pages/StationDetailPage';
import ReservationsPage from './pages/ReservationsPage';
import ProsumersPage from './pages/ProsumersPage';
import UsersPage from './pages/UsersPage';
import NotAuthorisedPage from './pages/NotAuthorisedPage';
import NotFoundPage from './pages/NotFoundPage';

const STAFF_ROLES = [ROLES.BACKOFFICE, ROLES.GRID_OPERATOR];

export default function App() {
  return (
    <Routes>
      {/* Open to everyone. The index page explains the system and points
          each kind of user at the right place. */}
      <Route path="/" element={<HomePage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      {/* Everything below requires a signed in user. */}
      <Route
        element={
          <ProtectedRoute allowedRoles={STAFF_ROLES}>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route path="/dashboard" element={<DashboardPage />} />

        {/* Open to every role: each user edits their own account here. */}
        <Route path="/profile" element={<ProfilePage />} />

        <Route path="/stations" element={<StationsPage />} />
        <Route path="/stations/:id" element={<StationDetailPage />} />

        <Route path="/reservations" element={<ReservationsPage />} />

        {/* Two addresses, one screen. /prosumers/pending opens it already
            filtered to the activation queue, so the dashboard tile and the
            menu can link straight to the work that is waiting. */}
        <Route path="/prosumers" element={<ProsumersPage />} />
        <Route path="/prosumers/:filter" element={<ProsumersPage />} />

        {/* Staff administration is a Backoffice duty only. */}
        <Route
          path="/users"
          element={
            <ProtectedRoute allowedRoles={[ROLES.BACKOFFICE]}>
              <UsersPage />
            </ProtectedRoute>
          }
        />

        <Route path="/not-authorised" element={<NotAuthorisedPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>

    </Routes>
  );
}
