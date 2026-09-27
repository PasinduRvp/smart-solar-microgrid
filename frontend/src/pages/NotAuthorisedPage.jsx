/*
 * ---------------------------------------------------------------------------
 * File        : NotAuthorisedPage.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Shown when a signed in user reaches a page their role does not
 *               allow. Reached from ProtectedRoute.
 * ---------------------------------------------------------------------------
 */

import { Link } from 'react-router-dom';

export default function NotAuthorisedPage() {
  return (
    <div className="text-center py-5">
      <h1 className="h3 fw-semibold">Access denied</h1>
      <p className="text-body-secondary">
        Your role does not have permission to open this page.
      </p>
      <Link to="/dashboard" className="btn btn-primary mt-2">
        Back to dashboard
      </Link>
    </div>
  );
}
