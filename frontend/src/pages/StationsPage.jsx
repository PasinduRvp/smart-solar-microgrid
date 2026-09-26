/*
 * ---------------------------------------------------------------------------
 * File        : StationsPage.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Microgrid node management: the list of nodes with their GPS
 *               position and capacity, and the actions to register, edit and
 *               take one out of service.
 *
 * BR-4 in the interface
 *               Deactivation is offered for every node in service, including
 *               ones that turn out to have live bookings. That is deliberate.
 *               The browser does not know how many active reservations a node
 *               holds without asking, and any count it fetched could be stale
 *               by the time the button was pressed. So the request is sent, and
 *               when the service refuses — "3 active reservation(s) must be
 *               cancelled or completed before node MG-COL-001 can be
 *               deactivated" — that sentence is shown in the dialog. The rule
 *               and its count both come from the one place that can answer
 *               correctly.
 * ---------------------------------------------------------------------------
 */

import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  createStation,
  deactivateStation,
  getStations,
  reactivateStation,
  updateStation,
} from '../api/stationsApi';
import { useAuth } from '../auth/AuthContext';
import ConfirmDialog from '../components/ConfirmDialog';
import StationFormModal from '../components/StationFormModal';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import TableSkeleton from '../components/TableSkeleton';
import { IconPlus, IconRefresh } from '../components/Icons';
import { useToast } from '../components/ToastProvider';

