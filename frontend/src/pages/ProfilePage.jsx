/*
 * ---------------------------------------------------------------------------
 * File        : ProfilePage.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-20
 * Description : "My account" — where every signed in user, whatever their
 *               role, edits their own details and changes their own password.
 *
 * Why one page for all three roles
 *               Editing your own name is the same operation whether you are a
 *               Backoffice officer, a Grid Operator or a Prosumer. The only
 *               difference is the solar array field, which means nothing for
 *               staff and is therefore shown to prosumers only.
 *
 * What cannot be changed here
 *               NIC, role and account status are displayed as read-only facts.
 *               They are not fields on the request at all, so a profile edit
 *               can never change who somebody is or what they may do. A
 *               prosumer cannot activate their own account by editing it, and
 *               nobody can promote themselves.
 *
 * Password     : Kept in a separate form with its own button. Mixing it into
 *               the details form would mean either sending the password on
 *               every name change, or leaving an empty box that quietly does
 *               nothing — both worse than two clear forms.
 * ---------------------------------------------------------------------------
 */

import { useCallback, useEffect, useState } from 'react';
import { changeMyPassword, getMyProfile, updateMyProfile } from '../api/profileApi';
import { useAuth } from '../auth/AuthContext';
import { ROLES } from '../config';
import Avatar from '../components/Avatar';
import PageHeader from '../components/PageHeader';
import StatusBadge from '../components/StatusBadge';
import { useToast } from '../components/ToastProvider';

