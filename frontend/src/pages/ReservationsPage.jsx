/*
 * ---------------------------------------------------------------------------
 * File        : ReservationsPage.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Reservation management: the approval queue, the full booking
 *               history with its search filters, and the actions to create,
 *               change, approve, cancel and complete a booking.
 *
 * Live filters
 *               There is no Search button: changing a filter applies it. The
 *               two kinds of control are treated differently, though, because
 *               they behave differently. A dropdown or a date picker produces
 *               one deliberate change, so it searches at once. The NIC box is
 *               typed into, and searching on every keystroke would fire a
 *               request per character and race the answers against each other,
 *               so it waits for a short pause in typing first.
 *
 * Searching on the server
 *               Every filter is sent to the API, which answers using the
 *               indexes created at startup. Booking history grows without
 *               limit, so downloading it all and filtering in the browser would
 *               get slower every week the system runs. The prosumer and staff
 *               lists are small and fixed, which is why those are filtered
 *               in the browser instead — the same question, answered
 *               differently because the data behaves differently.
 *
 * The 12 hour rule in the interface
 *               Change and Cancel are shown only when the API has said the
 *               booking may still be changed, through the canBeModified and
 *               canBeCancelled flags it calculates. The browser does not work
 *               out twelve hours for itself. If it did, its clock and the
 *               server's could disagree, and a user would be offered a button
 *               that then failed — or refused one that would have worked.
 *               The service also re-checks on every request, so a booking that
 *               crosses the twelve hour line while this page sits open is still
 *               refused correctly.
 * ---------------------------------------------------------------------------
 */

import { useCallback, useEffect, useState } from 'react';
import {
  approveReservation,
  cancelReservation,
  completeReservation,
  createReservation,
  searchReservations,
  updateReservation,
} from '../api/reservationsApi';
import { getStations } from '../api/stationsApi';
import ConfirmDialog from '../components/ConfirmDialog';
import ReservationFormModal from '../components/ReservationFormModal';
import StatusBadge from '../components/StatusBadge';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import TableSkeleton from '../components/TableSkeleton';
import Avatar from '../components/Avatar';
import { IconPlus, IconRefresh } from '../components/Icons';
import { useToast } from '../components/ToastProvider';

const EMPTY_FILTERS = { status: '', nic: '', from: '', to: '', stationId: '' };

/** How long typing must pause before the NIC box triggers a search. */
const NIC_TYPING_PAUSE_MS = 400;

