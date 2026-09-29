/*
 * ---------------------------------------------------------------------------
 * File        : StationDetailPage.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : One microgrid node in detail: its weekly opening hours, and
 *               the booking windows generated from them. This is where a
 *               Backoffice officer maintains the schedule and generates
 *               windows, and where a Grid Operator opens and closes individual
 *               windows — the "update battery slot availability" duty in the
 *               assignment.
 *
 * Why windows are generated rather than typed
 *               A window is only meaningful inside the node's opening hours,
 *               so the service divides each open day into windows of the chosen
 *               length. A person typing times by hand could create a window at
 *               an hour the node is shut, and the two would silently disagree.
 *
 * Repeating the generation
 *               The service skips days that already have windows, so pressing
 *               Generate twice does not duplicate anything and the second press
 *               is refused with an explanation rather than quietly doing
 *               nothing. That is what makes the button safe to press again
 *               after extending the schedule.
 * ---------------------------------------------------------------------------
 */

import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { getStation, updateStationSchedule } from '../api/stationsApi';
import { generateSlots, getStationSlots, updateSlotAvailability } from '../api/slotsApi';
import { useAuth } from '../auth/AuthContext';

const DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

export default function StationDetailPage() {
  const { id } = useParams();
  const { isBackoffice } = useAuth();

  const [station, setStation] = useState(null);
  const [slots, setSlots] = useState([]);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  const [schedule, setSchedule] = useState([]);
  const [scheduleMessage, setScheduleMessage] = useState(null);
  const [isSavingSchedule, setIsSavingSchedule] = useState(false);

  const [generateOptions, setGenerateOptions] = useState({
    numberOfDays: 7,
    slotDurationMinutes: 120,
    capacityPerSlot: 3,
    energyRatePerKwh: 42.5,
  });
  const [generateMessage, setGenerateMessage] = useState(null);
  const [isGenerating, setIsGenerating] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError('');

    try {
      const [stationResult, slotsResult] = await Promise.all([
        getStation(id),
        getStationSlots(id),
      ]);

      setStation(stationResult);
      setSlots(slotsResult);

      // Turn the stored schedule into a row per day, so days the node is closed
      // still appear as an unticked row that can be switched on.
      setSchedule(
        DAYS.map((day) => {
          const existing = stationResult.schedule.find((entry) => entry.dayOfWeek === day);
          return {
            dayOfWeek: day,
            enabled: Boolean(existing),
            openTime: existing?.openTime ?? '06:00',
            closeTime: existing?.closeTime ?? '18:00',
          };
        }),
      );
    } catch (loadError) {
      setError(loadError.message);
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => {
    load();
  }, [load]);

  function updateDay(index, field, value) {
    setSchedule((current) =>
      current.map((entry, position) => (position === index ? { ...entry, [field]: value } : entry)),
    );
  }

  async function saveSchedule() {
    setIsSavingSchedule(true);
    setScheduleMessage(null);

    try {
      await updateStationSchedule(
        id,
        schedule
          .filter((entry) => entry.enabled)
          .map(({ dayOfWeek, openTime, closeTime }) => ({ dayOfWeek, openTime, closeTime })),
      );

      setScheduleMessage({ type: 'success', text: 'Schedule saved.' });
      await load();
    } catch (saveError) {
      setScheduleMessage({ type: 'danger', text: saveError.message });
    } finally {
      setIsSavingSchedule(false);
    }
  }

  async function handleGenerate() {
    setIsGenerating(true);
    setGenerateMessage(null);

    try {
      const created = await generateSlots(id, {
        numberOfDays: Number(generateOptions.numberOfDays),
        slotDurationMinutes: Number(generateOptions.slotDurationMinutes),
        capacityPerSlot: Number(generateOptions.capacityPerSlot),
        energyRatePerKwh: Number(generateOptions.energyRatePerKwh),
      });

      setGenerateMessage({
        type: 'success',
        text: `Generated ${created.length} booking window${created.length === 1 ? '' : 's'}.`,
      });
      await load();
    } catch (generateError) {
      // A refusal here usually means every day already has windows.
      setGenerateMessage({ type: 'warning', text: generateError.message });
    } finally {
      setIsGenerating(false);
    }
  }

  async function toggleSlot(slot) {
    try {
      await updateSlotAvailability(slot.id, { isAvailable: !slot.isAvailable });
      await load();
    } catch (toggleError) {
      // Closing a window that people have booked is refused by the service.
      setError(toggleError.message);
    }
  }

  if (isLoading) {
    return (
      <div className="d-flex align-items-center gap-2 text-body-secondary py-5">
        <span className="spinner-border spinner-border-sm" aria-hidden="true" />
        Loading node...
      </div>
    );
  }

  if (!station) {
    return (
      <div>
        <div className="alert alert-danger">{error || 'Node not found.'}</div>
        <Link to="/stations" className="btn btn-outline-secondary btn-sm">
          Back to nodes
        </Link>
      </div>
    );
  }

  // Grouped by date so the operator reads a day at a time rather than a long
  // undifferentiated list of times.
  const slotsByDate = slots.reduce((groups, slot) => {
    const key = slot.slotDate.slice(0, 10);
    (groups[key] ??= []).push(slot);
    return groups;
  }, {});

  return (
    <div>
      <nav aria-label="breadcrumb">
        <ol className="breadcrumb small">
          <li className="breadcrumb-item">
            <Link to="/stations" className="text-decoration-none">
              Microgrid Nodes
            </Link>
          </li>
          <li className="breadcrumb-item active">{station.stationCode}</li>
        </ol>
      </nav>

      <div className="d-flex flex-wrap justify-content-between align-items-start gap-2 mb-4">
        <div>
          <h1 className="h3 fw-semibold mb-1">{station.name}</h1>
          <p className="text-body-secondary mb-0">
            <span className="font-monospace">{station.stationCode}</span>
            {' · '}
            {station.location.addressLine}
            {' · '}
            {station.capacityKwh} kW/h
            {' · '}
            {station.availableBatterySlots}/{station.totalBatterySlots} battery slots free
          </p>
        </div>
        {!station.isActive && <span className="badge text-bg-secondary">Deactivated</span>}
      </div>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      <div className="row g-4">
        {/* --- Schedule ---------------------------------------------------- */}
        <div className="col-12 col-xl-5">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <h2 className="h6 fw-semibold mb-1">Weekly opening hours</h2>
              <p className="text-body-secondary small">
                Booking windows are generated from these hours.
              </p>

              {scheduleMessage && (
                <div className={`alert alert-${scheduleMessage.type} py-2 small`} role="alert">
                  {scheduleMessage.text}
                </div>
              )}

              {schedule.map((entry, index) => (
                <div className="row g-2 align-items-center mb-2" key={entry.dayOfWeek}>
                  <div className="col-5">
                    <div className="form-check">
                      <input
                        className="form-check-input"
                        type="checkbox"
                        id={`sched-${entry.dayOfWeek}`}
                        checked={entry.enabled}
                        onChange={(event) => updateDay(index, 'enabled', event.target.checked)}
                        disabled={!isBackoffice}
                      />
                      <label className="form-check-label small" htmlFor={`sched-${entry.dayOfWeek}`}>
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
                      disabled={!entry.enabled || !isBackoffice}
                      aria-label={`${entry.dayOfWeek} opening time`}
                    />
                  </div>
                  <div className="col">
                    <input
                      type="time"
                      className="form-control form-control-sm"
                      value={entry.closeTime}
                      onChange={(event) => updateDay(index, 'closeTime', event.target.value)}
                      disabled={!entry.enabled || !isBackoffice}
                      aria-label={`${entry.dayOfWeek} closing time`}
                    />
                  </div>
                </div>
              ))}

              {isBackoffice && (
                <button
                  type="button"
                  className="btn btn-primary btn-sm mt-2"
                  onClick={saveSchedule}
                  disabled={isSavingSchedule}
                >
                  {isSavingSchedule ? 'Saving...' : 'Save schedule'}
                </button>
              )}
            </div>
          </div>
        </div>

        {/* --- Generation -------------------------------------------------- */}
        <div className="col-12 col-xl-7">
          {isBackoffice && (
            <div className="card border-0 shadow-sm mb-4">
              <div className="card-body">
                <h2 className="h6 fw-semibold mb-1">Generate booking windows</h2>
                <p className="text-body-secondary small">
                  Divides each open day into windows. Days that already have windows are skipped.
                </p>

                {generateMessage && (
                  <div className={`alert alert-${generateMessage.type} py-2 small`} role="alert">
                    {generateMessage.text}
                  </div>
                )}

                <div className="row g-2 align-items-end">
                  <div className="col-6 col-md-3">
                    <label htmlFor="numberOfDays" className="form-label small fw-semibold">
                      Days ahead
                    </label>
                    <input
                      id="numberOfDays"
                      type="number"
                      min="1"
                      max="7"
                      className="form-control form-control-sm"
                      value={generateOptions.numberOfDays}
                      onChange={(event) =>
                        setGenerateOptions((o) => ({ ...o, numberOfDays: event.target.value }))
                      }
                    />
                    <div className="form-text" style={{ fontSize: '0.7rem' }}>Max 7</div>
                  </div>

                  <div className="col-6 col-md-3">
                    <label htmlFor="slotDuration" className="form-label small fw-semibold">
                      Window (min)
                    </label>
                    <input
                      id="slotDuration"
                      type="number"
                      className="form-control form-control-sm"
                      value={generateOptions.slotDurationMinutes}
                      onChange={(event) =>
                        setGenerateOptions((o) => ({ ...o, slotDurationMinutes: event.target.value }))
                      }
                    />
                  </div>

                  <div className="col-6 col-md-3">
                    <label htmlFor="capacityPerSlot" className="form-label small fw-semibold">
                      Bookings each
                    </label>
                    <input
                      id="capacityPerSlot"
                      type="number"
                      className="form-control form-control-sm"
                      value={generateOptions.capacityPerSlot}
                      onChange={(event) =>
                        setGenerateOptions((o) => ({ ...o, capacityPerSlot: event.target.value }))
                      }
                    />
                  </div>

                  <div className="col-6 col-md-3">
                    <label htmlFor="rate" className="form-label small fw-semibold">
                      Rate / kWh
                    </label>
                    <input
                      id="rate"
                      type="number"
                      step="0.5"
                      className="form-control form-control-sm"
                      value={generateOptions.energyRatePerKwh}
                      onChange={(event) =>
                        setGenerateOptions((o) => ({ ...o, energyRatePerKwh: event.target.value }))
                      }
                    />
                  </div>
                </div>

                <button
                  type="button"
                  className="btn btn-primary btn-sm mt-3"
                  onClick={handleGenerate}
                  disabled={isGenerating || !station.isActive}
                >
                  {isGenerating ? 'Generating...' : 'Generate windows'}
                </button>
              </div>
            </div>
          )}

          {/* --- Windows ---------------------------------------------------- */}
          <div className="card border-0 shadow-sm">
            <div className="card-body pb-0">
              <h2 className="h6 fw-semibold mb-1">Booking windows</h2>
              <p className="text-body-secondary small">
                The next seven days. Operators can close a window that has no bookings.
              </p>
            </div>

            {Object.keys(slotsByDate).length === 0 && (
              <p className="text-body-secondary small px-3 pb-3 mb-0">
                No booking windows yet. Generate some above.
              </p>
            )}

            {Object.entries(slotsByDate).map(([date, daySlots]) => (
              <div key={date} className="border-top">
                <div className="px-3 py-2 bg-body-tertiary small fw-semibold">
                  {new Date(date).toLocaleDateString(undefined, {
                    weekday: 'long',
                    day: 'numeric',
                    month: 'short',
                  })}
                </div>

                <div className="d-flex flex-wrap gap-2 p-3">
                  {daySlots.map((slot) => (
                    <div
                      key={slot.id}
                      className={`border rounded px-3 py-2 small${
                        slot.isBookable ? '' : ' bg-body-tertiary text-body-secondary'
                      }`}
                      style={{ minWidth: '9.5rem' }}
                    >
                      <div className="fw-semibold">
                        {slot.startTime} – {slot.endTime}
                      </div>
                      <div style={{ fontVariantNumeric: 'tabular-nums' }}>
                        {slot.bookedCount}/{slot.totalCapacitySlots} booked
                      </div>

                      {!slot.isAvailable && (
                        <div className="text-danger-emphasis fw-semibold">Closed</div>
                      )}
                      {slot.isAvailable && !slot.isBookable && (
                        <div className="text-body-secondary">Full or past</div>
                      )}

                      <button
                        type="button"
                        className="btn btn-link btn-sm p-0 mt-1"
                        onClick={() => toggleSlot(slot)}
                      >
                        {slot.isAvailable ? 'Close' : 'Open'}
                      </button>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
