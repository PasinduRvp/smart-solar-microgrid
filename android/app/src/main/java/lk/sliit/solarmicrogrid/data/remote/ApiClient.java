/*
 * ---------------------------------------------------------------------------
 * File        : ApiClient.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Builds the one Retrofit object the whole app shares.
 *
 * Why only one
 *               A Retrofit object holds an OkHttp client. That client holds a
 *               thread pool and a connection pool. Making a new one per screen
 *               wastes memory. It also loses connection reuse, which is what
 *               makes the second call faster than the first.
 *
 * SOLID        Single Responsibility. It builds the HTTP stack. It does not
 *               know which endpoints exist. Callers ask for what they need
 *               with create(SomeApi.class).
 *
 * Threading    enqueue() runs the call on a background thread. It returns on
 *               the main thread. So screens never use the network on the UI
 *               thread. Android would throw NetworkOnMainThreadException.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote;

import java.util.concurrent.TimeUnit;

import lk.sliit.solarmicrogrid.BuildConfig;
import okhttp3.OkHttpClient;
import okhttp3.logging.HttpLoggingInterceptor;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;

public final class ApiClient {

    private static Retrofit retrofit;
    private static SessionExpiredInterceptor sessionExpiredInterceptor;

    /**
     * Builds the HTTP stack. Called once, from SolarApp.onCreate().
     *
     * @param tokenProvider        where the token of the signed in user
     *                             comes from.
     * @param sessionExpiredHandler told when the server stops accepting that
     *                             token.
     */
    public static synchronized void init(TokenProvider tokenProvider,
                                         SessionExpiredHandler sessionExpiredHandler) {

        if (retrofit != null) {
            return;
        }

        sessionExpiredInterceptor = new SessionExpiredInterceptor(sessionExpiredHandler);

        OkHttpClient.Builder http = new OkHttpClient.Builder()
                .connectTimeout(ApiConfig.CONNECT_TIMEOUT_SECONDS, TimeUnit.SECONDS)
                .readTimeout(ApiConfig.READ_TIMEOUT_SECONDS, TimeUnit.SECONDS)
                .addInterceptor(new AuthInterceptor(tokenProvider))

                // Added after AuthInterceptor, so it sees the request with
                // the token already on it.
                .addInterceptor(sessionExpiredInterceptor);

        // While developing, the request and reply bodies are printed to
        // Logcat. That is how a mismatch between the JSON of the app and the
        // JSON of the API is found in seconds instead of by guessing.
        //
        // This is on in debug builds and off in release, on purpose.
        // The login request holds a password. The reply holds a token.
        // Neither belongs in the log of a shipped app.
        if (BuildConfig.DEBUG) {
            HttpLoggingInterceptor logging = new HttpLoggingInterceptor();
            logging.setLevel(HttpLoggingInterceptor.Level.BODY);
            http.addInterceptor(logging);
        }

        retrofit = new Retrofit.Builder()
                .baseUrl(ApiConfig.BASE_URL)
                .client(http.build())
                .addConverterFactory(GsonConverterFactory.create())
                .build();
    }

    /**
     * Lets an expired session be reported again.
     *
     * Called after a successful sign in. Without it, the first expiry would
     * be the only one ever noticed in this run of the app.
     */
    public static synchronized void resetSessionExpiry() {
        if (sessionExpiredInterceptor != null) {
            sessionExpiredInterceptor.allowReportingAgain();
        }
    }

    /**
     * Returns a working copy of one of the Api interfaces.
     *
     * @param apiInterface for example AuthApi.class
     */
    public static <T> T create(Class<T> apiInterface) {
        if (retrofit == null) {
            throw new IllegalStateException(
                    "ApiClient.init() was never called. It belongs in SolarApp.onCreate().");
        }
        return retrofit.create(apiInterface);
    }

    /** Utility class: never instantiated. */
    private ApiClient() {
    }
}