export default function ReservationsPage() {
  const { showToast } = useToast();
  const [filters, setFilters] = useState(EMPTY_FILTERS);

  // The NIC box is held separately from the applied filters so that typing
  // updates the box immediately while the search waits for a pause.
  const [nicInput, setNicInput] = useState('');

  const [reservations, setReservations] = useState([]);
  const [stations, setStations] = useState([]);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  const [formState, setFormState] = useState(null);
  const [pendingAction, setPendingAction] = useState(null);
  const [cancelReason, setCancelReason] = useState('');
  const [actionError, setActionError] = useState('');
  const [isActionBusy, setIsActionBusy] = useState(false);

  const load = useCallback(async (searchFilters) => {
    setIsLoading(true);
    setError('');

    try {
      setReservations(await searchReservations(searchFilters));
    } catch (loadError) {
      setError(loadError.message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Typing settles into the applied filters after a short pause. The cleanup
  // cancels the previous timer, so only the last keystroke in a burst counts.
  useEffect(() => {
    const timer = window.setTimeout(() => {
      setFilters((current) =>
        current.nic === nicInput.trim() ? current : { ...current, nic: nicInput.trim() },
      );
    }, NIC_TYPING_PAUSE_MS);

    return () => window.clearTimeout(timer);
  }, [nicInput]);

  // Any change to the applied filters searches again.
  useEffect(() => {
    load(filters);
  }, [filters, load]);

  useEffect(() => {
    getStations().then(setStations).catch(() => setStations([]));
  }, []);

  /** Changes one filter and searches again. */
  function setFilter(field, value) {
    setFilters((current) => ({ ...current, [field]: value }));
  }

  function clearFilters() {
    setNicInput('');
    setFilters(EMPTY_FILTERS);
  }

  /** Shortcut used by the "Awaiting approval" button above the table. */
  function showPendingOnly() {
    setNicInput('');
    setFilters({ ...EMPTY_FILTERS, status: 'Pending' });
  }

  async function handleSave(payload) {
    if (formState.reservation) {
      await updateReservation(formState.reservation.id, payload);
    } else {
      await createReservation(payload);
    }

    showToast(formState.reservation ? 'Booking updated.' : 'Booking created.');
    setFormState(null);
    await load(filters);
  }

  async function confirmAction() {
    setIsActionBusy(true);
    setActionError('');

    try {
      const { type, reservation } = pendingAction;

      if (type === 'approve') {
        await approveReservation(reservation.id);
        showToast(reservation.reservationNo + ' approved. A QR code has been issued.');
      } else if (type === 'complete') {
        await completeReservation(reservation.id);
        showToast(reservation.reservationNo + ' completed.');
      } else {
        await cancelReservation(reservation.id, cancelReason.trim() || 'Cancelled by staff');
        showToast(reservation.reservationNo + ' cancelled.');
      }

      setPendingAction(null);
      setCancelReason('');
      await load(filters);
    } catch (confirmError) {
      // BR-3's refusal appears here, quoting the hours remaining.
      setActionError(confirmError.message);
    } finally {
      setIsActionBusy(false);
    }
  }

  const pendingCount = reservations.filter((r) => r.status === 'Pending').length;

  // Clear filters is only offered when there is something to clear.
  const hasActiveFilters = Object.values(filters).some(Boolean) || nicInput !== '';

  return (
    <div>
      <PageHeader
        title="Reservations"
        subtitle="Energy trading bookings across every microgrid node. Bookings must fall within seven days, and need twelve hours' notice to change or cancel."
        actions={
          <>
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm d-flex align-items-center gap-2"
              onClick={() => load(filters)}
            >
              <IconRefresh />
              Refresh
            </button>
            <button
              type="button"
              className="btn btn-primary btn-sm d-flex align-items-center gap-2"
              onClick={() => setFormState({ reservation: null })}
            >
              <IconPlus />
              New booking
            </button>
          </>
        }
      />

      {/* --- Filters ------------------------------------------------------- */}
      <div className="app-card mb-3">
        <div className="card-body">
          <div className="row g-2 align-items-end">
            <div className="col-6 col-md-2">
              <label htmlFor="f-status" className="form-label small fw-semibold">Status</label>
              <select
                id="f-status"
                className="form-select form-select-sm"
                value={filters.status}
                onChange={(event) => setFilter('status', event.target.value)}
              >
                <option value="">Any</option>
                <option value="Pending">Pending</option>
                <option value="Approved">Approved</option>
                <option value="Completed">Completed</option>
                <option value="Cancelled">Cancelled</option>
              </select>
            </div>

            <div className="col-6 col-md-2">
              <label htmlFor="f-nic" className="form-label small fw-semibold">Prosumer NIC</label>
              <input
                id="f-nic"
                className="form-control form-control-sm font-monospace"
                placeholder="Any"
                value={nicInput}
                onChange={(event) => setNicInput(event.target.value)}
              />
            </div>

            <div className="col-6 col-md-3">
              <label htmlFor="f-station" className="form-label small fw-semibold">Node</label>
              <select
                id="f-station"
                className="form-select form-select-sm"
                value={filters.stationId}
                onChange={(event) => setFilter('stationId', event.target.value)}
              >
                <option value="">Any node</option>
                {stations.map((station) => (
                  <option key={station.id} value={station.id}>
                    {station.stationCode} — {station.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="col-6 col-md-2">
              <label htmlFor="f-from" className="form-label small fw-semibold">From</label>
              <input
                id="f-from"
                type="date"
                className="form-control form-control-sm"
                value={filters.from}
                onChange={(event) => setFilter('from', event.target.value)}
              />
            </div>

            <div className="col-6 col-md-2">
              <label htmlFor="f-to" className="form-label small fw-semibold">To</label>
              <input
                id="f-to"
                type="date"
                className="form-control form-control-sm"
                value={filters.to}
                onChange={(event) => setFilter('to', event.target.value)}
              />
            </div>

            {/* A quiet indicator that a search is in flight. It replaces the
                Search button's old job of telling the user something happened,
                without taking a click to get there. */}
            <div className="col-12 col-md-1 d-flex align-items-center justify-content-md-center">
              {isLoading && (
                <span
                  className="spinner-border spinner-border-sm text-secondary"
                  role="status"
                  aria-label="Searching"
                />
              )}
            </div>
          </div>

          <div className="d-flex flex-wrap gap-2 mt-3 align-items-center">
            <button
              type="button"
              className={`btn btn-sm${filters.status === 'Pending' ? ' btn-primary' : ' btn-outline-secondary'}`}
              onClick={showPendingOnly}
            >
              Awaiting approval
              {pendingCount > 0 && <span className="badge text-bg-warning ms-2">{pendingCount}</span>}
            </button>

            {hasActiveFilters && (
              <button type="button" className="btn btn-link btn-sm" onClick={clearFilters}>
                Clear filters
              </button>
            )}
          </div>
        </div>
      </div>

      {error && <div className="alert alert-danger" role="alert">{error}</div>}

      {/* --- Table ---------------------------------------------------------- */}
      <div className="app-card">
        <div className="table-responsive">
          <table className="table app-table">
            <thead className="table-light">
              <tr>
                <th scope="col">Reference</th>
                <th scope="col">Prosumer</th>
                <th scope="col">Node</th>
                <th scope="col">Scheduled</th>
                <th scope="col" className="text-end">Energy</th>
                <th scope="col">Status</th>
                <th scope="col" className="text-end">Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && <TableSkeleton columns={7} />}

              {!isLoading && reservations.length === 0 && (
                <tr>
                  <td colSpan={7} className="p-0">
                    <EmptyState
                      title="No reservations found"
                      message="Nothing matches these filters. Try widening the date range or clearing them."
                    />
                  </td>
                </tr>
              )}

              {!isLoading &&
                reservations.map((reservation) => (
                  <tr key={reservation.id}>
                    <td>
                      <div className="font-monospace small">{reservation.reservationNo}</div>
                      {reservation.hasQrCode && (
                        <span className="badge text-bg-light border small fw-normal">QR issued</span>
                      )}
                    </td>
                    <td>
                      <div className="d-flex align-items-center gap-2">
                        <Avatar name={reservation.prosumerName} small />
                        <div>
                          <div className="fw-semibold">{reservation.prosumerName}</div>
                          <div className="text-body-secondary font-monospace" style={{ fontSize: '0.75rem' }}>
                            {reservation.prosumerNic}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td className="small">
                      <div>{reservation.stationName}</div>
                      <div className="text-body-secondary font-monospace" style={{ fontSize: '0.75rem' }}>
                        {reservation.stationCode}
                      </div>
                    </td>
                    <td className="small">
                      <div>{new Date(reservation.reservationDateTime).toLocaleString()}</div>
                      <div className="text-body-secondary">
                        {reservation.hoursUntilReservation > 0
                          ? `in ${reservation.hoursUntilReservation.toFixed(1)} h`
                          : 'past'}
                      </div>
                    </td>
                    <td className="text-end small tabular">
                      {reservation.energyKwh} kWh
                      <div className="text-body-secondary">{reservation.direction}</div>
                    </td>
                    <td>
                      <StatusBadge status={reservation.status} />
                      {reservation.cancelReason && (
                        <div className="text-body-secondary small mt-1">{reservation.cancelReason}</div>
                      )}
                    </td>
                    <td className="text-end">
                      <div className="d-inline-flex gap-1 flex-wrap justify-content-end">
                        {reservation.status === 'Pending' && (
                          <button
                            type="button"
                            className="btn btn-success btn-sm"
                            onClick={() => {
                              setActionError('');
                              setPendingAction({ type: 'approve', reservation });
                            }}
                          >
                            Approve
                          </button>
                        )}

                        {reservation.status === 'Approved' && (
                          <button
                            type="button"
                            className="btn btn-primary btn-sm"
                            onClick={() => {
                              setActionError('');
                              setPendingAction({ type: 'complete', reservation });
                            }}
                          >
                            Complete
                          </button>
                        )}

                        {/* Shown only while the API says the rule still allows it. */}
                        {reservation.canBeModified && (
                          <button
                            type="button"
                            className="btn btn-outline-secondary btn-sm"
                            onClick={() => setFormState({ reservation })}
                          >
                            Change
                          </button>
                        )}

                        {reservation.canBeCancelled && (
                          <button
                            type="button"
                            className="btn btn-outline-danger btn-sm"
                            onClick={() => {
                              setActionError('');
                              setCancelReason('');
                              setPendingAction({ type: 'cancel', reservation });
                            }}
                          >
                            Cancel
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      </div>

      <p className="text-body-secondary small mt-3 mb-0">
        {reservations.length} reservation{reservations.length === 1 ? '' : 's'} found.
      </p>

      {formState && (
        <ReservationFormModal
          reservation={formState.reservation}
          onSave={handleSave}
          onCancel={() => setFormState(null)}
        />
      )}

      {pendingAction && (
        <ConfirmDialog
          title={
            pendingAction.type === 'approve'
              ? 'Approve booking'
              : pendingAction.type === 'complete'
                ? 'Complete energy transfer'
                : 'Cancel booking'
          }
          message={
            pendingAction.type === 'approve'
              ? `Approve ${pendingAction.reservation.reservationNo} for ${pendingAction.reservation.prosumerName}? A QR code will be issued for the transfer.`
              : pendingAction.type === 'complete'
                ? `Mark ${pendingAction.reservation.reservationNo} as completed? The QR code will be retired so it cannot be scanned again.`
                : `Cancel ${pendingAction.reservation.reservationNo}? The place is returned to the booking window.`
          }
          confirmLabel={
            pendingAction.type === 'approve'
              ? 'Approve'
              : pendingAction.type === 'complete'
                ? 'Complete'
                : 'Cancel booking'
          }
          confirmVariant={
            pendingAction.type === 'cancel'
              ? 'danger'
              : pendingAction.type === 'approve'
                ? 'success'
                : 'primary'
          }
          isBusy={isActionBusy}
          error={actionError}
          onConfirm={confirmAction}
          onCancel={() => {
            setPendingAction(null);
            setActionError('');
          }}
        >
          {pendingAction.type === 'cancel' && (
            <div>
              <label htmlFor="cancelReason" className="form-label small fw-semibold">
                Reason
              </label>
              <input
                id="cancelReason"
                className="form-control form-control-sm"
                placeholder="Why is this booking being cancelled?"
                value={cancelReason}
                onChange={(event) => setCancelReason(event.target.value)}
              />
              <div className="form-text small">Recorded against the booking for the audit trail.</div>
            </div>
          )}
        </ConfirmDialog>
      )}

    </div>
  );
}
