/*
 * ---------------------------------------------------------------------------
 * File        : usersApi.js
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Staff account calls. Every endpoint behind these is restricted
 *               to Backoffice officers by the API.
 * ---------------------------------------------------------------------------
 */

import apiClient from './apiClient';

/** Lists all Backoffice and Grid Operator accounts. */
export async function getStaffUsers() {
  const { data } = await apiClient.get('/users');
  return data;
}

/** Creates a Backoffice or Grid Operator account. */
export async function createStaffUser(payload) {
  const { data } = await apiClient.post('/users', payload);
  return data;
}

/** Updates the editable details of an account. */
export async function updateStaffUser(id, payload) {
  const { data } = await apiClient.put(`/users/${id}`, payload);
  return data;
}

/** Permanently removes a staff account. */
export async function deleteStaffUser(id) {
  const { data } = await apiClient.delete(`/users/${id}`);
  return data;
}
