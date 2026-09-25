/*
 * ---------------------------------------------------------------------------
 * File        : Session.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Who is signed in, as the rest of the app sees them.
 *
 * Why not reuse AuthResponse
 *               AuthResponse is the shape of one HTTP reply. It belongs to
 *               the network layer. Session is what the app works with.
 *               Keeping them apart means a change to the login reply, such
 *               as a renamed field, is handled in one place. It does not
 *               spread into every screen.
 *
 * Immutable    All fields are final. One screen cannot half change a user
 *               while another screen reads it. That removes a class of bug
 *               with no locking.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.model;

import androidx.annotation.NonNull;

public final class Session {

    private final String token;
    private final String expiresAtUtc;
    private final String userId;
    private final String nic;
    private final String fullName;
    private final String email;
    private final String role;

    public Session(@NonNull String token,
                   @NonNull String expiresAtUtc,
                   @NonNull String userId,
                   @NonNull String nic,
                   @NonNull String fullName,
                   @NonNull String email,
                   @NonNull String role) {
        this.token = token;
        this.expiresAtUtc = expiresAtUtc;
        this.userId = userId;
        this.nic = nic;
        this.fullName = fullName;
        this.email = email;
        this.role = role;
    }

    public String getToken() {
        return token;
    }

    /** ISO-8601 UTC, exactly as the server sent it. */
    public String getExpiresAtUtc() {
        return expiresAtUtc;
    }

    public String getUserId() {
        return userId;
    }

    public String getNic() {
        return nic;
    }

    public String getFullName() {
        return fullName;
    }

    public String getEmail() {
        return email;
    }

    public String getRole() {
        return role;
    }

    public boolean isProsumer() {
        return Roles.isProsumer(role);
    }

    public boolean isGridOperator() {
        return Roles.isGridOperator(role);
    }

    public boolean isBackoffice() {
        return Roles.isBackoffice(role);
    }

    /**
     * The token is left out on purpose.
     *
     * toString() ends up in crash reports and in Logcat. A token in either of
     * those is a working key to the account until it expires.
     */
    @NonNull
    @Override
    public String toString() {
        return "Session{nic=" + nic + ", role=" + role + "}";
    }
}
