/*
 * ---------------------------------------------------------------------------
 * File        : apiClient.js
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : The single Axios instance every part of this application uses
 *               to talk to the web service. Nothing else in the client calls
 *               the network directly.
 *
 * Why one instance
 *               Two interceptors are configured here once and then apply to
 *               every request in the application:
 *                 - the request interceptor attaches the bearer token, so no
 *                   screen has to remember to send it;
 *                 - the response interceptor turns the API's error shape into
 *                   a readable message, so no screen has to unpick it.
 *               Scattering axios.get calls through the components would mean
 *               repeating both, and the copy somebody forgot is exactly where
 *               a screen would show a blank error or an unauthenticated call.
 *
 * Client role : This file is the whole extent of the web application's
 *               "logic". It moves requests and responses. Every rule about
 *               what is allowed lives in the API, which is what the assignment
 *               means by the FAT service pattern.
 * ---------------------------------------------------------------------------
 */

import axios from 'axios';
import { API_BASE_URL, AUTH_STORAGE_KEY } from '../config';

/** Reads the stored session, or null when nobody is signed in. */
export function readStoredAuth() {
  try {
    const raw = localStorage.getItem(AUTH_STORAGE_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    // Corrupt or unreadable storage should sign the user out, not crash the app.
    return null;
  }
}

/** Saves the session so a page refresh does not sign the user out. */
export function writeStoredAuth(auth) {
  localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(auth));
}

/** Clears the stored session. */
export function clearStoredAuth() {
  localStorage.removeItem(AUTH_STORAGE_KEY);
}

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 20000,
});

// --- Request: attach the token ----------------------------------------------
apiClient.interceptors.request.use((config) => {
  const auth = readStoredAuth();

  if (auth?.token) {
    config.headers.Authorization = `Bearer ${auth.token}`;
  }

  return config;
});

// --- Response: turn API errors into readable messages -----------------------
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    // A token that has expired or been tampered with. Clear the stale session
    // and send the user back to sign in rather than leaving them on a page
    // where every request will fail.
    if (error.response?.status === 401) {
      clearStoredAuth();

      if (!window.location.pathname.startsWith('/login')) {
        window.location.assign('/login?expired=1');
      }
    }

    return Promise.reject(new Error(extractMessage(error)));
  },
);

/**
 * Pulls the most useful message out of an API failure.
 *
 * The service answers with RFC 7807 problem details, so a refused business
 * rule arrives as { title, detail }. The detail is written for the end user —
 * "Reservations can only be cancelled at least 12 hours beforehand" — so it is
 * shown as-is. This is why the web application can explain the rule without
 * containing it.
 */
function extractMessage(error) {
  const data = error.response?.data;

  if (data?.detail) {
    return data.detail;
  }

  // Model validation failures arrive as { errors: { Field: [messages] } }.
  if (data?.errors) {
    const messages = Object.values(data.errors).flat();
    if (messages.length > 0) {
      return messages.join(' ');
    }
  }

  if (data?.title) {
    return data.title;
  }

  // No response at all: the service is unreachable rather than refusing.
  if (error.request && !error.response) {
    return 'Cannot reach the server. Check that the API is running and try again.';
  }

  return error.message || 'Something went wrong. Please try again.';
}

export default apiClient;
