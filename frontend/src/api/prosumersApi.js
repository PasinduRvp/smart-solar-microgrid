/*
 * ---------------------------------------------------------------------------
 * File        : prosumersApi.js
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-20
 * Description : Prosumer account calls used by the back office screens.
 *
 * Note on registering
 *               A prosumer created from the web application goes through the
 *               same /auth/register endpoint the mobile application uses, so
 *               the account is created in the Pending state exactly as a self
 *               registration would be. Giving the back office a private route
 *               that skipped that would mean two ways of creating a prosumer
 *               with two different sets of rules, and BR-7 would then depend on
 *               which screen was used.
 * ---------------------------------------------------------------------------
 */

import apiClient from './apiClient';

/**
 * Lists prosumers, optionally limited to one account state.
 * @param {string} [status] Pending, Active or Deactivated.
 */
export async function getProsumers(status) {
  const { data } = await apiClient.get('/prosumers', {
    params: status ? { status } : undefined,
  });
  return data;
}

/** Lists the accounts awaiting activation, oldest request first. */
export async function getPendingProsumers() {
  const { data } = await apiClient.get('/prosumers/pending');
  return data;
}

/** Returns one prosumer by National Identity Card number. */
export async function getProsumer(nic) {
  const { data } = await apiClient.get(`/prosumers/${encodeURIComponent(nic)}`);
  return data;
}

/** Updates a prosumer profile. NIC, role and status cannot be changed here. */
export async function updateProsumer(nic, payload) {
  const { data } = await apiClient.put(`/prosumers/${encodeURIComponent(nic)}`, payload);
  return data;
}

/** Activates a pending or deactivated account. Backoffice only (BR-5). */
export async function activateProsumer(nic) {
  const { data } = await apiClient.patch(`/prosumers/${encodeURIComponent(nic)}/activate`);
  return data;
}

/** Deactivates an account. */
export async function deactivateProsumer(nic) {
  const { data } = await apiClient.patch(`/prosumers/${encodeURIComponent(nic)}/deactivate`);
  return data;
}

/** Registers a new prosumer. The account starts Pending, as BR-7 requires. */
export async function registerProsumer(payload) {
  const { data } = await apiClient.post('/auth/register', payload);
  return data;
}
