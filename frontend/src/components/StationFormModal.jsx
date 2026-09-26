/*
 * ---------------------------------------------------------------------------
 * File        : StationFormModal.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : The form for registering a microgrid node and for editing one.
 *               Captures the three things the assignment names — GPS location,
 *               capacity in kW/h, and the battery storage slots — plus the
 *               weekly opening hours used to generate booking windows.
 *
 * Station code
 *               Set once when the node is registered and shown read-only
 *               afterwards. It is the node's stable reference, printed on signs
 *               and quoted in booking references, so changing it later would
 *               orphan every mention of it. The API's update request has no
 *               field for it either.
 *
 * Schedule    : Entered here only when registering, because an existing node's
 *               schedule is replaced through its own endpoint. That separation
 *               matters: replacing opening hours can leave already generated
 *               windows outside them, which is a decision worth making on its
 *               own screen rather than as a side effect of correcting a name.
 * ---------------------------------------------------------------------------
 */

import { useState } from 'react';

const DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

/** A sensible starting schedule: longer weekdays, shorter weekends. */
function defaultSchedule() {
  return DAYS.map((day) => ({
    dayOfWeek: day,
    enabled: true,
    openTime: day === 'Saturday' || day === 'Sunday' ? '08:00' : '06:00',
    closeTime: day === 'Saturday' || day === 'Sunday' ? '16:00' : '20:00',
  }));
}

