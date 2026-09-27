/*
 * ---------------------------------------------------------------------------
 * File        : AuthApi.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : The two endpoints that need no token: sign in, and register
 *               as a Solar Prosumer.
 *
 * How Retrofit uses this
 *               Retrofit reads the annotations and writes the code at run
 *               time. This interface is the whole call. There is no hand
 *               written HTTP anywhere in the project.
 *
 * SOLID        Interface Segregation. Endpoints are grouped by the screen
 *               that uses them. ProfileApi and StationApi are separate.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote;

import lk.sliit.solarmicrogrid.data.remote.dto.AuthResponse;
import lk.sliit.solarmicrogrid.data.remote.dto.LoginRequest;
import lk.sliit.solarmicrogrid.data.remote.dto.RegisterProsumerRequest;
import retrofit2.Call;
import retrofit2.http.Body;
import retrofit2.http.POST;

public interface AuthApi {

    /**
     * Signs a user in. The identifier takes an email address or an NIC.
     *
     * Returns 200 with a token.
     * Returns 401 when the details are wrong.
     * Returns 403 when the account exists but is closed, or still waiting
     * for approval.
     */
    @POST("api/auth/login")
    Call<AuthResponse> login(@Body LoginRequest request);

    /**
     * Registers a new Solar Prosumer. The NIC is the primary key.
     *
     * The new account starts as Pending. A Backoffice officer must activate
     * it before the prosumer can sign in. That is BR-6. The server enforces
     * it, not this app.
     *
     * Returns 201 with the new account id as a plain JSON string.
     * Returns 409 when the NIC or the email is already used.
     *
     * The app does not need the id. The prosumer signs in with their NIC.
     * So callers usually only check that the call worked.
     */
    @POST("api/auth/register")
    Call<String> register(@Body RegisterProsumerRequest request);
}
