/*
 * ---------------------------------------------------------------------------
 * File        : stationsApi.js
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Microgrid node calls. Reading is open to any signed in user;
 *               creating, editing and deactivating are Backoffice duties, and
 *               the API enforces that regardless of what this client sends.
 * ---------------------------------------------------------------------------
 */

import apiClient from './apiClient';

/** Lists nodes. Pass true to exclude deactivated ones. */
export async function getStations(activeOnly = false) {
  const { data } = await apiClient.get('/stations', { params: { activeOnly } });
  return data;
}

/** Returns one node by its identifier. */
export async function getStation(id) {
  const { data } = await apiClient.get(`/stations/${id}`);
  return data;
}

/** Registers a new node with its GPS position, capacity and schedule. */
export async function createStation(payload) {
  const { data } = await apiClient.post('/stations', payload);
  return data;
}

/** Updates a node's details. Station code and active state are not editable here. */
export async function updateStation(id, payload) {
  const { data } = await apiClient.put(`/stations/${id}`, payload);
  return data;
}

/** Replaces a node's weekly operating schedule. */
export async function updateStationSchedule(id, schedule) {
  const { data } = await apiClient.put(`/stations/${id}/schedule`, { schedule });
  return data;
}

/** Takes a node out of service. Refused by BR-4 while it has active bookings. */
export async function deactivateStation(id) {
  const { data } = await apiClient.patch(`/stations/${id}/deactivate`);
  return data;
}

/** Returns a deactivated node to service. */
export async function reactivateStation(id) {
  const { data } = await apiClient.patch(`/stations/${id}/reactivate`);
  return data;
}
