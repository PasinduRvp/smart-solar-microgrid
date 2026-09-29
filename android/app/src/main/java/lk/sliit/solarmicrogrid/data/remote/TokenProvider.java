/*
 * ---------------------------------------------------------------------------
 * File        : TokenProvider.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Gives the token of the user who is signed in.
 *               Returns null when nobody is signed in.
 *
 * SOLID        Dependency Inversion. AuthInterceptor needs a token. It does
 *               not need to know the token is kept in SQLite. It uses this
 *               small interface. SessionStore implements it.
 *
 * SOLID        Interface Segregation. One method, because that is all the
 *               interceptor uses. A bigger interface would force a test fake
 *               to write methods nothing calls.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote;

import androidx.annotation.Nullable;

public interface TokenProvider {

    /** @return the token to send, or null when nobody is signed in. */
    @Nullable
    String getBearerToken();
}
