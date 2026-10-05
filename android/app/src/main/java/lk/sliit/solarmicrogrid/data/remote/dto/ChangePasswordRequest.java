/*
 * ---------------------------------------------------------------------------
 * File        : ChangePasswordRequest.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-20
 * Description : The body sent to PUT /api/profile/password.
 *
 * Mirrors      ChangePasswordRequestDto in the Web API.
 *
 * Why the current password is needed
 *               Without it, a stolen unlocked phone would be enough to take
 *               an account for good. Asking for the old password means the
 *               person holding the phone must know it too.
 *               The server checks it against the stored BCrypt hash. Nothing
 *               about it is checked on the phone.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class ChangePasswordRequest {

    private final String currentPassword;
    private final String newPassword;

    public ChangePasswordRequest(String currentPassword, String newPassword) {
        this.currentPassword = currentPassword;
        this.newPassword = newPassword;
    }

    public String getCurrentPassword() {
        return currentPassword;
    }

    public String getNewPassword() {
        return newPassword;
    }
}