export default function ProfilePage() {
  const { auth } = useAuth();
  const { showToast } = useToast();

  const [profile, setProfile] = useState(null);
  const [loadError, setLoadError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  const [details, setDetails] = useState(null);
  const [detailsError, setDetailsError] = useState('');
  const [isSavingDetails, setIsSavingDetails] = useState(false);

  const [passwords, setPasswords] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });
  const [passwordError, setPasswordError] = useState('');
  const [isSavingPassword, setIsSavingPassword] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setLoadError('');

    try {
      const result = await getMyProfile();
      setProfile(result);
      setDetails({
        fullName: result.fullName,
        email: result.email,
        phone: result.phone,
        address: result.address ?? '',
        solarCapacityKw: result.solarCapacityKw ?? 0,
      });
    } catch (error) {
      setLoadError(error.message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  function updateDetail(field, value) {
    setDetails((current) => ({ ...current, [field]: value }));
    setDetailsError('');
  }

  async function saveDetails(event) {
    event.preventDefault();

    if (!details.fullName.trim()) return setDetailsError('Enter your full name.');
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(details.email.trim())) {
      return setDetailsError('Enter a valid email address.');
    }
    if (!/^0\d{9}$/.test(details.phone.trim())) {
      return setDetailsError('Enter a 10 digit phone number, for example 0771234567.');
    }

    setDetailsError('');
    setIsSavingDetails(true);

    try {
      const updated = await updateMyProfile({
        fullName: details.fullName.trim(),
        email: details.email.trim(),
        phone: details.phone.trim(),
        address: details.address.trim(),
        solarCapacityKw: Number(details.solarCapacityKw) || 0,
      });

      setProfile(updated);
      showToast('Your details have been saved.');
    } catch (error) {
      setDetailsError(error.message);
    } finally {
      setIsSavingDetails(false);
    }
  }

  async function savePassword(event) {
    event.preventDefault();

    if (!passwords.currentPassword) {
      return setPasswordError('Enter your current password.');
    }
    if (passwords.newPassword.length < 8) {
      return setPasswordError('The new password must be at least 8 characters.');
    }

    // Checked here only. The API never receives this field — it exists purely
    // to catch a typo before the password becomes one the user cannot repeat.
    if (passwords.newPassword !== passwords.confirmPassword) {
      return setPasswordError('The two new passwords do not match.');
    }

    setPasswordError('');
    setIsSavingPassword(true);

    try {
      await changeMyPassword(passwords.currentPassword, passwords.newPassword);
      setPasswords({ currentPassword: '', newPassword: '', confirmPassword: '' });
      showToast('Your password has been changed.');
    } catch (error) {
      setPasswordError(error.message);
    } finally {
      setIsSavingPassword(false);
    }
  }

  if (isLoading) {
    return (
      <div className="d-flex align-items-center gap-2 text-body-secondary py-5">
        <span className="spinner-border spinner-border-sm" aria-hidden="true" />
        Loading your profile...
      </div>
    );
  }

  if (loadError || !profile) {
    return (
      <div>
        <div className="alert alert-danger">{loadError || 'Profile could not be loaded.'}</div>
        <button type="button" className="btn btn-outline-secondary btn-sm" onClick={load}>
          Try again
        </button>
      </div>
    );
  }

  const isProsumer = profile.role === ROLES.PROSUMER;

  return (
    <div>
      <PageHeader
        title="My Account"
        subtitle="Your details are yours to edit. Nobody else can change them for you."
      />

      <div className="row g-4">
        {/* --- Identity, read only ---------------------------------------- */}
        <div className="col-12 col-xl-4">
          <div className="app-card h-100">
            <div className="card-body text-center">
              <div className="d-flex justify-content-center mb-3">
                <Avatar name={profile.fullName} />
              </div>

              <h2 className="h5 fw-semibold mb-1">{profile.fullName}</h2>
              <p className="text-body-secondary small mb-3">{formatRole(profile.role)}</p>

              <dl className="row small text-start mb-0">
                <dt className="col-5 fw-normal text-body-secondary">NIC</dt>
                <dd className="col-7 font-monospace mb-2">{profile.nic}</dd>

                <dt className="col-5 fw-normal text-body-secondary">Status</dt>
                <dd className="col-7 mb-2">
                  <StatusBadge status={profile.status} />
                </dd>

                <dt className="col-5 fw-normal text-body-secondary">Member since</dt>
                <dd className="col-7 mb-0">
                  {new Date(profile.createdAt).toLocaleDateString()}
                </dd>
              </dl>

              <div className="alert alert-light border small text-start mt-3 mb-0">
                Your NIC, role and account status identify you across the system and cannot
                be changed here.
              </div>
            </div>
          </div>
        </div>

        {/* --- Editable details -------------------------------------------- */}
        <div className="col-12 col-xl-8">
          <div className="app-card mb-4">
            <div className="app-card-header">
              <h2 className="app-card-title">Your details</h2>
            </div>

            <div className="card-body">
              {detailsError && (
                <div className="alert alert-danger py-2 small" role="alert">
                  {detailsError}
                </div>
              )}

              <form onSubmit={saveDetails} noValidate>
                <div className="row g-3">
                  <div className="col-md-6">
                    <label htmlFor="p-fullName" className="form-label small fw-semibold">
                      Full name
                    </label>
                    <input
                      id="p-fullName"
                      className="form-control"
                      value={details.fullName}
                      onChange={(event) => updateDetail('fullName', event.target.value)}
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
                      value={details.email}
                      onChange={(event) => updateDetail('email', event.target.value)}
                    />
                    <div className="form-text small">Used to sign in.</div>
                  </div>

                  <div className="col-md-6">
                    <label htmlFor="p-phone" className="form-label small fw-semibold">
                      Phone
                    </label>
                    <input
                      id="p-phone"
                      className="form-control"
                      value={details.phone}
                      onChange={(event) => updateDetail('phone', event.target.value)}
                    />
                  </div>

                  <div className="col-md-6">
                    <label htmlFor="p-address" className="form-label small fw-semibold">
                      Address
                    </label>
                    <input
                      id="p-address"
                      className="form-control"
                      value={details.address}
                      onChange={(event) => updateDetail('address', event.target.value)}
                    />
                  </div>

                  {/* Meaningless for staff, who have no panels. */}
                  {isProsumer && (
                    <div className="col-md-6">
                      <label htmlFor="p-capacity" className="form-label small fw-semibold">
                        Solar array size (kW)
                      </label>
                      <input
                        id="p-capacity"
                        type="number"
                        step="0.1"
                        className="form-control"
                        value={details.solarCapacityKw}
                        onChange={(event) => updateDetail('solarCapacityKw', event.target.value)}
                      />
                    </div>
                  )}
                </div>

                <button
                  type="submit"
                  className="btn btn-primary btn-sm mt-3"
                  disabled={isSavingDetails}
                >
                  {isSavingDetails ? 'Saving...' : 'Save details'}
                </button>
              </form>
            </div>
          </div>

          {/* --- Password ------------------------------------------------- */}
          <div className="app-card">
            <div className="app-card-header">
              <h2 className="app-card-title">Change password</h2>
            </div>

            <div className="card-body">
              {passwordError && (
                <div className="alert alert-danger py-2 small" role="alert">
                  {passwordError}
                </div>
              )}

              <form onSubmit={savePassword} noValidate>
                <div className="row g-3">
                  <div className="col-md-4">
                    <label htmlFor="p-current" className="form-label small fw-semibold">
                      Current password
                    </label>
                    <input
                      id="p-current"
                      type="password"
                      className="form-control"
                      value={passwords.currentPassword}
                      onChange={(event) =>
                        setPasswords((c) => ({ ...c, currentPassword: event.target.value }))
                      }
                      autoComplete="current-password"
                    />
                  </div>

                  <div className="col-md-4">
                    <label htmlFor="p-new" className="form-label small fw-semibold">
                      New password
                    </label>
                    <input
                      id="p-new"
                      type="password"
                      className="form-control"
                      value={passwords.newPassword}
                      onChange={(event) =>
                        setPasswords((c) => ({ ...c, newPassword: event.target.value }))
                      }
                      autoComplete="new-password"
                    />
                    <div className="form-text small">At least 8 characters.</div>
                  </div>

                  <div className="col-md-4">
                    <label htmlFor="p-confirm" className="form-label small fw-semibold">
                      Confirm new password
                    </label>
                    <input
                      id="p-confirm"
                      type="password"
                      className="form-control"
                      value={passwords.confirmPassword}
                      onChange={(event) =>
                        setPasswords((c) => ({ ...c, confirmPassword: event.target.value }))
                      }
                      autoComplete="new-password"
                    />
                  </div>
                </div>

                <div className="alert alert-light border small mt-3 mb-3">
                  Your current password is required. Holding a signed in session is not enough
                  to change it, so an unattended screen cannot be used to lock you out.
                </div>

                <button
                  type="submit"
                  className="btn btn-primary btn-sm"
                  disabled={isSavingPassword}
                >
                  {isSavingPassword ? 'Changing...' : 'Change password'}
                </button>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

/** Turns the stored role name into something readable for a person. */
function formatRole(role) {
  if (role === ROLES.BACKOFFICE) return 'Backoffice Officer';
  if (role === ROLES.GRID_OPERATOR) return 'Grid Operator';
  if (role === ROLES.PROSUMER) return 'Solar Prosumer';
  return '';
}
