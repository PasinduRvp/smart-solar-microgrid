/*
 * ---------------------------------------------------------------------------
 * File        : UserProfileDto.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : A user account as the Web API returns it.
 *
 * Mirrors      UserResponseDto in the Web API. GET /api/profile answers with
 *               this shape.
 *
 * Note         There is no password field, in either direction. The API never
 *               returns a password or its hash. Changing a password is a
 *               separate call to PUT /api/profile/password.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class UserProfileDto {

    private String id;
    private String nic;
    private String fullName;
    private String email;
    private String phone;
    private String address;
    private String role;
    private String status;
    private boolean deactivationRequested;
    private double solarCapacityKw;
    private String createdAt;
    private String updatedAt;

    public String getId() {
        return id;
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

    public boolean isDeactivationRequested() {
        return deactivationRequested;
    }

    public double getSolarCapacityKw() {
        return solarCapacityKw;
    }

    public String getCreatedAt() {
        return createdAt;
    }

    public String getUpdatedAt() {
        return updatedAt;
    }
}
