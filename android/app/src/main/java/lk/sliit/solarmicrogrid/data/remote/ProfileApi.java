/*
 * ---------------------------------------------------------------------------
 * File        : ProfileApi.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-20
 * Description : What a signed in user can do with their own account.
 *
 * Security     None of these paths has an account id in it.
 *               The server works out whose profile it is from the token.
 *               AuthInterceptor puts that token on the request.
 *               So a user cannot read or change another user by editing a
 *               value in the app. There is no value to edit.
 *
 *               The deactivation request is the one exception. It takes an
 *               NIC, because it sits on the Prosumers controller. The server
 *               allows it only for the Prosumer role, and only for that
 *               prosumer's own record.
 *
 * SOLID        Interface Segregation. Account calls only. Sign in lives in
 *               AuthApi.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote;

import lk.sliit.solarmicrogrid.data.remote.dto.ChangePasswordRequest;
import lk.sliit.solarmicrogrid.data.remote.dto.UpdateProfileRequest;
import lk.sliit.solarmicrogrid.data.remote.dto.UserProfileDto;
import retrofit2.Call;
import retrofit2.http.Body;
import retrofit2.http.GET;
import retrofit2.http.PATCH;
import retrofit2.http.PUT;
import retrofit2.http.Path;

public interface ProfileApi {

    /** Returns the profile of the signed in user. */
    @GET("api/profile")
    Call<UserProfileDto> getMyProfile();

    /** Updates the details of the signed in user. Returns what was saved. */
    @PUT("api/profile")
    Call<UserProfileDto> updateMyProfile(@Body UpdateProfileRequest request);

    /**
     * Changes the password of the signed in user.
     *
     * Returns 400 when the current password is wrong. The user sees the
     * message the server sends.
     */
    @PUT("api/profile/password")
    Call<Boolean> changeMyPassword(@Body ChangePasswordRequest request);

    /**
     * Records that this prosumer asked for their account to be closed. BR-6.
     *
     * This call does not close the account. It sets a flag. A Backoffice
     * officer acts on it. Only a Backoffice officer can open the account
     * again afterwards.
     */
    @PATCH("api/prosumers/{nic}/request-deactivation")
    Call<UserProfileDto> requestDeactivation(@Path("nic") String nic);
}
