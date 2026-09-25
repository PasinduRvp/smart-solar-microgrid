/*
 * ---------------------------------------------------------------------------
 * File        : config.js
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Settings the web client needs at run time. The only one that
 *               matters is where the web service lives.
 *
 * Why not hard coded
 *               The API address differs per machine: it is localhost while
 *               developing, and the laptop's network address when the site is
 *               opened from another device. Reading it from an environment
 *               variable lets each group member point at their own server
 *               without editing source, and the fallback keeps it working with
 *               no setup at all.
 *
 * Note        : This file holds NO secrets. Anything shipped to a browser is
 *               readable by the user, so API keys and passwords must never
 *               appear here. Only the service address does.
 * ---------------------------------------------------------------------------
 */

// Vite exposes variables beginning with VITE_ to the browser at build time.
// To override, create a file named .env.local containing:
//   VITE_API_BASE_URL=http://192.168.1.50:8081/api
export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8081/api';

/** Key used to remember the signed in session between page refreshes. */
export const AUTH_STORAGE_KEY = 'solar-microgrid-auth';

/** Role names, matching the values the API puts in the token. */
export const ROLES = {
  BACKOFFICE: 'Backoffice',
  GRID_OPERATOR: 'GridOperator',
  PROSUMER: 'Prosumer',
};
