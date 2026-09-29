/*
 * ---------------------------------------------------------------------------
 * File        : StaffUserFormModal.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : The form for creating a new staff account and for editing an
 *               existing one. One component serves both, because the fields
 *               are the same and only a few behave differently.
 *
 * What changes between the two modes
 *               NIC, role and password appear only when creating. They are
 *               absent when editing for the same reason the API's
 *               UpdateUserRequestDto has no fields for them: NIC is the
 *               person's key, and a role change is a different decision from
 *               correcting a phone number. Showing boxes the service would
 *               ignore would be worse than not showing them.
 *
 * Validation  : Only shape is checked here — required boxes, an email that
 *               looks like an email, an NIC in one of the two Sri Lankan
 *               formats. Whether the NIC is already taken is a question only
 *               the database can answer, so that refusal comes back from the
 *               API and is displayed as it arrives.
 * ---------------------------------------------------------------------------
 */

import { useState } from 'react';

const EMPTY_FORM = {
  nic: '',
  fullName: '',
  email: '',
  phone: '',
  address: '',
  role: 'GridOperator',
  password: '',
};

export default function StaffUserFormModal({ user, onSave, onCancel }) {
  const isEditing = Boolean(user);

  const [form, setForm] = useState(() =>
    isEditing
      ? {
          ...EMPTY_FORM,
          nic: user.nic,
          fullName: user.fullName,
          email: user.email,
          phone: user.phone,
          address: user.address ?? '',
          role: user.role,
        }
      : EMPTY_FORM,
  );

  const [error, setError] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  function update(field, value) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  /** Checks the shape of the input. Returns a message, or null when valid. */
  function validate() {
    if (!form.fullName.trim()) return 'Enter the full name.';
    if (!form.email.trim()) return 'Enter an email address.';
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) {
      return 'Enter a valid email address.';
    }
    if (!/^0\d{9}$/.test(form.phone.trim())) {
      return 'Enter a valid 10 digit phone number, for example 0771234567.';
    }

    if (!isEditing) {
      // Accepts the old nine digit plus letter format and the current twelve
      // digit one, matching the rule the API applies.
      if (!/^(\d{9}[VvXx]|\d{12})$/.test(form.nic.trim())) {
        return 'Enter a valid NIC: nine digits followed by V or X, or twelve digits.';
      }
      if (form.password.length < 8) {
        return 'The password must be at least 8 characters.';
      }
    }

    return null;
  }

  async function handleSubmit(event) {
    event.preventDefault();

    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }

    setError('');
    setIsSaving(true);

    try {
      await onSave(buildPayload());
    } catch (saveError) {
      // A refusal from the service, such as a duplicate NIC or email.
      setError(saveError.message);
      setIsSaving(false);
    }
  }

  /** Builds only the fields the matching API request accepts. */
  function buildPayload() {
    const common = {
      fullName: form.fullName.trim(),
      email: form.email.trim(),
      phone: form.phone.trim(),
      address: form.address.trim(),
    };

    if (isEditing) {
      // The update request carries a solar capacity that only means anything
      // for prosumers; staff have no panels, so it is sent as zero.
      return { ...common, solarCapacityKw: 0 };
    }

    return {
      ...common,
      nic: form.nic.trim(),
      role: form.role,
      password: form.password,
    };
  }

  return (
    <>
      <div className="modal fade show d-block" tabIndex="-1" role="dialog" aria-modal="true">
        <div className="modal-dialog modal-dialog-centered modal-lg">
          <div className="modal-content border-0 shadow">
            <form onSubmit={handleSubmit} noValidate>
              <div className="modal-header">
                <h5 className="modal-title h6 fw-semibold">
                  {isEditing ? 'Edit staff account' : 'New staff account'}
                </h5>
                <button
                  type="button"
                  className="btn-close"
                  aria-label="Close"
                  onClick={onCancel}
                  disabled={isSaving}
                />
              </div>

              <div className="modal-body">
                {error && (
                  <div className="alert alert-danger py-2 small" role="alert">
                    {error}
                  </div>
                )}

                <div className="row g-3">
                  {!isEditing && (
                    <>
                      <div className="col-md-6">
                        <label htmlFor="nic" className="form-label small fw-semibold">
                          NIC
                        </label>
                        <input
                          id="nic"
                          className="form-control"
                          placeholder="199012345678"
                          value={form.nic}
                          onChange={(event) => update('nic', event.target.value)}
                        />
                      </div>

                      <div className="col-md-6">
                        <label htmlFor="role" className="form-label small fw-semibold">
                          Role
                        </label>
                        <select
                          id="role"
                          className="form-select"
                          value={form.role}
                          onChange={(event) => update('role', event.target.value)}
                        >
                          <option value="GridOperator">Grid Operator</option>
                          <option value="Backoffice">Backoffice Officer</option>
                        </select>
                        <div className="form-text small">
                          Prosumers register through the mobile application.
                        </div>
                      </div>
                    </>
                  )}

                  {isEditing && (
                    <div className="col-12">
                      <div className="bg-body-tertiary rounded p-3 small">
                        <span className="text-body-secondary">NIC</span>{' '}
                        <span className="font-monospace">{form.nic}</span>
                        <span className="text-body-secondary ms-3">Role</span>{' '}
                        <span className="fw-semibold">{form.role}</span>
                        <div className="text-body-secondary mt-1">
                          These identify the account and cannot be changed here.
                        </div>
                      </div>
                    </div>
                  )}

                  <div className="col-md-6">
                    <label htmlFor="fullName" className="form-label small fw-semibold">
                      Full name
                    </label>
                    <input
                      id="fullName"
                      className="form-control"
                      value={form.fullName}
                      onChange={(event) => update('fullName', event.target.value)}
                    />
                  </div>

                  <div className="col-md-6">
                    <label htmlFor="email" className="form-label small fw-semibold">
                      Email address
                    </label>
                    <input
                      id="email"
                      type="email"
                      className="form-control"
                      value={form.email}
                      onChange={(event) => update('email', event.target.value)}
                    />
                    <div className="form-text small">Used to sign in.</div>
                  </div>

                  <div className="col-md-6">
                    <label htmlFor="phone" className="form-label small fw-semibold">
                      Phone
                    </label>
                    <input
                      id="phone"
                      className="form-control"
                      placeholder="0771234567"
                      value={form.phone}
                      onChange={(event) => update('phone', event.target.value)}
                    />
                  </div>

                  <div className="col-md-6">
                    <label htmlFor="address" className="form-label small fw-semibold">
                      Address
                    </label>
                    <input
                      id="address"
                      className="form-control"
                      value={form.address}
                      onChange={(event) => update('address', event.target.value)}
                    />
                  </div>

                  {!isEditing && (
                    <div className="col-md-6">
                      <label htmlFor="password" className="form-label small fw-semibold">
                        Initial password
                      </label>
                      <input
                        id="password"
                        type="password"
                        className="form-control"
                        value={form.password}
                        onChange={(event) => update('password', event.target.value)}
                        autoComplete="new-password"
                      />
                      <div className="form-text small">At least 8 characters.</div>
                    </div>
                  )}
                </div>
              </div>

              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-outline-secondary btn-sm"
                  onClick={onCancel}
                  disabled={isSaving}
                >
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary btn-sm" disabled={isSaving}>
                  {isSaving ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />
                      Saving...
                    </>
                  ) : isEditing ? (
                    'Save changes'
                  ) : (
                    'Create account'
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      <div className="modal-backdrop fade show" />
    </>
  );
}
