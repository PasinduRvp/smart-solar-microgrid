/*
 * ---------------------------------------------------------------------------
 * File        : ProtectedRoute.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Wraps a route so it can only be opened by a signed in user,
 *               and optionally only by particular roles.
 *
 * Security    : This is a CONVENIENCE, not a security boundary. It stops a
 *               Grid Operator being shown a page full of failed requests, and
 *               it keeps the interface honest about what each role can do.
 *               It is not what keeps data safe. Anyone can edit the role held
 *               in their own browser, and the page would then render — but
 *               every request it made would still be refused by the API with a
 *               403, because the role the API trusts comes from the signed
 *               token, not from this code.
 *
 *               That distinction is worth being clear about: the guard below
 *               shapes the interface, and the [Authorize] attributes in the
 *               controllers enforce the rule.
 * ---------------------------------------------------------------------------
 */

import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

/**
 * @param {object}   props
 * @param {string[]} [props.allowedRoles] Roles permitted here. Omit to allow any signed in user.
 * @param {React.ReactNode} props.children The screen to show when permitted.
 */
export default function ProtectedRoute({ allowedRoles, children }) {
  const { auth, isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    // Remember where they were heading so sign in can return them there.
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (allowedRoles && !allowedRoles.includes(auth.role)) {
    return <Navigate to="/not-authorised" replace />;
  }

  return children;
}