export default function StationFormModal({ station, onSave, onCancel }) {
  const isEditing = Boolean(station);

  const [form, setForm] = useState(() => ({
    stationCode: station?.stationCode ?? '',
    name: station?.name ?? '',
    latitude: station?.location?.latitude ?? 6.9271,
    longitude: station?.location?.longitude ?? 79.8612,
    addressLine: station?.location?.addressLine ?? '',
    capacityKwh: station?.capacityKwh ?? 150,
    totalBatterySlots: station?.totalBatterySlots ?? 8,
    availableBatterySlots: station?.availableBatterySlots ?? 8,
  }));

  const [schedule, setSchedule] = useState(defaultSchedule);
  const [error, setError] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  function update(field, value) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function updateDay(index, field, value) {
    setSchedule((current) =>
      current.map((entry, position) =>
        position === index ? { ...entry, [field]: value } : entry,
      ),
    );
  }

  function validate() {
    if (!isEditing && !/^[A-Z0-9-]{3,20}$/.test(form.stationCode.trim().toUpperCase())) {
      return 'Station code must be 3 to 20 characters of capital letters, digits or hyphens.';
    }
    if (!form.name.trim()) return 'Enter the node name.';
    if (!form.addressLine.trim()) return 'Enter the street address.';

    const latitude = Number(form.latitude);
    const longitude = Number(form.longitude);
    if (Number.isNaN(latitude) || latitude < -90 || latitude > 90) {
      return 'Latitude must be a number between -90 and 90.';
    }
    if (Number.isNaN(longitude) || longitude < -180 || longitude > 180) {
      return 'Longitude must be a number between -180 and 180.';
    }

    if (Number(form.capacityKwh) <= 0) return 'Capacity must be greater than zero.';
    if (Number(form.totalBatterySlots) < 1) return 'A node needs at least one battery slot.';

    if (isEditing && Number(form.availableBatterySlots) > Number(form.totalBatterySlots)) {
      return 'Free battery slots cannot exceed the total installed.';
    }

    // Each open day must close after it opens, the same check the API applies.
    const badDay = schedule.find(
      (entry) => entry.enabled && entry.closeTime <= entry.openTime,
    );
    if (!isEditing && badDay) {
      return `The closing time for ${badDay.dayOfWeek} must be later than its opening time.`;
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
      setError(saveError.message);
      setIsSaving(false);
    }
  }

  function buildPayload() {
    const location = {
      latitude: Number(form.latitude),
      longitude: Number(form.longitude),
      addressLine: form.addressLine.trim(),
    };

    if (isEditing) {
      return {
        name: form.name.trim(),
        location,
        capacityKwh: Number(form.capacityKwh),
        totalBatterySlots: Number(form.totalBatterySlots),
        availableBatterySlots: Number(form.availableBatterySlots),
      };
    }

    return {
      stationCode: form.stationCode.trim().toUpperCase(),
      name: form.name.trim(),
      location,
      capacityKwh: Number(form.capacityKwh),
      totalBatterySlots: Number(form.totalBatterySlots),
      // Only the days left enabled are sent; a day with no entry is closed.
      schedule: schedule
        .filter((entry) => entry.enabled)
        .map(({ dayOfWeek, openTime, closeTime }) => ({ dayOfWeek, openTime, closeTime })),
    };
  }

  return (
    <>
      <div className="modal fade show d-block" tabIndex="-1" role="dialog" aria-modal="true">
        <div className="modal-dialog modal-dialog-centered modal-lg modal-dialog-scrollable">
          <div className="modal-content border-0 shadow">
            <form onSubmit={handleSubmit} noValidate>
              <div className="modal-header">
                <h5 className="modal-title h6 fw-semibold">
                  {isEditing ? `Edit ${station.stationCode}` : 'Register microgrid node'}
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
                  <div className="col-md-4">
                    <label htmlFor="stationCode" className="form-label small fw-semibold">
                      Station code
                    </label>
                    <input
                      id="stationCode"
                      className="form-control font-monospace"
                      placeholder="MG-COL-003"
                      value={form.stationCode}
                      onChange={(event) => update('stationCode', event.target.value.toUpperCase())}
                      disabled={isEditing}
                    />
                    {isEditing && (
                      <div className="form-text small">The code identifies the node permanently.</div>
                    )}
                  </div>

                  <div className="col-md-8">
                    <label htmlFor="name" className="form-label small fw-semibold">
                      Node name
                    </label>
                    <input
                      id="name"
                      className="form-control"
                      placeholder="Dehiwala Coastal Hub"
                      value={form.name}
                      onChange={(event) => update('name', event.target.value)}
                    />
                  </div>

                  {/* --- Location ------------------------------------------ */}
                  <div className="col-12">
                    <hr className="my-1" />
                    <div className="text-uppercase text-body-secondary fw-semibold small" style={{ letterSpacing: '0.08em' }}>
                      GPS location
                    </div>
                    <div className="form-text small mb-2">
                      Used to plot the node on the map in the mobile application.
                    </div>
                  </div>

                  <div className="col-md-3">
                    <label htmlFor="latitude" className="form-label small fw-semibold">
                      Latitude
                    </label>
                    <input
                      id="latitude"
                      type="number"
                      step="0.0001"
                      className="form-control"
                      value={form.latitude}
                      onChange={(event) => update('latitude', event.target.value)}
                    />
                  </div>

                  <div className="col-md-3">
                    <label htmlFor="longitude" className="form-label small fw-semibold">
                      Longitude
                    </label>
                    <input
                      id="longitude"
                      type="number"
                      step="0.0001"
                      className="form-control"
                      value={form.longitude}
                      onChange={(event) => update('longitude', event.target.value)}
                    />
                  </div>

                  <div className="col-md-6">
                    <label htmlFor="addressLine" className="form-label small fw-semibold">
                      Street address
                    </label>
                    <input
                      id="addressLine"
                      className="form-control"
                      placeholder="Galle Road, Dehiwala"
                      value={form.addressLine}
                      onChange={(event) => update('addressLine', event.target.value)}
                    />
                  </div>

                  {/* --- Capacity ------------------------------------------ */}
                  <div className="col-12">
                    <hr className="my-1" />
                    <div className="text-uppercase text-body-secondary fw-semibold small" style={{ letterSpacing: '0.08em' }}>
                      Capacity
                    </div>
                  </div>

                  <div className="col-md-4">
                    <label htmlFor="capacityKwh" className="form-label small fw-semibold">
                      Capacity (kW/h)
                    </label>
                    <input
                      id="capacityKwh"
                      type="number"
                      step="0.1"
                      className="form-control"
                      value={form.capacityKwh}
                      onChange={(event) => update('capacityKwh', event.target.value)}
                    />
                  </div>

                  <div className="col-md-4">
                    <label htmlFor="totalBatterySlots" className="form-label small fw-semibold">
                      Battery slots installed
                    </label>
                    <input
                      id="totalBatterySlots"
                      type="number"
                      className="form-control"
                      value={form.totalBatterySlots}
                      onChange={(event) => update('totalBatterySlots', event.target.value)}
                    />
                  </div>

                  {isEditing && (
                    <div className="col-md-4">
                      <label htmlFor="availableBatterySlots" className="form-label small fw-semibold">
                        Battery slots free
                      </label>
                      <input
                        id="availableBatterySlots"
                        type="number"
                        className="form-control"
                        value={form.availableBatterySlots}
                        onChange={(event) => update('availableBatterySlots', event.target.value)}
                      />
                      <div className="form-text small">Maintained by grid operators.</div>
                    </div>
                  )}

                  {/* --- Schedule, only when registering ------------------- */}
                  {!isEditing && (
                    <>
                      <div className="col-12">
                        <hr className="my-1" />
                        <div className="text-uppercase text-body-secondary fw-semibold small" style={{ letterSpacing: '0.08em' }}>
                          Weekly opening hours
                        </div>
                        <div className="form-text small mb-2">
                          Booking windows are generated from these hours. Untick a day to close
                          the node on that day.
                        </div>
                      </div>

                      <div className="col-12">
                        {schedule.map((entry, index) => (
                          <div className="row g-2 align-items-center mb-2" key={entry.dayOfWeek}>
                            <div className="col-5 col-sm-4">
                              <div className="form-check">
                                <input
                                  className="form-check-input"
                                  type="checkbox"
                                  id={`day-${entry.dayOfWeek}`}
                                  checked={entry.enabled}
                                  onChange={(event) =>
                                    updateDay(index, 'enabled', event.target.checked)
                                  }
                                />
                                <label className="form-check-label small" htmlFor={`day-${entry.dayOfWeek}`}>
                                  {entry.dayOfWeek}
                                </label>
                              </div>
                            </div>

                            <div className="col">
                              <input
                                type="time"
                                className="form-control form-control-sm"
                                value={entry.openTime}
                                onChange={(event) => updateDay(index, 'openTime', event.target.value)}
                                disabled={!entry.enabled}
                                aria-label={`${entry.dayOfWeek} opening time`}
                              />
                            </div>

                            <div className="col-auto text-body-secondary small">to</div>

                            <div className="col">
                              <input
                                type="time"
                                className="form-control form-control-sm"
                                value={entry.closeTime}
                                onChange={(event) => updateDay(index, 'closeTime', event.target.value)}
                                disabled={!entry.enabled}
                                aria-label={`${entry.dayOfWeek} closing time`}
                              />
                            </div>
                          </div>
                        ))}
                      </div>
                    </>
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
                    'Register node'
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
