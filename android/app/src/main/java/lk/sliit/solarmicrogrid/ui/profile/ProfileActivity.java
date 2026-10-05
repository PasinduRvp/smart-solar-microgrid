/*
 * ---------------------------------------------------------------------------
 * File        : ProfileActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-20
 * Description : The user's own profile: view it, edit it, change the
 *               password, and ask for the account to be deactivated.
 *               Tasks B4 and B5.
 *
 * Whose profile is this
 *               Always the signed in user's. The screen takes no NIC and no
 *               account id from anywhere, and neither do the endpoints it
 *               calls. A Backoffice officer cannot reach another person's
 *               details through this screen because there is nothing to point
 *               it at - which is the same rule the web application follows.
 *
 * Read only until asked
 *               The screen opens showing the details as text. The pencil in
 *               the toolbar switches to input fields, and Cancel puts back
 *               what was last saved. A profile permanently displayed inside
 *               text boxes gives the user no way to tell whether what they
 *               are looking at has been saved or not.
 *
 * Draw first, refresh second
 *               The cached profile fills the screen immediately, then the
 *               server copy replaces it when it arrives. On a dropped
 *               connection the user still sees their details rather than an
 *               empty screen and an error.
 *
 * Business rule BR-6
 *               Requesting deactivation does not close the account. It raises
 *               a flag a Backoffice officer acts on, and only they can
 *               reactivate afterwards. The screen says so before the button
 *               and again in the confirmation, because the action cannot be
 *               undone by the person taking it.
 *
 * SOLID        Single Responsibility - it collects input and displays results.
 *               Every decision about what is allowed belongs to the API.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.profile;

import android.os.Bundle;
import android.view.View;
import android.widget.Toast;

import androidx.activity.OnBackPressedCallback;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.app.AlertDialog;

import com.google.android.material.textfield.TextInputLayout;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.SolarApp;
import lk.sliit.solarmicrogrid.data.remote.dto.UpdateProfileRequest;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository;
import lk.sliit.solarmicrogrid.data.repository.ProfileRepository;
import lk.sliit.solarmicrogrid.databinding.ActivityProfileBinding;
import lk.sliit.solarmicrogrid.model.Roles;
import lk.sliit.solarmicrogrid.model.Session;
import lk.sliit.solarmicrogrid.model.UserProfile;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.ui.Navigation;
import lk.sliit.solarmicrogrid.util.TextFormat;
import lk.sliit.solarmicrogrid.util.Validators;

public final class ProfileActivity extends BaseActivity {

    private ActivityProfileBinding binding;
    private ProfileRepository profileRepository;
    private Session session;

    /** The profile as it was last saved. Cancel puts these values back. */
    @Nullable
    private UserProfile currentProfile;

    /** True while the input fields are on screen. */
    private boolean editing;

    /** True for a Prosumer. Only a Prosumer has solar panels. */
    private boolean isProsumer;

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityProfileBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        session = SolarApp.get().sessions().read();
        if (session == null) {
            Navigation.toLogin(this);
            return;
        }

        isProsumer = Roles.isProsumer(session.getRole());
        profileRepository = ProfileRepository.create();

        setUpToolbar();
        setUpButtons();
        setUpBackHandling();

        // Only a Prosumer may ask for deactivation. The endpoint allows
        // that role only. So an operator is not shown a button that would
        // always be refused.
        binding.groupDeactivation.setVisibility(isProsumer ? View.VISIBLE : View.GONE);

        // A Grid Operator has no solar panels. The capacity would always
        // read 0 kW for them, so it is hidden.
        binding.groupViewCapacity.setVisibility(isProsumer ? View.VISIBLE : View.GONE);
        binding.tilCapacity.setVisibility(isProsumer ? View.VISIBLE : View.GONE);

        setEditing(false);
        showCachedProfileIfAny();
        refreshFromServer();
    }

    private void setUpToolbar() {
        binding.toolbar.inflateMenu(R.menu.menu_profile);
        binding.toolbar.setNavigationOnClickListener(view -> leaveScreen());
        binding.toolbar.setOnMenuItemClickListener(item -> {
            if (item.getItemId() == R.id.action_edit) {
                setEditing(true);
                return true;
            }
            return false;
        });
    }

    private void setUpButtons() {
        binding.btnSave.setOnClickListener(view -> attemptSave());
        binding.btnCancel.setOnClickListener(view -> cancelEditing());
        binding.btnChangePassword.setOnClickListener(view -> attemptChangePassword());
        binding.btnRequestDeactivation.setOnClickListener(view -> confirmDeactivation());
    }

    /**
     * Back leaves the edit boxes first. It closes the screen on a second
     * press.
     *
     * Without this, one stray Back while editing would throw away everything
     * typed. There would be no warning and no way to get it back.
     */
    private void setUpBackHandling() {
        getOnBackPressedDispatcher().addCallback(this, new OnBackPressedCallback(true) {
            @Override
            public void handleOnBackPressed() {
                leaveScreen();
            }
        });
    }

    private void leaveScreen() {
        if (editing) {
            cancelEditing();
            return;
        }
        finish();
    }

    // ----- Switching between reading and editing ---------------------------

    /**
     * Shows either the text rows or the input boxes. Never both.
     *
     * The pencil is hidden while editing. Cancel and Save are on screen then.
     * A third way out would only be one more thing to read.
     */
    private void setEditing(boolean nowEditing) {

        editing = nowEditing;

        binding.groupDetailsView.setVisibility(nowEditing ? View.GONE : View.VISIBLE);
        binding.groupDetailsEdit.setVisibility(nowEditing ? View.VISIBLE : View.GONE);

        binding.toolbar.getMenu()
                .findItem(R.id.action_edit)
                .setVisible(!nowEditing);

        if (nowEditing) {
            fillEditFields();
        }
    }

    /** Puts the last saved values back, and returns to reading. */
    private void cancelEditing() {
        clearFieldErrors();
        setEditing(false);
    }

    // ----- Loading ---------------------------------------------------------

    /** Fills the screen from SQLite. It is then never blank while loading. */
    private void showCachedProfileIfAny() {
        UserProfile cached = profileRepository.cached(session.getNic());
        if (cached != null) {
            showProfile(cached);
        }
    }

    private void refreshFromServer() {

        showBusy(true);

        profileRepository.load(new AuthRepository.Callback<UserProfile>() {

            @Override
            public void onSuccess(@Nullable UserProfile profile) {
                if (isGone() || profile == null) {
                    return;
                }
                showBusy(false);
                showProfile(profile);
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isGone()) {
                    return;
                }
                showBusy(false);

                // A failed refresh is not serious when the cache has
                // already filled the screen. So it is reported quietly.
                Toast.makeText(ProfileActivity.this, message, Toast.LENGTH_LONG).show();
            }
        });
    }

    private void showProfile(@NonNull UserProfile profile) {

        currentProfile = profile;

        binding.tvAvatar.setText(TextFormat.initialsOf(profile.getFullName()));
        binding.tvHeaderName.setText(profile.getFullName());
        binding.tvHeaderEmail.setText(profile.getEmail());

        binding.tvNic.setText(profile.getNic());
        binding.tvRole.setText(profile.getRole());
        binding.tvStatus.setText(profile.getStatus());

        binding.tvViewFullName.setText(orNotSet(profile.getFullName()));
        binding.tvViewEmail.setText(orNotSet(profile.getEmail()));
        binding.tvViewPhone.setText(orNotSet(profile.getPhone()));
        binding.tvViewAddress.setText(orNotSet(profile.getAddress()));
        binding.tvViewCapacity.setText(getString(
                R.string.profile_capacity_value,
                TextFormat.capacity(profile.getSolarCapacityKw())));

        // The input boxes are NOT refilled while the user is editing.
        // A refresh arriving in the middle would wipe out what was typed.
        if (!editing) {
            fillEditFields();
        }

        showDeactivationState(profile);
    }

    private void fillEditFields() {

        if (currentProfile == null) {
            return;
        }

        setText(binding.tilFullName, currentProfile.getFullName());
        setText(binding.tilEmail, currentProfile.getEmail());
        setText(binding.tilPhone, currentProfile.getPhone());
        setText(binding.tilAddress, currentProfile.getAddress());
        setText(binding.tilCapacity, TextFormat.capacity(currentProfile.getSolarCapacityKw()));

        clearFieldErrors();
    }

    /**
     * A request can only be made once.
     *
     * So the button is switched off, and the words change to say what is
     * happening. Leaving it on would invite a second tap and a confusing
     * error.
     */
    private void showDeactivationState(@NonNull UserProfile profile) {
        if (profile.isDeactivationRequested()) {
            binding.btnRequestDeactivation.setEnabled(false);
            binding.tvDeactivationNote.setText(R.string.profile_deactivation_already);
        } else {
            binding.btnRequestDeactivation.setEnabled(true);
            binding.tvDeactivationNote.setText(R.string.profile_deactivation_explain);
        }
    }

    // ----- Saving details --------------------------------------------------

    private void attemptSave() {

        if (!areDetailsValid()) {
            return;
        }

        UpdateProfileRequest request = new UpdateProfileRequest(
                textOf(binding.tilFullName),
                textOf(binding.tilEmail),
                textOf(binding.tilPhone),
                textOf(binding.tilAddress),
                capacityToSave());

        showBusy(true);

        profileRepository.save(request, new AuthRepository.Callback<UserProfile>() {

            @Override
            public void onSuccess(@Nullable UserProfile saved) {
                if (isGone() || saved == null) {
                    return;
                }
                showBusy(false);

                // Go back to reading first. showProfile then refills the
                // boxes from what the server really saved, not from what was
                // typed.
                setEditing(false);
                showProfile(saved);

                Toast.makeText(ProfileActivity.this, R.string.profile_saved, Toast.LENGTH_LONG).show();
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isGone()) {
                    return;
                }
                showBusy(false);

                // Stay in edit mode. The user can then fix what was
                // refused, instead of tapping the pencil again.
                Toast.makeText(ProfileActivity.this, message, Toast.LENGTH_LONG).show();
            }
        });
    }

    /**
     * The capacity to send with an update.
     *
     * The box is hidden for a Grid Operator. Reading it would send 0 and
     * wipe what is stored. So their saved value is kept instead.
     */
    private double capacityToSave() {

        if (isProsumer) {
            return valueOrZero(textOf(binding.tilCapacity));
        }
        if (currentProfile == null) {
            return 0d;
        }
        return currentProfile.getSolarCapacityKw();
    }

    /**
     * Checks every field. Marks all the bad ones at once.
     * It does not stop at the first one and make the user submit again.
     */
    private boolean areDetailsValid() {

        boolean valid = true;

        if (!Validators.isNotBlank(textOf(binding.tilFullName))) {
            binding.tilFullName.setError(getString(R.string.register_error_full_name));
            valid = false;
        } else {
            binding.tilFullName.setError(null);
        }

        if (!Validators.isValidEmail(textOf(binding.tilEmail))) {
            binding.tilEmail.setError(getString(R.string.register_error_email));
            valid = false;
        } else {
            binding.tilEmail.setError(null);
        }

        if (!Validators.isValidPhone(textOf(binding.tilPhone))) {
            binding.tilPhone.setError(getString(R.string.register_error_phone));
            valid = false;
        } else {
            binding.tilPhone.setError(null);
        }

        // Capacity is only checked when it is on screen. The update
        // endpoint allows 0, unlike registration. So an empty box is fine.
        if (isProsumer
                && Validators.isNotBlank(textOf(binding.tilCapacity))
                && Validators.parseDoubleOrNull(textOf(binding.tilCapacity)) == null) {
            binding.tilCapacity.setError(getString(R.string.register_error_capacity));
            valid = false;
        } else {
            binding.tilCapacity.setError(null);
        }

        return valid;
    }

    private void clearFieldErrors() {
        binding.tilFullName.setError(null);
        binding.tilEmail.setError(null);
        binding.tilPhone.setError(null);
        binding.tilAddress.setError(null);
        binding.tilCapacity.setError(null);
    }

    // ----- Changing the password -------------------------------------------

    private void attemptChangePassword() {

        String current = rawTextOf(binding.tilCurrentPassword);
        String updated = rawTextOf(binding.tilNewPassword);

        boolean valid = true;

        if (!Validators.isNotBlank(current)) {
            binding.tilCurrentPassword.setError(getString(R.string.profile_error_current_password));
            valid = false;
        } else {
            binding.tilCurrentPassword.setError(null);
        }

        if (!Validators.isValidPassword(updated)) {
            binding.tilNewPassword.setError(getString(R.string.profile_error_new_password));
            valid = false;
        } else {
            binding.tilNewPassword.setError(null);
        }

        if (!valid) {
            return;
        }

        showBusy(true);

        profileRepository.changePassword(current, updated, new AuthRepository.Callback<Void>() {

            @Override
            public void onSuccess(@Nullable Void nothing) {
                if (isGone()) {
                    return;
                }
                showBusy(false);

                // Emptied so the old and new passwords are not left on
                // screen behind the user.
                setText(binding.tilCurrentPassword, "");
                setText(binding.tilNewPassword, "");

                Toast.makeText(ProfileActivity.this,
                        R.string.profile_password_changed, Toast.LENGTH_LONG).show();
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isGone()) {
                    return;
                }
                showBusy(false);
                Toast.makeText(ProfileActivity.this, message, Toast.LENGTH_LONG).show();
            }
        });
    }

    // ----- Requesting deactivation (BR-6) ----------------------------------

    private void confirmDeactivation() {
        new AlertDialog.Builder(this)
                .setTitle(R.string.profile_deactivation_confirm_title)
                .setMessage(R.string.profile_deactivation_confirm_message)
                .setNegativeButton(R.string.action_cancel, null)
                .setPositiveButton(R.string.profile_deactivation_confirm_action,
                        (dialog, which) -> requestDeactivation())
                .show();
    }

    private void requestDeactivation() {

        showBusy(true);

        profileRepository.requestDeactivation(session.getNic(),
                new AuthRepository.Callback<UserProfile>() {

                    @Override
                    public void onSuccess(@Nullable UserProfile profile) {
                        if (isGone() || profile == null) {
                            return;
                        }
                        showBusy(false);
                        showProfile(profile);
                        Toast.makeText(ProfileActivity.this,
                                R.string.profile_deactivation_sent, Toast.LENGTH_LONG).show();
                    }

                    @Override
                    public void onFailure(@NonNull String message) {
                        if (isGone()) {
                            return;
                        }
                        showBusy(false);
                        Toast.makeText(ProfileActivity.this, message, Toast.LENGTH_LONG).show();
                    }
                });
    }

    // ----- Shared helpers ---------------------------------------------------

    /**
     * Switches every action off while a call is running.
     * Two requests cannot then be started at the same time.
     */
    private void showBusy(boolean busy) {
        binding.progress.setVisibility(busy ? View.VISIBLE : View.GONE);
        binding.btnSave.setEnabled(!busy);
        binding.btnCancel.setEnabled(!busy);
        binding.btnChangePassword.setEnabled(!busy);

        // Only switched back on if the account has not asked already.
        boolean alreadyRequested =
                currentProfile != null && currentProfile.isDeactivationRequested();
        binding.btnRequestDeactivation.setEnabled(!busy && !alreadyRequested);
    }

    /**
     * True once this screen can no longer be touched. The user pressed Back,
     * or the screen was destroyed.
     *
     * A late reply must then be dropped. Touching a view that is gone would
     * crash.
     */
    private boolean isGone() {
        return isFinishing() || isDestroyed();
    }

    /** Shows Not set instead of a blank line. A blank line looks broken. */
    @NonNull
    private String orNotSet(@Nullable String value) {
        return Validators.isNotBlank(value) ? value : getString(R.string.profile_value_not_set);
    }

    private static void setText(@NonNull TextInputLayout field, @NonNull String value) {
        if (field.getEditText() != null) {
            field.getEditText().setText(value);
        }
    }

    @NonNull
    private static String textOf(@NonNull TextInputLayout field) {
        return rawTextOf(field).trim();
    }

    /** Not trimmed. A space can be a real part of a password. */
    @NonNull
    private static String rawTextOf(@NonNull TextInputLayout field) {
        return field.getEditText() == null
                ? ""
                : field.getEditText().getText().toString();
    }

    private static double valueOrZero(@Nullable String value) {
        Double parsed = Validators.parseDoubleOrNull(value);
        return parsed == null ? 0d : parsed;
    }
}
