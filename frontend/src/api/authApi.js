/*
 * ---------------------------------------------------------------------------
 * File        : authApi.js
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : The authentication calls the web application makes.
 *
 * Why grouped : Each API area gets its own small module — authApi, usersApi,
 *               stationsApi and so on. A component then imports one named
 *               function instead of building a URL, which keeps endpoint paths
 *               out of the screens and in one place per area.
 * ---------------------------------------------------------------------------
 */

import apiClient from './apiClient';

/**
 * Signs a user in.
 *
 * @param {string} identifier Email address for staff, NIC for prosumers.
 * @param {string} password   The account password.
 * @returns The token and the signed in user's details.
 */
export async function login(identifier, password) {
  const { data } = await apiClient.post('/auth/login', { identifier, password });
  return data;
}

/** Reports whether the service and its database are available. */
export async function checkHealth() {
  const { data } = await apiClient.get('/health');
  return data;
}
