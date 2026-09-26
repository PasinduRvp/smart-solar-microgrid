/*
 * ---------------------------------------------------------------------------
 * File        : NotFoundPage.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Shown for any address that does not match a known page.
 * ---------------------------------------------------------------------------
 */

import { Link } from 'react-router-dom';

export default function NotFoundPage() {
  return (
    <div className="text-center py-5">
      <h1 className="display-5 fw-semibold">404</h1>
      <p className="text-body-secondary">That page does not exist.</p>
      <Link to="/dashboard" className="btn btn-primary mt-2">
        Back to dashboard
      </Link>
    </div>
  );
}
