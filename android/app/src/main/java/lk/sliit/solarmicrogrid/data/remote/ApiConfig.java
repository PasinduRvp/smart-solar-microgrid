/*
 * ---------------------------------------------------------------------------
 * File        : ApiConfig.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : M T A J Yapa (IT 23278530)
 * Created     : 2026-09-20
 * Description : The address of the C# Web API, and how long to wait for it.
 *
 * Where the address now comes from
 *               local.properties, through BuildConfig. It used to be typed
 *               into this file, which meant every member had to edit source
 *               to run the app, and whoever committed last pushed their own
 *               IP onto everyone else.
 *
 *               local.properties is never committed, so each member keeps
 *               their own address and nobody overwrites anybody.
 *
 *               To change it, edit android/local.properties:
 *                   API_BASE_URL=http://10.214.53.228:8081/
 *               then rebuild. Gradle writes it into BuildConfig.
 *
 * Addresses
 *               10.214.53.228 : laptop, on the phone hotspot
 *               192.168.0.2   : laptop, on the USB dongle
 *               10.0.2.2      : the laptop, seen from the emulator
 *
 *               Find the address on the laptop with:  ipconfig
 *
 * Plain HTTP
 *               Debug builds are allowed to use it, through the network
 *               security config in src/debug. Release builds are not. IIS on
 *               a laptop has no certificate a phone would trust, so the demo
 *               runs on a debug build.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote;

import lk.sliit.solarmicrogrid.BuildConfig;

public final class ApiConfig {

    /**
     * Where the Web API on IIS answers.
     *
     * The slash at the end is required. Retrofit joins each endpoint path to
     * this value the way a browser joins a relative link. Without the slash
     * the last part of the path is replaced instead of added.
     *
     * It is http, not https. IIS on the laptop has no certificate a phone
     * would trust. The API only redirects to HTTPS in Development, so the
     * hosted site serves plain HTTP.
     */
    public static final String BASE_URL = BuildConfig.API_BASE_URL;

    /** How long to wait before deciding the server will not answer. */
    public static final int CONNECT_TIMEOUT_SECONDS = 15;
    public static final int READ_TIMEOUT_SECONDS = 30;

    /** Utility class: never instantiated. */
    private ApiConfig() {
    }
}
