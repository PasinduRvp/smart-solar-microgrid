/*
 * ---------------------------------------------------------------------------
 * File        : RegisterPage.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-20
 * Description : Public self registration for solar prosumers.
 *
 * Why a web page as well as the mobile app
 *               Prosumers normally register on the Android application, but
 *               not everyone has an Android phone, and the assignment asks the
 *               web application to be able to create prosumer profiles too.
 *
 * Same endpoint, same rules
 *               This posts to the very same /auth/register the mobile
 *               application uses, so the account is created Pending and still
 *               needs a Backoffice officer to activate it. A separate web-only
 *               route that skipped that would mean two ways of creating a
 *               prosumer with two different sets of rules, and BR-7 would then
 *               depend on which screen somebody happened to use.
 *
 * Telling the user what happens next
 *               On success the form is replaced by a panel explaining that the
 *               account is awaiting activation. Without it a user would try to
 *               sign in straight away, be refused, and reasonably conclude the
 *               registration had failed.
 *
 * Validation   : Shape only — NIC format, a plausible email, ten digit phone,
 *               password length. Whether the NIC or email is already taken is
 *               a question only the database can answer, so that refusal comes
 *               back from the API and is displayed as it arrives.
 * ---------------------------------------------------------------------------
 */

import { useState } from 'react';
import { Link } from 'react-router-dom';
import { registerProsumer } from '../api/prosumersApi';
import AuthAside, { REGISTER_POINTS } from '../components/AuthAside';
import { IconAlert, IconCheck } from '../components/Icons';

const EMPTY_FORM = {
  nic: '',
  fullName: '',
  email: '',
  phone: '',
  address: '',
  solarCapacityKw: '',
  password: '',
  confirmPassword: '',
};

