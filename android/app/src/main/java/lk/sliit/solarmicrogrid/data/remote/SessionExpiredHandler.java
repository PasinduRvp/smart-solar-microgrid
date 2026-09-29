/*
 * ---------------------------------------------------------------------------
 * File        : SessionExpiredHandler.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-21
 * Description : Told when the server stops accepting the saved token.
 *
 * SOLID        Dependency Inversion. SessionExpiredInterceptor knows that a
 *              401 means the session is over. It does not know what to do
 *              about it, because signing out and opening a screen is not a
 *              job for networking code.
 *              SolarApp implements this and does that part.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote;

public interface SessionExpiredHandler {

    /**
     * Called when a request that carried a token was refused with 401.
     *
     * This runs on a background thread, so anything that touches the screen
     * has to be moved to the main thread by the implementation.
     */
    void onSessionExpired();
}
