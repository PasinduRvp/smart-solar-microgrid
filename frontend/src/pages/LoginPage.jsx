/*
 * ---------------------------------------------------------------------------
 * File        : LoginPage.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : The sign in screen for Backoffice officers and Grid Operators.
 *
 * Error handling
 *               Whatever the API says is shown unchanged. When an account is
 *               awaiting activation the service answers 403 with "Your account
 *               is awaiting activation by a Backoffice officer", and that exact
 *               sentence appears on screen. This page contains no rule about
 *               who may sign in; it asks and reports.
 *
 * Validation   : Only "is the box empty" is checked here, which saves a
 *               pointless round trip. Whether the credentials are correct, and
 *               whether the account is allowed in, are decisions only the
 *               service can make — the browser has no password hash to compare
 *               against, and any check placed here could be edited by the user.
 *
 * Small touches that matter
 *               - Errors clear as soon as the user starts correcting them,
 *                 rather than sitting there contradicting what is now typed.
 *               - The password can be revealed, because a hidden password on a
 *                 phone keyboard is a common cause of failed sign ins.
 *               - Field level messages sit under the field they concern, so
 *                 the user does not have to work out which box is wrong.
 * ---------------------------------------------------------------------------
 */

import { useState } from 'react';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { ROLES } from '../config';
import AuthAside, { SIGN_IN_POINTS } from '../components/AuthAside';
import { IconAlert, IconSun } from '../components/Icons';

export default function LoginPage() {
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);

  const [fieldErrors, setFieldErrors] = useState({});
  const [formError, setFormError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { signIn } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();

  // Set by the API client when a token expires mid-session.
  const sessionExpired = searchParams.get('expired') === '1';

  /** Clears the message for a field as soon as the user corrects it. */
  function updateField(setter, field) {
    return (event) => {
      setter(event.target.value);
      setFormError('');
      setFieldErrors((current) => {
        if (!current[field]) return current;
        const next = { ...current };
        delete next[field];
        return next;
      });
    };
  }

  function validate() {
    const errors = {};
    if (!identifier.trim()) errors.identifier = 'Enter your email address.';
    if (!password) errors.password = 'Enter your password.';
    return errors;
  }

  async function handleSubmit(event) {
    event.preventDefault();

    const errors = validate();
    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      return;
    }

    setFieldErrors({});
    setFormError('');
    setIsSubmitting(true);

    try {
      const session = await signIn(identifier.trim(), password);

      // A prosumer account belongs on the mobile application. Signing them in
      // here would only show screens the API refuses them.
      if (session.role === ROLES.PROSUMER) {
        setFormError('Prosumer accounts sign in through the mobile application.');
        setIsSubmitting(false);
        return;
      }

      // Return them to wherever they were heading before being asked to sign in.
      const destination = location.state?.from?.pathname ?? '/dashboard';
      navigate(destination, { replace: true });
    } catch (submitError) {
      // The message comes from the API and is already written for the user.
      setFormError(submitError.message);
      setIsSubmitting(false);
    }
  }

  return (
    <div className="auth-page">
      <AuthAside
        heading="Manage the community solar grid from one place."
        points={SIGN_IN_POINTS}
      />

      <div className="auth-main">
        <div className="auth-form-wrap">
          {/* Shown only on phones, where the dark panel is hidden. */}
          <Link to="/" className="auth-mobile-brand text-decoration-none">
            <IconSun size={22} />
            <span className="fw-semibold">Smart Solar Microgrid</span>
          </Link>

          <div className="mb-4">
            <h1 className="h3 fw-semibold mb-1">Sign in</h1>
            <p className="text-body-secondary mb-0" style={{ fontSize: '0.9rem' }}>
              For Backoffice officers and Grid Operators.
            </p>
          </div>

          {sessionExpired && !formError && (
            <div className="alert alert-warning d-flex gap-2 py-2 small" role="alert">
              <IconAlert />
              <span>Your session has expired. Please sign in again.</span>
            </div>
          )}

          {formError && (
            <div className="alert alert-danger d-flex gap-2 py-2 small" role="alert">
              <IconAlert />
              <span>{formError}</span>
            </div>
          )}

          <form onSubmit={handleSubmit} noValidate>
            <div className="mb-3">
              <label htmlFor="identifier" className="form-label small fw-semibold">
                Email address
              </label>
              <input
                id="identifier"
                type="text"
                className={`form-control${fieldErrors.identifier ? ' is-invalid' : ''}`}
                placeholder="you@microgrid.lk"
                value={identifier}
                onChange={updateField(setIdentifier, 'identifier')}
                autoComplete="username"
                autoFocus
              />
              {fieldErrors.identifier && (
                <div className="auth-field-error">{fieldErrors.identifier}</div>
              )}
            </div>

            <div className="mb-4">
              <label htmlFor="password" className="form-label small fw-semibold">
                Password
              </label>

              <div className="auth-password-wrap">
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  className={`form-control${fieldErrors.password ? ' is-invalid' : ''}`}
                  value={password}
                  onChange={updateField(setPassword, 'password')}
                  autoComplete="current-password"
                />
                <button
                  type="button"
                  className="auth-password-toggle"
                  onClick={() => setShowPassword((shown) => !shown)}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                  tabIndex={-1}
                >
                  {showPassword ? <EyeOffIcon /> : <EyeIcon />}
                </button>
              </div>

              {fieldErrors.password && (
                <div className="auth-field-error">{fieldErrors.password}</div>
              )}
            </div>

            <button
              type="submit"
              className="btn btn-primary w-100 py-2"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />
                  Signing in...
                </>
              ) : (
                'Sign in'
              )}
            </button>
          </form>

          <hr className="my-4" />

          <p className="text-body-secondary small text-center mb-2">
            Are you a solar prosumer?{' '}
            <Link to="/register" className="fw-semibold text-decoration-none">
              Register here
            </Link>
          </p>
          <p className="text-body-secondary text-center mb-0" style={{ fontSize: '0.78rem' }}>
            <Link to="/" className="text-decoration-none">
              Back to home
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}

/* --- The two states of the reveal button -------------------------------- */

function EyeIcon() {
  return (
    <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7z" />
      <circle cx="12" cy="12" r="3" />
    </svg>
  );
}

function EyeOffIcon() {
  return (
    <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M10.6 6.2A9.9 9.9 0 0 1 12 6c6.5 0 10 7 10 7a17 17 0 0 1-3 3.8M6.3 6.4A17 17 0 0 0 2 13s3.5 7 10 7a9.6 9.6 0 0 0 4.3-1" />
      <path d="M9.9 9.9a3 3 0 0 0 4.2 4.2M3 3l18 18" />
    </svg>
  );
}
