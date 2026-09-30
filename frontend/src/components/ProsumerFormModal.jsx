/*
 * ---------------------------------------------------------------------------
 * File        : ProsumerFormModal.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-20
 * Description : The form a member of staff uses to register a prosumer at the
 *               counter, and to correct an existing prosumer's details.
 *
 * Why the back office can register one
 *               Prosumers normally sign themselves up on the Android
 *               application, but the assignment asks the web application to
 *               create them too — somebody without a smartphone still has to
 *               be able to join, with an officer entering their details.
 *
 * Same rules either way
 *               A prosumer created here goes through the very same
 *               /auth/register endpoint the mobile application uses, so the
 *               account is created Pending and still needs activating. Giving
 *               the back office a private shortcut that skipped that would
 *               mean two ways of creating a prosumer with two different sets of
 *               rules, and BR-7 would then depend on which screen was used.
 *
 * NIC         : Entered once and shown read-only afterwards. It is the
 *               prosumer's primary key, referenced by every booking they hold,
 *               so changing it would orphan their history. The API's update
 *               request has no field for it either.
 * ---------------------------------------------------------------------------
 */

import { useState } from 'react';

export default function ProsumerFormModal({ prosumer, onSave, onCancel }) {
  const isEditing = Boolean(prosumer);

  const [form, setForm] = useState({
    nic: prosumer?.nic ?? '',
    fullName: prosumer?.fullName ?? '',
    email: prosumer?.email ?? '',
    phone: prosumer?.phone ?? '',
    address: prosumer?.address ?? '',
    solarCapacityKw: prosumer?.solarCapacityKw ?? 5,
    password: '',
  });

  const [error, setError] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  function update(field, value) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function validate() {
    if (!isEditing && !/^(\d{9}[VvXx]|\d{12})$/.test(form.nic.trim())) {
      return 'Enter a valid NIC: nine digits followed by V or X, or twelve digits.';
    }
    if (!form.fullName.trim()) return 'Enter the full name.';
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) {
      return 'Enter a valid email address.';
    }
    if (!/^0\d{9}$/.test(form.phone.trim())) {
      return 'Enter a valid 10 digit phone number, for example 0771234567.';
    }
    if (!form.address.trim()) return 'Enter the property address.';

    const capacity = Number(form.solarCapacityKw);
    if (Number.isNaN(capacity) || capacity <= 0) {
      return 'Solar array capacity must be greater than zero.';
    }
    if (!isEditing && form.password.length < 8) {
      return 'The password must be at least 8 characters.';
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
      const common = {
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        phone: form.phone.trim(),
        address: form.address.trim(),
        solarCapacityKw: Number(form.solarCapacityKw),
      };

      await onSave(
        isEditing
          ? common
          : { ...common, nic: form.nic.trim(), password: form.password },
      );
    } catch (saveError) {
      // A duplicate NIC or email is refused by the database's unique indexes.
      setError(saveError.message);
      setIsSaving(false);
    }
  }

  return (
    <>
      <div className="modal fade show d-block" tabIndex="-1" role="dialog" aria-modal="true">
        <div className="modal-dialog modal-dialog-centered modal-lg">
          <div className="modal-content border-0 shadow">
            <form onSubmit={handleSubmit} noValidate>
              <div className="modal-header">
                <h5 className="modal-title h6 fw-semibold">
                  {isEditing ? `Edit ${prosumer.fullName}` : 'Register prosumer'}
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
                  <div className="col-md-6">
                    <label htmlFor="p-nic" className="form-label small fw-semibold">
                      NIC
                    </label>
                    <input
                      id="p-nic"
                      className="form-control font-monospace"
                      placeholder="199512345678"
                      value={form.nic}
                      onChange={(event) => update('nic', event.target.value)}
                      disabled={isEditing}
                    />
                    <div className="form-text small">
                      {isEditing
                        ? 'The NIC identifies the prosumer permanently.'
                        : 'Used as the prosumer’s primary key across the system.'}
                    </div>
                  </div>

                  <div className="col-md-6">
                    <label htmlFor="p-fullName" className="form-label small fw-semibold">
                      Full name
                    </label>
                    <input
                      id="p-fullName"
                      className="form-control"
                      value={form.fullName}
                      onChange={(event) => update('fullName', event.target.value)}
                    />
                  </div>

                  <div className="col-md-6">
                    <label htmlFor="p-email" className="form-label small fw-semibold">
                      Email address
                    </label>
                    <input
                      id="p-email"
                      type="email"
                      className="form-control"
                      value={form.email}
                      onChange={(event) => update('email', event.target.value)}
                    />
                  </div>

                  <div className="col-md-6">
                    <label htmlFor="p-phone" className="form-label small fw-semibold">
                      Phone
                    </label>
                    <input
                      id="p-phone"
                      className="form-control"
                      placeholder="0771234567"
                      value={form.phone}
                      onChange={(event) => update('phone', event.target.value)}
                    />
                  </div>

                  <div className="col-md-8">
                    <label htmlFor="p-address" className="form-label small fw-semibold">
                      Property address
                    </label>
                    <input
                      id="p-address"
                      className="form-control"
                      value={form.address}
                      onChange={(event) => update('address', event.target.value)}
                    />
                  </div>

                  <div className="col-md-4">
                    <label htmlFor="p-capacity" className="form-label small fw-semibold">
                      Solar array (kW)
                    </label>
                    <input
                      id="p-capacity"
                      type="number"
                      step="0.1"
                      className="form-control"
                      value={form.solarCapacityKw}
                      onChange={(event) => update('solarCapacityKw', event.target.value)}
                    />
                  </div>

                  {!isEditing && (
                    <div className="col-md-6">
                      <label htmlFor="p-password" className="form-label small fw-semibold">
                        Initial password
                      </label>
                      <input
                        id="p-password"
                        type="password"
                        className="form-control"
                        value={form.password}
                        onChange={(event) => update('password', event.target.value)}
                        autoComplete="new-password"
                      />
                      <div className="form-text small">
                        At least 8 characters. The prosumer signs in with their NIC.
                      </div>
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
                  {isSaving ? 'Saving...' : isEditing ? 'Save changes' : 'Register prosumer'}
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
