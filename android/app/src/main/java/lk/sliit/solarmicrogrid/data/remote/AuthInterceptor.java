/*
 * ---------------------------------------------------------------------------
 * File        : AuthInterceptor.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Adds the user token to every request that goes out.
 *
 * Why here and not in each call
 *               Almost every endpoint needs the token. Adding it by hand in
 *               forty places gives forty chances to forget. A forgotten one
 *               shows up later as a 401 that is hard to explain.
 *               One place cannot be skipped by mistake.
 *
 * SOLID        Single Responsibility. It adds one header. Nothing else.
 *
 * Security     The header is added only when a token exists. Login and
 *               register run before sign in, so they go out clean. They never
 *               carry an old token from the last user.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote;

import androidx.annotation.NonNull;

import java.io.IOException;

import okhttp3.Interceptor;
import okhttp3.Request;
import okhttp3.Response;

public final class AuthInterceptor implements Interceptor {

    private static final String HEADER_AUTHORIZATION = "Authorization";
    private static final String BEARER_PREFIX = "Bearer ";

    private final TokenProvider tokenProvider;

    public AuthInterceptor(TokenProvider tokenProvider) {
        this.tokenProvider = tokenProvider;
    }

    @NonNull
    @Override
    public Response intercept(@NonNull Chain chain) throws IOException {

        Request original = chain.request();
        String token = tokenProvider.getBearerToken();

        // Nobody is signed in yet. Let the request pass as it is.
        // This is normal for /api/auth/login and /api/auth/register.
        if (token == null || token.trim().isEmpty()) {
            return chain.proceed(original);
        }

        Request authorised = original.newBuilder()
                .header(HEADER_AUTHORIZATION, BEARER_PREFIX + token)
                .build();

        return chain.proceed(authorised);
    }
}
