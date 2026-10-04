/*
 * ---------------------------------------------------------------------------
 * File        : RegisterProsumerRequest.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-20
 * Description : The body sent to POST /api/auth/register.
 *
 * Mirrors      RegisterProsumerRequestDto in the Web API.
 *
 * Validation   The server checks every field. It returns RFC 7807 problem
 *               details saying what was wrong.
 *               The app also checks the easy ones first. Then the user is not
 *               made to wait for the server to be told a box is empty.
 *               Client checks are for comfort. The server check is the one
 *               that protects the data.
 *
 * Server rules NIC      : 9 digits then V or X, or 12 digits
 *               Phone    : 10 digits starting with 0, e.g. 0771234567
 *               Password : at least 8 characters
 *               Capacity : 0.1 to 1000 kW
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class RegisterProsumerRequest {

    private final String nic;
    private final String fullName;
    private final String email;
    private final String phone;
    private final String address;
    private final double solarCapacityKw;
    private final String password;

    public RegisterProsumerRequest(String nic,
                                   String fullName,
                                   String email,
                                   String phone,
                                   String address,
                                   double solarCapacityKw,
                                   String password) {
        this.nic = nic;
        this.fullName = fullName;
        this.email = email;
        this.phone = phone;
        this.address = address;
        this.solarCapacityKw = solarCapacityKw;
        this.password = password;
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

    public double getSolarCapacityKw() {
        return solarCapacityKw;
    }

    public String getPassword() {
        return password;
    }
}
