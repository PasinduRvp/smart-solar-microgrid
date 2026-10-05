/*
 * ---------------------------------------------------------------------------
 * File        : ProfileRepository.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-20
 * Description : Reading and changing the account of the signed in user.
 *               Tasks B4 and B5.
 *
 * Cache first, then refresh
 *               load() answers twice when it can. Once at once from SQLite,
 *               so the screen is filled the moment it opens. Then again
 *               when the server replies.
 *               Waiting for the network before drawing anything is what
 *               makes an app feel slow. With no signal it would show
 *               nothing at all.
 *
 * Every write updates the cache
 *               The API returns the saved profile. That is what gets
 *               stored, not what was typed. If the server trimmed or fixed
 *               a value, the phone holds what was really saved.
 *
 * SOLID        Single Responsibility. Profile calls and their caching.
 * SOLID        Dependency Inversion. The API and the store are passed in.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.repository;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.SolarApp;
import lk.sliit.solarmicrogrid.data.local.ProfileStore;
import lk.sliit.solarmicrogrid.data.remote.ApiClient;
import lk.sliit.solarmicrogrid.data.remote.ApiErrors;
import lk.sliit.solarmicrogrid.data.remote.ProfileApi;
import lk.sliit.solarmicrogrid.data.remote.dto.ChangePasswordRequest;
import lk.sliit.solarmicrogrid.data.remote.dto.UpdateProfileRequest;
import lk.sliit.solarmicrogrid.data.remote.dto.UserProfileDto;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository.Callback;
import lk.sliit.solarmicrogrid.model.UserProfile;
import retrofit2.Call;
import retrofit2.Response;

public final class ProfileRepository {

    private final ProfileApi profileApi;
    private final ProfileStore profileStore;

    public ProfileRepository(@NonNull ProfileApi profileApi, @NonNull ProfileStore profileStore) {
        this.profileApi = profileApi;
        this.profileStore = profileStore;
    }

    @NonNull
    public static ProfileRepository create() {
        return new ProfileRepository(
                ApiClient.create(ProfileApi.class),
                SolarApp.get().profiles());
    }

    /**
     * The cached profile. Null if this account was never loaded on this
     * phone. Returns at once. No network.
     */
    @Nullable
    public UserProfile cached(@NonNull String nic) {
        return profileStore.read(nic);
    }

    /**
     * Fetches the profile from the server and caches it.
     */
    public void load(@NonNull Callback<UserProfile> callback) {

        profileApi.getMyProfile().enqueue(new retrofit2.Callback<UserProfileDto>() {

            @Override
            public void onResponse(@NonNull Call<UserProfileDto> call,
                                   @NonNull Response<UserProfileDto> response) {
                completeWithProfile(response, callback);
            }

            @Override
            public void onFailure(@NonNull Call<UserProfileDto> call, @NonNull Throwable t) {
                callback.onFailure(offlineMessage());
            }
        });
    }

    /**
     * Saves the details the user owns.
     */
    public void save(@NonNull UpdateProfileRequest request,
                     @NonNull Callback<UserProfile> callback) {

        profileApi.updateMyProfile(request).enqueue(new retrofit2.Callback<UserProfileDto>() {

            @Override
            public void onResponse(@NonNull Call<UserProfileDto> call,
                                   @NonNull Response<UserProfileDto> response) {
                completeWithProfile(response, callback);
            }

            @Override
            public void onFailure(@NonNull Call<UserProfileDto> call, @NonNull Throwable t) {
                callback.onFailure(offlineMessage());
            }
        });
    }

    /**
     * Changes the password.
     *
     * The session is left alone on purpose. The old token stays valid until
     * it expires. So the user is not thrown out of the app at the moment they
     * succeed at something.
     */
    public void changePassword(@NonNull String currentPassword,
                               @NonNull String newPassword,
                               @NonNull Callback<Void> callback) {

        profileApi.changeMyPassword(new ChangePasswordRequest(currentPassword, newPassword))
                .enqueue(new retrofit2.Callback<Boolean>() {

                    @Override
                    public void onResponse(@NonNull Call<Boolean> call,
                                           @NonNull Response<Boolean> response) {
                        if (!response.isSuccessful()) {
                            callback.onFailure(ApiErrors.messageFrom(response));
                            return;
                        }
                        callback.onSuccess(null);
                    }

                    @Override
                    public void onFailure(@NonNull Call<Boolean> call, @NonNull Throwable t) {
                        callback.onFailure(offlineMessage());
                    }
                });
    }

    /**
     * Asks a Backoffice officer to deactivate this account. BR-6.
     *
     * The reply has deactivationRequested set to true. The screen uses that
     * to switch the button off.
     */
    public void requestDeactivation(@NonNull String nic,
                                    @NonNull Callback<UserProfile> callback) {

        profileApi.requestDeactivation(nic).enqueue(new retrofit2.Callback<UserProfileDto>() {

            @Override
            public void onResponse(@NonNull Call<UserProfileDto> call,
                                   @NonNull Response<UserProfileDto> response) {
                completeWithProfile(response, callback);
            }

            @Override
            public void onFailure(@NonNull Call<UserProfileDto> call, @NonNull Throwable t) {
                callback.onFailure(offlineMessage());
            }
        });
    }

    /**
     * Four calls return a profile. All four do the same three steps.
     * Check it worked. Cache what came back. Give it to the screen.
     *
     * Written once here instead of four times.
     */
    private void completeWithProfile(@NonNull Response<UserProfileDto> response,
                                     @NonNull Callback<UserProfile> callback) {

        UserProfileDto body = response.body();

        if (!response.isSuccessful() || body == null) {
            callback.onFailure(ApiErrors.messageFrom(response));
            return;
        }

        UserProfile profile = UserProfile.from(body);
        profileStore.save(profile);
        callback.onSuccess(profile);
    }

    @NonNull
    private static String offlineMessage() {
        return SolarApp.get().getString(R.string.error_no_connection);
    }
}