export default function StationsPage() {
  const { isBackoffice } = useAuth();
  const { showToast } = useToast();

  const [stations, setStations] = useState([]);
  const [showActiveOnly, setShowActiveOnly] = useState(false);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  const [formState, setFormState] = useState(null);
  const [pendingAction, setPendingAction] = useState(null);
  const [actionError, setActionError] = useState('');
  const [isActionBusy, setIsActionBusy] = useState(false);

  const load = useCallback(async (activeOnly) => {
    setIsLoading(true);
    setError('');

    try {
      setStations(await getStations(activeOnly));
    } catch (loadError) {
      setError(loadError.message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load(showActiveOnly);
  }, [showActiveOnly, load]);

  async function handleSave(payload) {
    if (formState.station) {
      await updateStation(formState.station.id, payload);
    } else {
      await createStation(payload);
    }

    showToast(formState.station ? 'Node updated.' : 'Microgrid node registered.');
    setFormState(null);
    await load(showActiveOnly);
  }

  async function confirmAction() {
    setIsActionBusy(true);
    setActionError('');

    try {
      if (pendingAction.type === 'deactivate') {
        await deactivateStation(pendingAction.station.id);
        showToast(pendingAction.station.stationCode + ' is out of service.');
      } else {
        await reactivateStation(pendingAction.station.id);
        showToast(pendingAction.station.stationCode + ' is back in service.');
      }

      setPendingAction(null);
      await load(showActiveOnly);
    } catch (confirmError) {
      // This is where BR-4's refusal appears, naming how many bookings block it.
      setActionError(confirmError.message);
    } finally {
      setIsActionBusy(false);
    }
  }

  return (
    <div>
      <PageHeader
        title="Microgrid Nodes"
        subtitle="Solar grid hubs, their GPS location, generating capacity and battery storage."
        actions={
          <>
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm d-flex align-items-center gap-2"
              onClick={() => load(showActiveOnly)}
            >
              <IconRefresh />
              Refresh
            </button>
            {isBackoffice && (
              <button
                type="button"
                className="btn btn-primary btn-sm d-flex align-items-center gap-2"
                onClick={() => setFormState({ station: null })}
              >
                <IconPlus />
                Register node
              </button>
            )}
          </>
        }
      />

      <div className="form-check form-switch mb-3">
        <input
          className="form-check-input"
          type="checkbox"
          id="activeOnly"
          checked={showActiveOnly}
          onChange={(event) => setShowActiveOnly(event.target.checked)}
        />
        <label className="form-check-label small" htmlFor="activeOnly">
          Show only nodes in service
        </label>
      </div>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      <div className="app-card">
        <div className="table-responsive">
          <table className="table app-table">
            <thead className="table-light">
              <tr>
                <th scope="col">Code</th>
                <th scope="col">Node</th>
                <th scope="col">Location</th>
                <th scope="col" className="text-end">Capacity</th>
                <th scope="col" className="text-end">Battery slots</th>
                <th scope="col">State</th>
                <th scope="col" className="text-end">Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && <TableSkeleton columns={7} />}

              {!isLoading && stations.length === 0 && (
                <tr>
                  <td colSpan={7} className="p-0">
                    <EmptyState
                      title="No microgrid nodes"
                      message="Register the first solar grid hub with its GPS location and capacity."
                    />
                  </td>
                </tr>
              )}

              {!isLoading &&
                stations.map((station) => (
                  <tr key={station.id}>
                    <td className="font-monospace small">{station.stationCode}</td>
                    <td>
                      <Link to={`/stations/${station.id}`} className="fw-semibold text-decoration-none">
                        {station.name}
                      </Link>
                      <div className="text-body-secondary small">
                        {station.schedule.length} day
                        {station.schedule.length === 1 ? '' : 's'} open per week
                      </div>
                    </td>
                    <td className="small">
                      <div>{station.location.addressLine}</div>
                      <div className="text-body-secondary font-monospace" style={{ fontSize: '0.75rem' }}>
                        {station.location.latitude.toFixed(4)}, {station.location.longitude.toFixed(4)}
                      </div>
                    </td>
                    <td className="text-end tabular">
                      {station.capacityKwh}
                      <span className="text-body-secondary small"> kW/h</span>
                    </td>
                    <td className="text-end tabular">
                      {station.availableBatterySlots}
                      <span className="text-body-secondary"> / {station.totalBatterySlots}</span>
                    </td>
                    <td>
                      {station.isActive ? (
                        <span className="badge text-bg-success">In service</span>
                      ) : (
                        <span className="badge text-bg-secondary">Deactivated</span>
                      )}
                    </td>
                    <td className="text-end">
                      <div className="d-inline-flex gap-1">
                        <Link to={`/stations/${station.id}`} className="btn btn-outline-secondary btn-sm">
                          Windows
                        </Link>

                        {isBackoffice && (
                          <>
                            <button
                              type="button"
                              className="btn btn-outline-secondary btn-sm"
                              onClick={() => setFormState({ station })}
                            >
                              Edit
                            </button>

                            {station.isActive ? (
                              <button
                                type="button"
                                className="btn btn-outline-danger btn-sm"
                                onClick={() => {
                                  setActionError('');
                                  setPendingAction({ type: 'deactivate', station });
                                }}
                              >
                                Deactivate
                              </button>
                            ) : (
                              <button
                                type="button"
                                className="btn btn-outline-success btn-sm"
                                onClick={() => {
                                  setActionError('');
                                  setPendingAction({ type: 'reactivate', station });
                                }}
                              >
                                Reactivate
                              </button>
                            )}
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      </div>

      {formState && (
        <StationFormModal
          station={formState.station}
          onSave={handleSave}
          onCancel={() => setFormState(null)}
        />
      )}

      {pendingAction && (
        <ConfirmDialog
          title={
            pendingAction.type === 'deactivate' ? 'Take node out of service' : 'Return node to service'
          }
          message={
            pendingAction.type === 'deactivate'
              ? `Deactivate ${pendingAction.station.stationCode} — ${pendingAction.station.name}? It will stop accepting new bookings. Nodes holding active reservations cannot be deactivated.`
              : `Return ${pendingAction.station.stationCode} — ${pendingAction.station.name} to service? Prosumers will be able to book it again.`
          }
          confirmLabel={pendingAction.type === 'deactivate' ? 'Deactivate' : 'Reactivate'}
          confirmVariant={pendingAction.type === 'deactivate' ? 'danger' : 'success'}
          isBusy={isActionBusy}
          error={actionError}
          onConfirm={confirmAction}
          onCancel={() => {
            setPendingAction(null);
            setActionError('');
          }}
        />
      )}
    </div>
  );
}
