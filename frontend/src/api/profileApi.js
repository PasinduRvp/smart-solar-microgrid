/*
 * ---------------------------------------------------------------------------
 * File        : profileApi.js
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-20
 * Description : Calls for the signed in user's own account.
 *
 * Note        : None of these takes an account id. The account is always the
 *               one in the token, so there is nothing a caller could change to
 *               aim one of these at somebody else's record.
 * ---------------------------------------------------------------------------
 */

import apiClient from './apiClient';

/** Returns your own profile. */
export async function getMyProfile() {
  const { data } = await apiClient.get('/profile');
  return data;
}

/** Updates your own editable details. */
export async function updateMyProfile(payload) {
  const { data } = await apiClient.put('/profile', payload);
  return data;
}

/** Changes your own password. The current one must be supplied. */
export async function changeMyPassword(currentPassword, newPassword) {
  const { data } = await apiClient.put('/profile/password', { currentPassword, newPassword });
  return data;
}
