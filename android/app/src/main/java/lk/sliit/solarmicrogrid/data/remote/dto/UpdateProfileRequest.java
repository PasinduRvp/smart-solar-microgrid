/*
 * ---------------------------------------------------------------------------
 * File        : UpdateProfileRequest.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-20
 * Description : The body sent to PUT /api/profile.
 *
 * Mirrors      UpdateMyProfileRequestDto in the Web API.
 *
 * What is missing is the point
 *               There is no nic, no role and no status field. The endpoint
 *               takes no account id either. The server works out whose
 *               profile it is from the token.
 *               That is what stops one user editing another user. It is
 *               enforced on the server, not by hiding a button.
 *
 * Note         Capacity may be 0 here. At registration the smallest is 0.1.
 *               A Grid Operator has no solar panels, and uses this same
 *               endpoint.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class UpdateProfileRequest {

    private final String fullName;
    private final String email;
    private final String phone;
    private final String address;
    private final double solarCapacityKw;

    public UpdateProfileRequest(String fullName,
                                String email,
                                String phone,
                                String address,
                                double solarCapacityKw) {
        this.fullName = fullName;
        this.email = email;
        this.phone = phone;
        this.address = address;
        this.solarCapacityKw = solarCapacityKw;
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

    public double getSolarCapacityKw() {
        return solarCapacityKw;
    }
}
