/*
 * ---------------------------------------------------------------------------
 * File        : VerifyQrRequest.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : The body sent to POST /api/reservations/verify-qr. Task E4.
 *
 * Mirrors      VerifyQrRequestDto in the Web API.
 *
 * The scanned text is sent straight through
 *              The app does not read the token, check it, or take it apart.
 *              It sends what the camera read. The server decides whether it
 *              belongs to a real booking and whether that booking is still
 *              valid.
 *              That is what stops a copied or edited code working.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

public final class VerifyQrRequest {

    private final String qrToken;

    public VerifyQrRequest(String qrToken) {
        this.qrToken = qrToken;
    }

    public String getQrToken() {
        return qrToken;
    }
}
