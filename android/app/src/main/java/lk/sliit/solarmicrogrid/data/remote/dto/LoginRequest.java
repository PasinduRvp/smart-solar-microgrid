/*
 * ---------------------------------------------------------------------------
 * File        : LoginRequest.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : The body sent to POST /api/auth/login.
 *
 * Mirrors      LoginRequestDto in the Web API. The field names must match
 *               the JSON the server expects. ASP.NET Core sends properties as
 *               camelCase. These names are already camelCase, so no Gson
 *               @SerializedName is needed.
 *
 * Why it is called identifier, not email
 *               The API accepts an email address or an NIC in this one field.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class LoginRequest {

    private final String identifier;
    private final String password;

    public LoginRequest(String identifier, String password) {
        this.identifier = identifier;
        this.password = password;
    }

    public String getIdentifier() {
        return identifier;
    }

    public String getPassword() {
        return password;
    }
}
