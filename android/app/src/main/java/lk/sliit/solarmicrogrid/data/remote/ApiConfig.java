/*
 * ---------------------------------------------------------------------------
 * File        : ApiConfig.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : The address of the C# Web API. Written down once.
 *
 * Why one file
 *               The address is used by every screen. Here it is one line to
 *               change when the laptop moves to another network.
 *
 * IMPORTANT     If you change BASE_URL, change the matching <domain> in
 *               res/xml/network_security_config.xml too.
 *               Android blocks plain HTTP to any host not listed there.
 *               Change only one, and you get this error:
 *               CLEARTEXT communication to ... not permitted
 *
 * Addresses
 *               10.214.53.228 : laptop, on the phone hotspot
 *               192.168.0.2   : laptop, on the USB dongle
 *               10.0.2.2      : the laptop, seen from the emulator
 *
 *               Find the address on the laptop with:  ipconfig
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote;

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
    public static final String BASE_URL = "http://10.214.53.228:8081/";

    /** How long to wait before deciding the server will not answer. */
    public static final int CONNECT_TIMEOUT_SECONDS = 15;
    public static final int READ_TIMEOUT_SECONDS = 30;

    /** Utility class: never instantiated. */
    private ApiConfig() {
    }
}
