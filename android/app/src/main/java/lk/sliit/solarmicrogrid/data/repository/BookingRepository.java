/*
 * ---------------------------------------------------------------------------
 * File        : BookingRepository.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Booking windows and reservations, as the screens see them.
 *               Tasks D1 to D5.
 *
 * Nothing is cached here
 *              The other repositories keep a copy in SQLite. This one does
 *              not, on purpose.
 *
 *              A booking window fills up as other prosumers book it. A saved
 *              copy would let a user pick a window that is already full, and
 *              only find out after tapping Book. The same goes for the 12
 *              hour rule: a booking that could be changed an hour ago may not
 *              be changeable now.
 *
 *              So every screen here asks the server. Being right matters more
 *              than being instant.
 *
 * No rules live here
 *              This class checks nothing. It does not decide whether a window
 *              is bookable, or whether a booking is too close to change. The
 *              Web API decides all of that and says so in its reply. That is
 *              what the assignment means by a FAT service.
 *
 * SOLID       Single Responsibility. Booking calls only.
 * SOLID       Dependency Inversion. The API is passed in, so a test can use
 *              a fake without a server.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.repository;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import java.util.ArrayList;
import java.util.Date;
import java.util.List;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.SolarApp;
import lk.sliit.solarmicrogrid.data.remote.ApiClient;
import lk.sliit.solarmicrogrid.data.remote.ApiErrors;
import lk.sliit.solarmicrogrid.data.remote.BookingApi;
import lk.sliit.solarmicrogrid.data.remote.dto.CancelReservationRequest;
import lk.sliit.solarmicrogrid.data.remote.dto.CreateReservationRequest;
import lk.sliit.solarmicrogrid.data.remote.dto.ReservationDto;
import lk.sliit.solarmicrogrid.data.remote.dto.QrTokenDto;
import lk.sliit.solarmicrogrid.data.remote.dto.SlotDto;
import lk.sliit.solarmicrogrid.data.remote.dto.VerifyQrRequest;
import lk.sliit.solarmicrogrid.data.remote.dto.UpdateReservationRequest;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository.Callback;
import lk.sliit.solarmicrogrid.model.EnergySlot;
import lk.sliit.solarmicrogrid.model.QrToken;
import lk.sliit.solarmicrogrid.model.Reservation;
import lk.sliit.solarmicrogrid.util.DateTimes;
import retrofit2.Call;
import retrofit2.Response;

public final class BookingRepository {

    private final BookingApi bookingApi;

    public BookingRepository(@NonNull BookingApi bookingApi) {
        this.bookingApi = bookingApi;
    }

    @NonNull
    public static BookingRepository create() {
        return new BookingRepository(ApiClient.create(BookingApi.class));
    }

    /**
     * The booking windows at one node on one day. Task D1.
     */
    public void loadSlots(@NonNull String stationId,
                          @NonNull Date day,
                          @NonNull Callback<List<EnergySlot>> callback) {

        bookingApi.getSlots(stationId, DateTimes.asQueryDate(day))
                .enqueue(new retrofit2.Callback<List<SlotDto>>() {

                    @Override
                    public void onResponse(@NonNull Call<List<SlotDto>> call,
                                           @NonNull Response<List<SlotDto>> response) {

                        List<SlotDto> body = response.body();

                        if (!response.isSuccessful() || body == null) {
                            callback.onFailure(ApiErrors.messageFrom(response));
                            return;
                        }

                        List<EnergySlot> slots = new ArrayList<>(body.size());
                        for (SlotDto dto : body) {
                            slots.add(EnergySlot.from(dto));
                        }
                        callback.onSuccess(slots);
                    }

                    @Override
                    public void onFailure(@NonNull Call<List<SlotDto>> call, @NonNull Throwable t) {
                        callback.onFailure(offlineMessage());
                    }
                });
    }

    /**
     * Every booking belonging to one prosumer.
     *
     * The server also limits a prosumer to their own bookings, so the NIC
     * here only narrows what is already allowed.
     */
    public void loadMyBookings(@NonNull String nic,
                               @NonNull Callback<List<Reservation>> callback) {

        bookingApi.searchReservations(nic).enqueue(new retrofit2.Callback<List<ReservationDto>>() {

            @Override
            public void onResponse(@NonNull Call<List<ReservationDto>> call,
                                   @NonNull Response<List<ReservationDto>> response) {

                List<ReservationDto> body = response.body();

                if (!response.isSuccessful() || body == null) {
                    callback.onFailure(ApiErrors.messageFrom(response));
                    return;
                }

                List<Reservation> bookings = new ArrayList<>(body.size());
                for (ReservationDto dto : body) {
                    bookings.add(Reservation.from(dto));
                }
                callback.onSuccess(bookings);
            }

            @Override
            public void onFailure(@NonNull Call<List<ReservationDto>> call, @NonNull Throwable t) {
                callback.onFailure(offlineMessage());
            }
        });
    }

    /** One booking, fresh from the server. */
    public void loadBooking(@NonNull String id, @NonNull Callback<Reservation> callback) {
        bookingApi.getReservation(id).enqueue(bookingCallback(callback));
    }

    /**
     * Books a window. Task D2.
     *
     * A 409 here means somebody else took the last place first. That is
     * business rule BR-9, and the server settles it. The screen shows the
     * message the server sends.
     */
    public void createBooking(@NonNull String slotId,
                              double energyKwh,
                              @NonNull String direction,
                              @NonNull Callback<Reservation> callback) {

        bookingApi.createReservation(new CreateReservationRequest(slotId, energyKwh, direction))
                .enqueue(bookingCallback(callback));
    }

    /**
     * Changes a booking. Task D3, business rule BR-2.
     *
     * @param slotId a new window, or null to keep the window it already has.
     */
    public void updateBooking(@NonNull String reservationId,
                              @Nullable String slotId,
                              double energyKwh,
                              @NonNull String direction,
                              @NonNull Callback<Reservation> callback) {

        bookingApi.updateReservation(reservationId,
                        new UpdateReservationRequest(slotId, energyKwh, direction))
                .enqueue(bookingCallback(callback));
    }

    /**
     * Cancels a booking. Task D4, business rule BR-3.
     */
    public void cancelBooking(@NonNull String reservationId,
                              @NonNull String reason,
                              @NonNull Callback<Reservation> callback) {

        bookingApi.cancelReservation(reservationId, new CancelReservationRequest(reason))
                .enqueue(bookingCallback(callback));
    }

    /**
     * The QR code of an approved booking. Task E3.
     *
     * The server refuses with a 409 while the booking is still Pending. A
     * code is only issued once a booking is approved, which is BR-7.
     */
    public void loadQrToken(@NonNull String reservationId,
                            @NonNull Callback<QrToken> callback) {

        bookingApi.getQrToken(reservationId).enqueue(new retrofit2.Callback<QrTokenDto>() {

            @Override
            public void onResponse(@NonNull Call<QrTokenDto> call,
                                   @NonNull Response<QrTokenDto> response) {

                QrTokenDto body = response.body();

                if (!response.isSuccessful() || body == null) {
                    callback.onFailure(ApiErrors.messageFrom(response));
                    return;
                }
                callback.onSuccess(QrToken.from(body));
            }

            @Override
            public void onFailure(@NonNull Call<QrTokenDto> call, @NonNull Throwable t) {
                callback.onFailure(offlineMessage());
            }
        });
    }

    /**
     * Checks a scanned QR code. Task E4.
     *
     * The scanned text is sent exactly as the camera read it. The app does
     * not look inside it. The server decides whether it belongs to a real
     * booking, and whether that booking can still be used.
     */
    public void verifyQr(@NonNull String scannedToken,
                         @NonNull Callback<Reservation> callback) {

        bookingApi.verifyQr(new VerifyQrRequest(scannedToken))
                .enqueue(bookingCallback(callback));
    }

    /** Marks the energy transfer as done. Task E5. */
    public void completeBooking(@NonNull String reservationId,
                                @NonNull Callback<Reservation> callback) {

        bookingApi.completeReservation(reservationId).enqueue(bookingCallback(callback));
    }

    /**
     * Four calls return one booking. All four do the same thing with it.
     * Written once here instead of four times.
     */
    @NonNull
    private retrofit2.Callback<ReservationDto> bookingCallback(
            @NonNull Callback<Reservation> callback) {

        return new retrofit2.Callback<ReservationDto>() {

            @Override
            public void onResponse(@NonNull Call<ReservationDto> call,
                                   @NonNull Response<ReservationDto> response) {

                ReservationDto body = response.body();

                if (!response.isSuccessful() || body == null) {
                    callback.onFailure(ApiErrors.messageFrom(response));
                    return;
                }
                callback.onSuccess(Reservation.from(body));
            }

            @Override
            public void onFailure(@NonNull Call<ReservationDto> call, @NonNull Throwable t) {
                callback.onFailure(offlineMessage());
            }
        };
    }

    @NonNull
    private static String offlineMessage() {
        return SolarApp.get().getString(R.string.error_no_connection);
    }
}
