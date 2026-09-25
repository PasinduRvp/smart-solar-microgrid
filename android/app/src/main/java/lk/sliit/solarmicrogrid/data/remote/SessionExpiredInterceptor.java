/*
 * ---------------------------------------------------------------------------
 * File        : SessionExpiredInterceptor.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-21
 * Description : Notices when the saved token has stopped working, and says
 *              so once.
 *
 * The problem this fixes
 *              A token lasts two hours. After that the server answers 401 to
 *              every call. Without this class the app kept the dead token,
 *              showed the message for wrong details, and left the user on a
 *              screen that could never load. Signing out was the only way
 *              forward, and nothing said so.
 *
 * Why it checks for the Authorization header
 *              A wrong password on the sign in screen also returns 401. That
 *              request carries no token, so it is not a dead session and
 *              must not send the user anywhere. Only a request that carried
 *              a token counts.
 *
 * Why it fires only once
 *              A screen can start three calls at once, as the dashboard
 *              does. All three come back 401 together. Without the guard the
 *              sign in screen would be opened three times.
 *              The guard is cleared after each report, so a later expiry is
 *              still noticed.
 *
 * SOLID       Single Responsibility. It spots one status code and reports
 *              it. It does not sign anybody out and it opens no screens.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote;

import androidx.annotation.NonNull;

import java.io.IOException;
import java.net.HttpURLConnection;
import java.util.concurrent.atomic.AtomicBoolean;

import okhttp3.Interceptor;
import okhttp3.Request;
import okhttp3.Response;

public final class SessionExpiredInterceptor implements Interceptor {

    private static final String HEADER_AUTHORIZATION = "Authorization";

    private final SessionExpiredHandler handler;

    /** Stops several calls failing at once from reporting it several times. */
    private final AtomicBoolean alreadyReported = new AtomicBoolean(false);

    public SessionExpiredInterceptor(@NonNull SessionExpiredHandler handler) {
        this.handler = handler;
    }

    @NonNull
    @Override
    public Response intercept(@NonNull Chain chain) throws IOException {

        Request request = chain.request();
        Response response = chain.proceed(request);

        boolean carriedToken = request.header(HEADER_AUTHORIZATION) != null;
        boolean refused = response.code() == HttpURLConnection.HTTP_UNAUTHORIZED;

        // compareAndSet returns true only for the first caller, so only one
        // of several failing calls reports it.
        if (carriedToken && refused && alreadyReported.compareAndSet(false, true)) {
            handler.onSessionExpired();
        }

        return response;
    }

    /**
     * Lets it report again.
     *
     * Called after the user signs in, so that the next time their token runs
     * out it is noticed just the same.
     */
    public void allowReportingAgain() {
        alreadyReported.set(false);
    }
}
