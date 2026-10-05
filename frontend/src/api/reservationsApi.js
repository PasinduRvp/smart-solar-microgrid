/*
 * ---------------------------------------------------------------------------
 * File        : reservationsApi.js
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Reservation calls used by the back office screens.
 *
 * Why searching happens on the server
 *               Unlike the prosumer and staff lists, booking history grows
 *               without limit. Fetching every reservation and filtering in the
 *               browser would get slower every week the system runs, so the
 *               filters are passed to the API and the database does the work
 *               using the indexes created at startup.
 * ---------------------------------------------------------------------------
 */

import apiClient from './apiClient';

/**
 * Searches reservations. Any filter left undefined simply widens the search.
 *
 * @param {object} filters
 * @param {string} [filters.nic]       Limit to one prosumer.
 * @param {string} [filters.status]    Pending, Approved, Completed or Cancelled.
 * @param {string} [filters.from]      Earliest reservation time, ISO format.
 * @param {string} [filters.to]        Latest reservation time, ISO format.
 * @param {string} [filters.stationId] Limit to one microgrid node.
 */
export async function searchReservations(filters = {}) {
  // Blank boxes are dropped rather than sent as empty strings, which the API
  // would otherwise treat as a filter matching nothing.
  const params = Object.fromEntries(
    Object.entries(filters).filter(([, value]) => value !== undefined && value !== ''),
  );

  const { data } = await apiClient.get('/reservations', { params });
  return data;
}

/** Lists bookings awaiting approval, soonest first. */
export async function getPendingReservations() {
  const { data } = await apiClient.get('/reservations/pending');
  return data;
}

/** Returns one booking. */
export async function getReservation(id) {
  const { data } = await apiClient.get(`/reservations/${id}`);
  return data;
}

/** Creates a booking. Staff supply the prosumer's NIC to book on their behalf. */
export async function createReservation(payload) {
  const { data } = await apiClient.post('/reservations', payload);
  return data;
}

/** Changes a booking. Subject to the 12 hour rule (BR-2). */
export async function updateReservation(id, payload) {
  const { data } = await apiClient.put(`/reservations/${id}`, payload);
  return data;
}

/** Cancels a booking. Subject to the 12 hour rule (BR-3). */
export async function cancelReservation(id, reason) {
  const { data } = await apiClient.patch(`/reservations/${id}/cancel`, { reason });
  return data;
}

/** Approves a booking and issues its QR token (BR-8). */
export async function approveReservation(id) {
  const { data } = await apiClient.patch(`/reservations/${id}/approve`);
  return data;
}

/** Finalises the energy transfer after a successful scan. */
export async function completeReservation(id) {
  const { data } = await apiClient.patch(`/reservations/${id}/complete`);
  return data;
}

/** Verifies a scanned QR token against the stored booking. */
export async function verifyQrToken(qrToken) {
  const { data } = await apiClient.post('/reservations/verify-qr', { qrToken });
  return data;
}
