/*
 * ---------------------------------------------------------------------------
 * File        : RegisterActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-20
 * Description : Registration for a new Solar Prosumer. Task B1.
 *
 * NIC is the key
 *               The assignment says prosumers register with their NIC as
 *               the primary key. So it is the first field. The server
 *               refuses a second registration with the same NIC with a 409,
 *               and that message is shown as it arrives.
 *
 * What happens after success
 *               Nobody is signed in. The new account is Pending. A
 *               Backoffice officer must activate it. That is BR-6.
 *               The dialog says so plainly. A user who is not told this
 *               will think registration failed and try again.
 *
 * SOLID        Single Responsibility. Collect the form, check the formats,
 *               hand it to AuthRepository, report what happened.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.auth;

import android.os.Bundle;
import android.view.View;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.app.AlertDialog;

import com.google.android.material.textfield.TextInputLayout;

import java.util.Objects;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.data.remote.dto.RegisterProsumerRequest;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository;
import lk.sliit.solarmicrogrid.databinding.ActivityRegisterBinding;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.util.Validators;

public final class RegisterActivity extends BaseActivity {

    private ActivityRegisterBinding binding;
    private AuthRepository authRepository;

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityRegisterBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        authRepository = AuthRepository.create();

        binding.btnRegister.setOnClickListener(view -> attemptRegister());

        // finish(), not a new LoginActivity. This screen was opened from
        // the login screen, so closing goes back there. Starting a new one
        // would leave a second copy behind this.
        binding.btnGoToLogin.setOnClickListener(view -> finish());

        // The arrow and the text button do the same thing. A user at the
        // bottom of the form should not have to scroll back up.
        binding.toolbar.setNavigationOnClickListener(view -> finish());
    }

    private void attemptRegister() {

        if (!isFormValid()) {
            return;
        }

        RegisterProsumerRequest request = new RegisterProsumerRequest(
                textOf(binding.tilNic).toUpperCase(java.util.Locale.ROOT),
                textOf(binding.tilFullName),
                textOf(binding.tilEmail),
                textOf(binding.tilPhone),
                textOf(binding.tilAddress),
                Objects.requireNonNull(Validators.parseDoubleOrNull(textOf(binding.tilCapacity))),
                rawTextOf(binding.tilPassword));

        showBusy(true);

        authRepository.register(request, new AuthRepository.Callback<Void>() {

            @Override
            public void onSuccess(@Nullable Void nothing) {
                if (isFinishing() || isDestroyed()) {
                    return;
                }
                showBusy(false);
                showPendingActivationNotice();
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isFinishing() || isDestroyed()) {
                    return;
                }
                showBusy(false);
                Toast.makeText(RegisterActivity.this, message, Toast.LENGTH_LONG).show();
            }
        });
    }

    /**
     * Checks every field. Marks all the bad ones at once.
     *
     * It does not stop at the first problem, on purpose. A form that reports
     * one problem at a time makes the user submit again and again to find
     * the rest.
     */
    private boolean isFormValid() {

        boolean valid = true;

        if (!Validators.isValidNic(textOf(binding.tilNic))) {
            binding.tilNic.setError(getString(R.string.register_error_nic));
            valid = false;
        } else {
            binding.tilNic.setError(null);
        }

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

        if (!Validators.isNotBlank(textOf(binding.tilAddress))) {
            binding.tilAddress.setError(getString(R.string.register_error_address));
            valid = false;
        } else {
            binding.tilAddress.setError(null);
        }

        if (!Validators.isValidSolarCapacity(textOf(binding.tilCapacity))) {
            binding.tilCapacity.setError(getString(R.string.register_error_capacity));
            valid = false;
        } else {
            binding.tilCapacity.setError(null);
        }

        if (!Validators.isValidPassword(rawTextOf(binding.tilPassword))) {
            binding.tilPassword.setError(getString(R.string.register_error_password));
            valid = false;
        } else {
            binding.tilPassword.setError(null);
        }

        return valid;
    }

    /**
     * Explains the Pending state, then goes back to the sign in screen.
     *
     * setCancelable(false) so the message cannot be closed by tapping
     * outside it before it has been read.
     */
    private void showPendingActivationNotice() {
        new AlertDialog.Builder(this)
                .setTitle(R.string.register_success_title)
                .setMessage(R.string.register_success_message)
                .setCancelable(false)
                .setPositiveButton(android.R.string.ok, (dialog, which) -> finish())
                .show();
    }

    private void showBusy(boolean busy) {
        binding.progress.setVisibility(busy ? View.VISIBLE : View.GONE);
        binding.btnRegister.setEnabled(!busy);
        binding.btnGoToLogin.setEnabled(!busy);
        binding.btnRegister.setText(busy ? R.string.label_loading : R.string.action_register);
    }

    /** Trimmed. In these fields a space at either end is a typing slip. */
    @NonNull
    private static String textOf(@NonNull TextInputLayout field) {
        return rawTextOf(field).trim();
    }

    /**
     * Not trimmed. Used for the password only.
     *
     * A space can be a real part of a password. Removing one quietly would
     * make the account impossible to sign in to later.
     */
    @NonNull
    private static String rawTextOf(@NonNull TextInputLayout field) {
        return field.getEditText() == null
                ? ""
                : field.getEditText().getText().toString();
    }
}
