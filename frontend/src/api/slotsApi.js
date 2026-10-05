/*
 * ---------------------------------------------------------------------------
 * File        : slotsApi.js
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Booking window calls.
 *
 * Note        : Windows are addressed under the node they belong to when they
 *               are listed or generated, because a window has no meaning apart
 *               from its node. Acting on a single window uses its own address.
 * ---------------------------------------------------------------------------
 */

import apiClient from './apiClient';

/** Lists the windows at a node. Omit the date to see the next seven days. */
export async function getStationSlots(stationId, date) {
  const { data } = await apiClient.get(`/stations/${stationId}/slots`, {
    params: date ? { date } : undefined,
  });
  return data;
}

/** Generates windows for a node from its weekly schedule. */
export async function generateSlots(stationId, payload) {
  const { data } = await apiClient.post(`/stations/${stationId}/slots/generate`, payload);
  return data;
}

/** Opens or closes a window, and optionally changes its capacity. */
export async function updateSlotAvailability(slotId, payload) {
  const { data } = await apiClient.patch(`/slots/${slotId}/availability`, payload);
  return data;
}
