/*
 * ---------------------------------------------------------------------------
 * File        : AuthResponse.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : M T A J Yapa (IT 23278530)
 * Created     : 2026-09-20
 * Description : What POST /api/auth/login returns when it works.
 *
 * Mirrors      AuthResponseDto in the Web API.
 *
 * Security     The token is a signed JWT. Signed is not the same as
 *               encrypted. Anyone who sees it can read it. So never log it
 *               and never show it on screen.
 *               The role inside it is what the API trusts. The copy on the
 *               phone only picks which screen opens. It gives no access.
 *               Every protected call is still checked by the server.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class AuthResponse {

    private String token;
    private String expiresAtUtc;
    private String userId;
    private String nic;
    private String fullName;
    private String email;
    private String role;

    public String getToken() {
        return token;
    }

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
}
