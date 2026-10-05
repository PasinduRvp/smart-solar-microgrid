/*
 * ---------------------------------------------------------------------------
 * File        : dashboardApi.js
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Calls for the summary figures shown on the staff home screen.
 *
 * Note        : Every number on the dashboard arrives already calculated. This
 *               client counts nothing and derives nothing; it asks the service
 *               and renders the answer.
 * ---------------------------------------------------------------------------
 */

import apiClient from './apiClient';

/** Summary figures for Grid Operators and Backoffice officers. */
export async function getOperatorDashboard() {
  const { data } = await apiClient.get('/dashboard/operator');
  return data;
}

/** Summary figures for one prosumer, by National Identity Card number. */
export async function getProsumerDashboard(nic) {
  const { data } = await apiClient.get(`/dashboard/prosumer/${encodeURIComponent(nic)}`);
  return data;
}