export default function RegisterPage() {
  const [form, setForm] = useState(EMPTY_FORM);
  const [fieldErrors, setFieldErrors] = useState({});
  const [formError, setFormError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isRegistered, setIsRegistered] = useState(false);

  function update(field) {
    return (event) => {
      const { value } = event.target;
      setForm((current) => ({ ...current, [field]: value }));
      setFormError('');

      // Clear this field's message as soon as the user starts correcting it.
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

    // Accepts the old nine digit plus letter format and the current twelve
    // digit one, matching the rule the API applies.
    if (!/^(\d{9}[VvXx]|\d{12})$/.test(form.nic.trim())) {
      errors.nic = 'Enter a valid NIC: 12 digits, or 9 digits followed by V or X.';
    }
    if (!form.fullName.trim()) {
      errors.fullName = 'Enter your full name.';
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) {
      errors.email = 'Enter a valid email address.';
    }
    if (!/^0\d{9}$/.test(form.phone.trim())) {
      errors.phone = 'Enter a 10 digit number, for example 0771234567.';
    }
    if (!form.address.trim()) {
      errors.address = 'Enter the address of the property.';
    }

    const capacity = Number(form.solarCapacityKw);
    if (!form.solarCapacityKw || Number.isNaN(capacity) || capacity <= 0) {
      errors.solarCapacityKw = 'Enter the size of your solar array in kW.';
    }
    if (form.password.length < 8) {
      errors.password = 'Use at least 8 characters.';
    }

    // Checked in the browser only. The API never receives this field — it
    // exists purely to catch a typo before the account is created with a
    // password the user cannot reproduce.
    if (form.confirmPassword !== form.password) {
      errors.confirmPassword = 'The two passwords do not match.';
    }

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
      await registerProsumer({
        nic: form.nic.trim(),
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        phone: form.phone.trim(),
        address: form.address.trim(),
        solarCapacityKw: Number(form.solarCapacityKw),
        password: form.password,
      });

      setIsRegistered(true);
    } catch (submitError) {
      // A duplicate NIC or email is refused by the database's unique indexes.
      setFormError(submitError.message);
      setIsSubmitting(false);
    }
  }

  return (
    <div className="auth-page">
      <AuthAside
        heading="Trade your surplus solar energy with the community grid."
        points={REGISTER_POINTS}
      />

      <div className="auth-main">
        <div className="auth-form-wrap">
          <Link to="/" className="auth-mobile-brand text-decoration-none">
            <img src="/logo.png" alt="" width="26" height="26" className="brand-logo" />
            <span className="fw-semibold">Smart Solar Microgrid</span>
          </Link>

          {isRegistered ? <RegisteredPanel email={form.email} /> : (
            <>
              <div className="mb-4">
                <h1 className="h3 fw-semibold mb-1">Register as a prosumer</h1>
                <p className="text-body-secondary mb-0" style={{ fontSize: '0.9rem' }}>
                  Your NIC identifies you across the system.
                </p>
              </div>

              {formError && (
                <div className="alert alert-danger d-flex gap-2 py-2 small" role="alert">
                  <IconAlert />
                  <span>{formError}</span>
                </div>
              )}

              <form onSubmit={handleSubmit} noValidate>
                <Field
                  id="nic"
                  label="NIC"
                  placeholder="200156789012"
                  value={form.nic}
                  onChange={update('nic')}
                  error={fieldErrors.nic}
                  className="font-monospace"
                  autoFocus
                />

                <Field
                  id="fullName"
                  label="Full name"
                  placeholder="Nuwan Silva"
                  value={form.fullName}
                  onChange={update('fullName')}
                  error={fieldErrors.fullName}
                />

                <div className="row g-3">
                  <div className="col-sm-7">
                    <Field
                      id="email"
                      label="Email address"
                      type="email"
                      placeholder="you@example.lk"
                      value={form.email}
                      onChange={update('email')}
                      error={fieldErrors.email}
                    />
                  </div>
                  <div className="col-sm-5">
                    <Field
                      id="phone"
                      label="Phone"
                      placeholder="0771234567"
                      value={form.phone}
                      onChange={update('phone')}
                      error={fieldErrors.phone}
                    />
                  </div>
                </div>

                <Field
                  id="address"
                  label="Property address"
                  placeholder="12 Lake Road, Kandy"
                  value={form.address}
                  onChange={update('address')}
                  error={fieldErrors.address}
                />

                <Field
                  id="solarCapacityKw"
                  label="Solar array size (kW)"
                  type="number"
                  step="0.1"
                  placeholder="6.5"
                  value={form.solarCapacityKw}
                  onChange={update('solarCapacityKw')}
                  error={fieldErrors.solarCapacityKw}
                  hint="The rated output of your panels."
                />

                <div className="row g-3">
                  <div className="col-sm-6">
                    <Field
                      id="password"
                      label="Password"
                      type="password"
                      value={form.password}
                      onChange={update('password')}
                      error={fieldErrors.password}
                      autoComplete="new-password"
                      hint="At least 8 characters."
                    />
                  </div>
                  <div className="col-sm-6">
                    <Field
                      id="confirmPassword"
                      label="Confirm password"
                      type="password"
                      value={form.confirmPassword}
                      onChange={update('confirmPassword')}
                      error={fieldErrors.confirmPassword}
                      autoComplete="new-password"
                    />
                  </div>
                </div>

                <div className="alert alert-info py-2 small mt-2" role="note">
                  Your account is reviewed by a Backoffice officer before you can sign in.
                </div>

                <button
                  type="submit"
                  className="btn btn-primary w-100 py-2"
                  disabled={isSubmitting}
                >
                  {isSubmitting ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />
                      Creating account...
                    </>
                  ) : (
                    'Create account'
                  )}
                </button>
              </form>

              <hr className="my-4" />

              <p className="text-body-secondary small text-center mb-0">
                Already registered?{' '}
                <Link to="/login" className="fw-semibold text-decoration-none">
                  Sign in
                </Link>
              </p>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

/**
 * One labelled input with its optional hint and error message.
 * Written once so every field on the form has identical spacing and the
 * error always appears in the same place relative to its box.
 */
function Field({ id, label, error, hint, className = '', ...inputProps }) {
  return (
    <div className="mb-3">
      <label htmlFor={id} className="form-label small fw-semibold">
        {label}
      </label>
      <input
        id={id}
        className={`form-control${error ? ' is-invalid' : ''} ${className}`.trim()}
        {...inputProps}
      />
      {error ? (
        <div className="auth-field-error">{error}</div>
      ) : (
        hint && <div className="form-text small">{hint}</div>
      )}
    </div>
  );
}

/**
 * Replaces the form once the account exists, so the user knows the
 * registration worked AND that they cannot sign in yet.
 */
function RegisteredPanel({ email }) {
  return (
    <div className="text-center">
      <div className="auth-success-icon">
        <IconCheck size={26} />
      </div>

      <h1 className="h4 fw-semibold mb-2">Account created</h1>

      <p className="text-body-secondary" style={{ fontSize: '0.92rem' }}>
        We have registered <span className="fw-semibold">{email}</span>.
      </p>

      <div className="alert alert-warning text-start small" role="status">
        <div className="fw-semibold mb-1">You cannot sign in yet</div>
        A Backoffice officer reviews every new prosumer before the account is activated.
        You will be able to sign in once that is done.
      </div>

      <Link to="/login" className="btn btn-primary w-100 py-2 mt-2">
        Go to sign in
      </Link>

      <Link to="/" className="btn btn-link btn-sm mt-2">
        Back to home
      </Link>
    </div>
  );
}
