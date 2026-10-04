/*
 * ---------------------------------------------------------------------------
 * File        : LoginActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : M T A J Yapa (IT 23278530)
 * Created     : 2026-09-20
 * Description : The sign in screen. Task B2.
 *
 * What it does
 *               Takes an email address or NIC and a password. Asks
 *               AuthRepository to sign in. On success it lets Navigation
 *               pick which home screen to open.
 *
 * What it does NOT do
 *               It never decides whether the details are correct. It never
 *               decides whether an account may sign in. A deactivated
 *               account is refused by the API with a 403 and its own
 *               message. This screen just shows that message.
 *               All such rules live in the Web API, as the assignment asks.
 *
 * SOLID        Single Responsibility. Take input, show progress, show the
 *               result. No HTTP. No SQL.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui.auth;

import android.os.Bundle;
import android.text.Editable;
import android.text.TextUtils;
import android.text.TextWatcher;
import android.view.View;
import android.view.animation.DecelerateInterpolator;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.app.AlertDialog;

import com.google.android.material.textfield.TextInputLayout;

import lk.sliit.solarmicrogrid.R;
import lk.sliit.solarmicrogrid.SolarApp;
import lk.sliit.solarmicrogrid.data.repository.AuthRepository;
import lk.sliit.solarmicrogrid.databinding.ActivityLoginBinding;
import lk.sliit.solarmicrogrid.model.Session;
import lk.sliit.solarmicrogrid.ui.BaseActivity;
import lk.sliit.solarmicrogrid.ui.Navigation;

public final class LoginActivity extends BaseActivity {

    /** How small the logo starts before it grows to full size. */
    private static final float LOGO_ENTER_FROM = 0.85f;

    /** How long the logo takes to arrive, in milliseconds. */
    private static final long LOGO_ENTER_MS = 520L;

    /** A beat before it starts, so the screen has settled first. */
    private static final long LOGO_ENTER_DELAY_MS = 80L;

    private ActivityLoginBinding binding;
    private AuthRepository authRepository;

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityLoginBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());
        applySystemBarInsets(binding.root);

        playLogoEntrance();

        authRepository = AuthRepository.create();

        binding.btnSignIn.setOnClickListener(view -> attemptSignIn());
        binding.btnGoToRegister.setOnClickListener(view ->
                startActivity(new android.content.Intent(this, RegisterActivity.class)));

        // Clear the error as soon as the user starts fixing the field.
        // A red message under a box the user has already fixed is confusing.
        clearErrorWhileTyping(binding.tilIdentifier);
        clearErrorWhileTyping(binding.tilPassword);
    }

    /**
     * Fades and grows the logo in when the screen opens.
     *
     * The first screen of an app is a blank wall until something moves on
     * it. A short entrance gives the eye somewhere to land and makes the
     * app feel like it started rather than just appeared.
     *
     * It is kept small on purpose. 0.85 to full size over half a second,
     * slowing as it arrives. Anything longer and a user signing in for the
     * second time is waiting on decoration.
     *
     * Nothing depends on it finishing. The boxes and the button are usable
     * from the first frame.
     */
    private void playLogoEntrance() {

        binding.imgLogo.setAlpha(0f);
        binding.imgLogo.setScaleX(LOGO_ENTER_FROM);
        binding.imgLogo.setScaleY(LOGO_ENTER_FROM);

        binding.imgLogo.animate()
                .alpha(1f)
                .scaleX(1f)
                .scaleY(1f)
                .setStartDelay(LOGO_ENTER_DELAY_MS)
                .setDuration(LOGO_ENTER_MS)
                .setInterpolator(new DecelerateInterpolator())
                .start();
    }

    private void attemptSignIn() {

        String identifier = textOf(binding.tilIdentifier);
        String password = textOf(binding.tilPassword);

        // Only empty boxes are checked here. The server decides whether
        // the password is right. The identifier may be an email or an NIC,
        // so checking its format here would only get in the way.
        boolean valid = true;

        if (TextUtils.isEmpty(identifier)) {
            binding.tilIdentifier.setError(getString(R.string.login_error_identifier));
            valid = false;
        }
        if (TextUtils.isEmpty(password)) {
            binding.tilPassword.setError(getString(R.string.login_error_password));
            valid = false;
        }
        if (!valid) {
            return;
        }

        showBusy(true);

        authRepository.login(identifier, password, new AuthRepository.Callback<Session>() {

            @Override
            public void onSuccess(@Nullable Session session) {

                // The screen can close while the call is running. The user
                // may press Back, or turn the phone. Touching views after
                // that crashes. So the result is dropped.
                if (isFinishing() || isDestroyed() || session == null) {
                    return;
                }

                showBusy(false);

                // The assignment gives the mobile app to Prosumers and
                // Grid Operators. A Backoffice officer has a real account
                // but no screens here. So the session is thrown away.
                // Otherwise they would sit on a dashboard that does nothing.
                if (session.isBackoffice()) {
                    SolarApp.get().signOut();
                    showBackofficeNotice();
                    return;
                }

                Navigation.toHome(LoginActivity.this, session);
            }

            @Override
            public void onFailure(@NonNull String message) {
                if (isFinishing() || isDestroyed()) {
                    return;
                }
                showBusy(false);
                Toast.makeText(LoginActivity.this, message, Toast.LENGTH_LONG).show();
            }
        });
    }

    private void showBackofficeNotice() {
        new AlertDialog.Builder(this)
                .setMessage(R.string.home_backoffice_web_only)
                .setPositiveButton(android.R.string.ok, null)
                .show();
    }

    /**
     * Switches the button off while the request runs.
     * A second tap cannot then start a second login.
     */
    private void showBusy(boolean busy) {
        binding.progress.setVisibility(busy ? View.VISIBLE : View.GONE);
        binding.btnSignIn.setEnabled(!busy);
        binding.btnGoToRegister.setEnabled(!busy);
        binding.btnSignIn.setText(busy ? R.string.label_loading : R.string.action_sign_in);
    }

    @NonNull
    private static String textOf(@NonNull TextInputLayout field) {
        return field.getEditText() == null
                ? ""
                : field.getEditText().getText().toString().trim();
    }

    private static void clearErrorWhileTyping(@NonNull TextInputLayout field) {
        if (field.getEditText() == null) {
            return;
        }
        field.getEditText().addTextChangedListener(new TextWatcher() {

            @Override
            public void beforeTextChanged(CharSequence s, int start, int count, int after) {
                // Nothing to do before the change.
            }

            @Override
            public void onTextChanged(CharSequence s, int start, int before, int count) {
                field.setError(null);
            }

            @Override
            public void afterTextChanged(Editable s) {
                // Nothing to do after the change.
            }
        });
    }
}
