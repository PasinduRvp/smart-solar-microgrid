/*
 * ---------------------------------------------------------------------------
 * File        : UserProfile.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : A user account as the app works with it.
 *
 * Why this and UserProfileDto both exist
 *               UserProfileDto is the shape of a JSON reply. Gson fills it.
 *               UserProfile is what screens and SQLite use.
 *               Same idea as Session and AuthResponse, for the same reason.
 *               A profile can come from the network or from SQLite. Neither
 *               side should know about the other.
 *
 * Immutable    Every field is final. A profile cannot be half updated
 *               while another screen reads it.
 *
 * Status       Pending     - registered, waiting for a Backoffice officer
 *               Active      - may sign in and book
 *               Deactivated - blocked. Only Backoffice can turn it back on.
 *                             That is business rule BR-6.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.model;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.data.remote.dto.UserProfileDto;

public final class UserProfile {

    public static final String STATUS_PENDING = "Pending";
    public static final String STATUS_ACTIVE = "Active";
    public static final String STATUS_DEACTIVATED = "Deactivated";

    private final String nic;
    private final String fullName;
    private final String email;
    private final String phone;
    private final String address;
    private final String role;
    private final String status;
    private final boolean deactivationRequested;
    private final double solarCapacityKw;
    private final String updatedAt;

    public UserProfile(@NonNull String nic,
                       @NonNull String fullName,
                       @NonNull String email,
                       @NonNull String phone,
                       @NonNull String address,
                       @NonNull String role,
                       @NonNull String status,
                       boolean deactivationRequested,
                       double solarCapacityKw,
                       @NonNull String updatedAt) {
        this.nic = nic;
        this.fullName = fullName;
        this.email = email;
        this.phone = phone;
        this.address = address;
        this.role = role;
        this.status = status;
        this.deactivationRequested = deactivationRequested;
        this.solarCapacityKw = solarCapacityKw;
        this.updatedAt = updatedAt;
    }

    /**
     * Turns the reply from GET /api/profile into the model the app uses.
     *
     * Nulls become empty strings here, once. Then no screen has to check for
     * null before putting a field in a TextView.
     */
    @NonNull
    public static UserProfile from(@NonNull UserProfileDto dto) {
        return new UserProfile(
                orEmpty(dto.getNic()),
                orEmpty(dto.getFullName()),
                orEmpty(dto.getEmail()),
                orEmpty(dto.getPhone()),
                orEmpty(dto.getAddress()),
                orEmpty(dto.getRole()),
                orEmpty(dto.getStatus()),
                dto.isDeactivationRequested(),
                dto.getSolarCapacityKw(),
                orEmpty(dto.getUpdatedAt()));
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

    public String getPhone() {
        return phone;
    }

    public String getAddress() {
        return address;
    }

    public String getRole() {
        return role;
    }

    public String getStatus() {
        return status;
    }

    /** True once the user has asked to have their account deactivated (BR-6). */
    public boolean isDeactivationRequested() {
        return deactivationRequested;
    }

    public double getSolarCapacityKw() {
        return solarCapacityKw;
    }

    public String getUpdatedAt() {
        return updatedAt;
    }

    public boolean isActive() {
        return STATUS_ACTIVE.equals(status);
    }

    public boolean isPending() {
        return STATUS_PENDING.equals(status);
    }

    @NonNull
    private static String orEmpty(@Nullable String value) {
        return value == null ? "" : value;
    }
}
