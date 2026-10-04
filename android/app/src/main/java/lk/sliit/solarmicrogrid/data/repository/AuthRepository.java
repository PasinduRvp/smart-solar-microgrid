/*
 * ---------------------------------------------------------------------------
 * File        : AuthRepository.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : M T A J Yapa (IT 23278530)
 * Created     : 2026-09-20
 * Description : Signing in and registering, as the screens see them.
 *
 * What a repository is for
 *               A screen should say sign this person in. It should then be
 *               told either who they are, or what to show the user.
 *               It should not know about Retrofit, HTTP status codes, or
 *               that a good login has to be written to SQLite first.
 *               All of that lives here, once, instead of in every screen.
 *
 * About the FAT service rule
 *               The assignment asks for all business logic in the Web API,
 *               and it is there. This class decides nothing. It does not
 *               check whether an account is active. It does not check what
 *               a role may do. It calls the API, stores the reply, and
 *               reports the result.
 *
 * SOLID        Single Responsibility. Sign in calls only.
 * SOLID        Dependency Inversion. It is given an AuthApi and a
 *               SessionStore instead of making them. A test can pass fakes.
 *               create() is there for normal use.
 *
 * Threading    Retrofit calls back on the main thread. So the screen can
 *               touch its views directly. The session write is one small
 *               row and takes well under a millisecond. That is why it is
 *               done here and not on a worker thread.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.repository;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.SolarApp;
import lk.sliit.solarmicrogrid.data.local.SessionStore;
import lk.sliit.solarmicrogrid.data.remote.ApiClient;
import lk.sliit.solarmicrogrid.data.remote.ApiErrors;
import lk.sliit.solarmicrogrid.data.remote.AuthApi;
import lk.sliit.solarmicrogrid.data.remote.dto.AuthResponse;
import lk.sliit.solarmicrogrid.data.remote.dto.LoginRequest;
import lk.sliit.solarmicrogrid.data.remote.dto.RegisterProsumerRequest;
import lk.sliit.solarmicrogrid.model.Session;
import retrofit2.Call;
import retrofit2.Response;

public final class AuthRepository {

    /**
     * How a repository answers a screen.
     *
     * There are only two answers. It worked. Or here is a sentence to show
     * the user.
     *
     * The screen never sees a status code or an exception. So a raw technical
     * message cannot reach the user by mistake.
     */
    public interface Callback<T> {

        void onSuccess(@Nullable T value);

        void onFailure(@NonNull String message);
    }

    private final AuthApi authApi;
    private final SessionStore sessionStore;

    public AuthRepository(@NonNull AuthApi authApi, @NonNull SessionStore sessionStore) {
        this.authApi = authApi;
        this.sessionStore = sessionStore;
    }

    /** Used by the screens. A test uses the constructor instead. */
    @NonNull
    public static AuthRepository create() {
        return new AuthRepository(
                ApiClient.create(AuthApi.class),
                SolarApp.get().sessions());
    }

    /**
     * Signs a user in and saves the session.
     * The app can then reopen without asking for the password again.
     *
     * @param identifier an email address or an NIC. The API takes either.
     */
    public void login(@NonNull String identifier,
                      @NonNull String password,
                      @NonNull Callback<Session> callback) {

        authApi.login(new LoginRequest(identifier, password))
                .enqueue(new retrofit2.Callback<AuthResponse>() {

                    @Override
                    public void onResponse(@NonNull Call<AuthResponse> call,
                                           @NonNull Response<AuthResponse> response) {

                        AuthResponse body = response.body();

                        // Without this check, a 200 with an empty body
                        // would be saved as a session with no token. Every
                        // later call would then fail with a 401.
                        if (!response.isSuccessful() || body == null || body.getToken() == null) {
                            callback.onFailure(ApiErrors.messageFrom(response));
                            return;
                        }

                        sessionStore.save(body);

                        // A new token is in place, so a later expiry must be
                        // reported again.
                        ApiClient.resetSessionExpiry();

                        Session session = sessionStore.read();
                        if (session == null) {
                            callback.onFailure(offlineMessage());
                            return;
                        }
                        callback.onSuccess(session);
                    }

                    @Override
                    public void onFailure(@NonNull Call<AuthResponse> call, @NonNull Throwable t) {
                        // Only reached when no answer came back at all.
                        // Wrong IP, server not running, or no network.
                        callback.onFailure(offlineMessage());
                    }
                });
    }

    /**
     * Registers a new Solar Prosumer.
     *
     * No session is made. A new account stays Pending until a Backoffice
     * officer activates it. That is BR-6. So the user goes back to sign in,
     * not into the app.
     */
    public void register(@NonNull RegisterProsumerRequest request,
                         @NonNull Callback<Void> callback) {

        authApi.register(request).enqueue(new retrofit2.Callback<String>() {

            @Override
            public void onResponse(@NonNull Call<String> call, @NonNull Response<String> response) {
                if (!response.isSuccessful()) {
                    callback.onFailure(ApiErrors.messageFrom(response));
                    return;
                }
                callback.onSuccess(null);
            }

            @Override
            public void onFailure(@NonNull Call<String> call, @NonNull Throwable t) {
                callback.onFailure(offlineMessage());
            }
        });
    }

    @NonNull
    private static String offlineMessage() {
        return SolarApp.get().getString(R.string.error_no_connection);
    }
}
