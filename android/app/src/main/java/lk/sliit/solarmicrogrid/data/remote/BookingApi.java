/*
 * ---------------------------------------------------------------------------
 * File        : BookingApi.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Booking windows and energy reservations. Tasks D1 to D5.
 *
 * Windows and bookings are kept together
 *              A booking is always made against a window. The two are never
 *              used apart. Two interfaces would only mean two objects to
 *              create for one screen.
 *
 * Token       Both controllers are behind [Authorize]. AuthInterceptor adds
 *              the token, so no method below mentions it.
 *
 * SOLID       Interface Segregation. Booking calls only. Nodes are in
 *              StationApi. The account is in ProfileApi.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote;

import java.util.List;

import lk.sliit.solarmicrogrid.data.remote.dto.CancelReservationRequest;
import lk.sliit.solarmicrogrid.data.remote.dto.CreateReservationRequest;
import lk.sliit.solarmicrogrid.data.remote.dto.QrTokenDto;
import lk.sliit.solarmicrogrid.data.remote.dto.ReservationDto;
import lk.sliit.solarmicrogrid.data.remote.dto.SlotDto;
import lk.sliit.solarmicrogrid.data.remote.dto.UpdateReservationRequest;
import lk.sliit.solarmicrogrid.data.remote.dto.VerifyQrRequest;
import retrofit2.Call;
import retrofit2.http.Body;
import retrofit2.http.GET;
import retrofit2.http.PATCH;
import retrofit2.http.POST;
import retrofit2.http.PUT;
import retrofit2.http.Path;
import retrofit2.http.Query;

public interface BookingApi {

    /**
     * The booking windows at one node on one day.
     *
     * @param date the day wanted, as yyyy-MM-dd.
     */
    @GET("api/stations/{stationId}/slots")
    Call<List<SlotDto>> getSlots(
            @Path("stationId") String stationId,
            @Query("date") String date);

    /**
     * Searches bookings.
     *
     * A prosumer passes their own NIC. The server also limits a prosumer to
     * their own bookings, so another NIC here returns nothing useful.
     */
    @GET("api/reservations")
    Call<List<ReservationDto>> searchReservations(@Query("nic") String nic);

    /** One booking, fresh from the server. */
    @GET("api/reservations/{id}")
    Call<ReservationDto> getReservation(@Path("id") String id);

    /**
     * Books a window. Task D2.
     *
     * Returns 201 with the new booking.
     * Returns 409 when the window filled up first. That is BR-9, and the
     * server settles it, not the app.
     */
    @POST("api/reservations")
    Call<ReservationDto> createReservation(@Body CreateReservationRequest request);

    /**
     * Changes a booking. Task D3.
     *
     * Returns 400 when the booking is less than 12 hours away. That is BR-2.
     */
    @PUT("api/reservations/{id}")
    Call<ReservationDto> updateReservation(
            @Path("id") String id,
            @Body UpdateReservationRequest request);

    /**
     * Cancels a booking. Task D4.
     *
     * Returns 400 when the booking is less than 12 hours away. That is BR-3.
     */
    @PATCH("api/reservations/{id}/cancel")
    Call<ReservationDto> cancelReservation(
            @Path("id") String id,
            @Body CancelReservationRequest request);

    /**
     * The QR code for an approved booking. Task E3.
     *
     * Prosumers only. Returns 409 while the booking is still Pending,
     * because a code is only issued once a booking is approved. That is
     * business rule BR-7.
     */
    @GET("api/reservations/{id}/qr")
    Call<QrTokenDto> getQrToken(@Path("id") String id);

    /**
     * Checks a scanned QR code and returns the booking it belongs to.
     * Task E4.
     *
     * Grid Operators and Backoffice officers only.
     * Returns 404 when no booking has that token.
     * Returns 409 when the booking is cancelled or already finished.
     */
    @POST("api/reservations/verify-qr")
    Call<ReservationDto> verifyQr(@Body VerifyQrRequest request);

    /**
     * Marks the energy transfer as done. Task E5.
     *
     * Grid Operators and Backoffice officers only.
     */
    @PATCH("api/reservations/{id}/complete")
    Call<ReservationDto> completeReservation(@Path("id") String id);
}
