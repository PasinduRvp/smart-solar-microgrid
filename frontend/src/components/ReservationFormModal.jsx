/*
 * ---------------------------------------------------------------------------
 * File        : ReservationFormModal.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : The form a member of staff uses to book an energy trading slot
 *               on a prosumer's behalf, and to change an existing booking.
 *
 * Choosing a window rather than a time
 *               The form asks for a node and then one of that node's booking
 *               windows. A free date and time box would let someone pick an
 *               hour the node is shut, or a moment outside the seven day
 *               window, and the request would simply be refused. Offering only
 *               the windows that exist makes most refusals impossible to
 *               trigger by accident.
 *
 * Which windows are offered
 *               Only those the service marked bookable — open, with room left,
 *               and still in the future. That flag is calculated by the API, so
 *               this form never works out for itself whether a window can be
 *               booked; it reads the answer. The remaining rules, BR-1 for the
 *               seven day window and BR-9 for capacity, are still enforced when
 *               the request arrives, because a window could fill up between
 *               this list loading and the button being pressed.
 * ---------------------------------------------------------------------------
 */

import { useEffect, useState } from 'react';
import { getStations } from '../api/stationsApi';
import { getStationSlots } from '../api/slotsApi';

export default function ReservationFormModal({ reservation, onSave, onCancel }) {
  const isEditing = Boolean(reservation);

  const [stations, setStations] = useState([]);
  const [slots, setSlots] = useState([]);
  const [isLoadingSlots, setIsLoadingSlots] = useState(false);

  const [form, setForm] = useState({
    prosumerNic: reservation?.prosumerNic ?? '',
    stationId: reservation?.stationId ?? '',
    slotId: reservation?.slotId ?? '',
    energyKwh: reservation?.energyKwh ?? 10,
    direction: reservation?.direction ?? 'Deliver',
  });

  const [error, setError] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  // Only nodes in service can take a booking.
  useEffect(() => {
    getStations(true)
      .then(setStations)
      .catch((loadError) => setError(loadError.message));
  }, []);

  // Reload the windows whenever the chosen node changes.
  useEffect(() => {
    if (!form.stationId) {
      setSlots([]);
      return;
    }

    setIsLoadingSlots(true);
    getStationSlots(form.stationId)
      .then(setSlots)
      .catch((loadError) => setError(loadError.message))
      .finally(() => setIsLoadingSlots(false));
  }, [form.stationId]);

  function update(field, value) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function validate() {
    if (!isEditing && !/^(\d{9}[VvXx]|\d{12})$/.test(form.prosumerNic.trim())) {
      return 'Enter a valid prosumer NIC.';
    }
    if (!form.stationId) return 'Choose a microgrid node.';
    if (!form.slotId) return 'Choose a booking window.';
    if (Number(form.energyKwh) <= 0) return 'Energy must be greater than zero.';
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
      await onSave(
        isEditing
          ? {
              slotId: form.slotId,
              energyKwh: Number(form.energyKwh),
              direction: form.direction,
            }
          : {
              prosumerNic: form.prosumerNic.trim(),
              slotId: form.slotId,
              energyKwh: Number(form.energyKwh),
              direction: form.direction,
            },
      );
    } catch (saveError) {
      // Where BR-1, BR-2 and BR-9 refusals appear, in the service's own words.
      setError(saveError.message);
      setIsSaving(false);
    }
  }

  // The window currently attached to the booking may already be full or past,
  // so it is shown alongside the bookable ones rather than disappearing.
  const selectableSlots = slots.filter(
    (slot) => slot.isBookable || slot.id === reservation?.slotId,
  );

  return (
    <>
      <div className="modal fade show d-block" tabIndex="-1" role="dialog" aria-modal="true">
        <div className="modal-dialog modal-dialog-centered modal-lg">
          <div className="modal-content border-0 shadow">
            <form onSubmit={handleSubmit} noValidate>
              <div className="modal-header">
                <h5 className="modal-title h6 fw-semibold">
                  {isEditing ? `Change booking ${reservation.reservationNo}` : 'New booking'}
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

                {isEditing && (
                  <div className="alert alert-info py-2 small">
                    Changing an approved booking returns it to Pending and cancels its QR code,
                    because the details an officer approved are no longer the details on file.
                  </div>
                )}

                <div className="row g-3">
                  <div className="col-md-6">
                    <label htmlFor="prosumerNic" className="form-label small fw-semibold">
                      Prosumer NIC
                    </label>
                    <input
                      id="prosumerNic"
                      className="form-control font-monospace"
                      placeholder="199512345678"
                      value={form.prosumerNic}
                      onChange={(event) => update('prosumerNic', event.target.value)}
                      disabled={isEditing}
                    />
                    {!isEditing && (
                      <div className="form-text small">
                        The account must be active to hold a booking.
                      </div>
                    )}
                  </div>

                  <div className="col-md-6">
                    <label htmlFor="stationId" className="form-label small fw-semibold">
                      Microgrid node
                    </label>
                    <select
                      id="stationId"
                      className="form-select"
                      value={form.stationId}
                      onChange={(event) => {
                        update('stationId', event.target.value);
                        // The old window belongs to the old node.
                        update('slotId', '');
                      }}
                    >
                      <option value="">Choose a node...</option>
                      {stations.map((station) => (
                        <option key={station.id} value={station.id}>
                          {station.stationCode} — {station.name}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="col-12">
                    <label htmlFor="slotId" className="form-label small fw-semibold">
                      Booking window
                    </label>
                    <select
                      id="slotId"
                      className="form-select"
                      value={form.slotId}
                      onChange={(event) => update('slotId', event.target.value)}
                      disabled={!form.stationId || isLoadingSlots}
                    >
                      <option value="">
                        {!form.stationId
                          ? 'Choose a node first...'
                          : isLoadingSlots
                            ? 'Loading windows...'
                            : selectableSlots.length === 0
                              ? 'No windows available at this node'
                              : 'Choose a window...'}
                      </option>

                      {selectableSlots.map((slot) => (
                        <option key={slot.id} value={slot.id}>
                          {new Date(slot.slotDate).toLocaleDateString(undefined, {
                            weekday: 'short',
                            day: 'numeric',
                            month: 'short',
                          })}
                          {'  '}
                          {slot.startTime}–{slot.endTime}
                          {'  ·  '}
                          {slot.remainingCapacity} place
                          {slot.remainingCapacity === 1 ? '' : 's'} left
                          {'  ·  '}
                          Rs {slot.energyRatePerKwh}/kWh
                        </option>
                      ))}
                    </select>
                    <div className="form-text small">
                      Only windows that are open, have room, and are still in the future.
                    </div>
                  </div>

                  <div className="col-md-6">
                    <label htmlFor="energyKwh" className="form-label small fw-semibold">
                      Energy (kWh)
                    </label>
                    <input
                      id="energyKwh"
                      type="number"
                      step="0.1"
                      className="form-control"
                      value={form.energyKwh}
                      onChange={(event) => update('energyKwh', event.target.value)}
                    />
                  </div>

                  <div className="col-md-6">
                    <label htmlFor="direction" className="form-label small fw-semibold">
                      Direction
                    </label>
                    <select
                      id="direction"
                      className="form-select"
                      value={form.direction}
                      onChange={(event) => update('direction', event.target.value)}
                    >
                      <option value="Deliver">Deliver — prosumer feeds the grid</option>
                      <option value="Draw">Draw — prosumer takes from the grid</option>
                    </select>
                  </div>
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
                  {isSaving ? 'Saving...' : isEditing ? 'Save changes' : 'Create booking'}
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
