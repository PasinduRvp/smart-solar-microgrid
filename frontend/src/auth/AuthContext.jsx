/*
 * ---------------------------------------------------------------------------
 * File        : AuthContext.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Holds who is signed in and makes that available to every
 *               screen, so the navigation bar, the route guards and the pages
 *               all agree about the current user.
 *
 * Why context : Passing the signed in user down through every component as a
 *               prop would thread it through screens that have no interest in
 *               it. React context puts it in one place that any component can
 *               read directly.
 *
 * Security    : The role kept here decides only what this browser DISPLAYS.
 *               It is read from the sign in response for convenience and is
 *               not a security control: the token is what the API checks, and
 *               every endpoint re-checks the role server side. Editing this
 *               value in the browser would change the menu and nothing else —
 *               the API would still refuse the request.
 * ---------------------------------------------------------------------------
 */

import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import { login as loginRequest } from '../api/authApi';
import { clearStoredAuth, readStoredAuth, writeStoredAuth } from '../api/apiClient';
import { ROLES } from '../config';

const AuthContext = createContext(null);

/**
 * Makes the signed in user available to everything inside it.
 */
export function AuthProvider({ children }) {
  // Seeded from storage so a page refresh does not sign the user out.
  const [auth, setAuth] = useState(() => readStoredAuth());

  const signIn = useCallback(async (identifier, password) => {
    const result = await loginRequest(identifier, password);

    // The API returns the token plus the details the interface needs, so no
    // second request is required to find out who signed in.
    const session = {
      token: result.token,
      expiresAtUtc: result.expiresAtUtc,
      userId: result.userId,
      nic: result.nic,
      fullName: result.fullName,
      email: result.email,
      role: result.role,
    };

    writeStoredAuth(session);
    setAuth(session);
    return session;
  }, []);

  const signOut = useCallback(() => {
    clearStoredAuth();
    setAuth(null);
  }, []);

  const value = useMemo(
    () => ({
      auth,
      isAuthenticated: Boolean(auth?.token),
      isBackoffice: auth?.role === ROLES.BACKOFFICE,
      isGridOperator: auth?.role === ROLES.GRID_OPERATOR,
      isStaff: auth?.role === ROLES.BACKOFFICE || auth?.role === ROLES.GRID_OPERATOR,
      signIn,
      signOut,
    }),
    [auth, signIn, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/**
 * Reads the authentication context.
 * Throws when used outside the provider, which turns a wiring mistake into an
 * immediate, obvious error instead of a confusing undefined further down.
 */
export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used inside an AuthProvider.');
  }

  return context;
}
