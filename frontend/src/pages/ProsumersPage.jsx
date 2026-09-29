/*
 * ---------------------------------------------------------------------------
 * File        : ProsumersPage.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : Vidvanga W A U (IT 23293694)
 * Created     : 2026-09-20
 * Description : Prosumer account management, including the queue of accounts
 *               awaiting activation.
 *
 * Pending view
 *               The Pending tab is the "pending activation" screen the
 *               assignment asks for. It is a filter on this page rather than a
 *               separate screen so an officer can activate an account and
 *               immediately see it move into Active, without navigating
 *               anywhere. It also has its own address, /prosumers/pending, so
 *               the dashboard tile can link straight to it.
 *
 * Business    : BR-5 is enforced by the API: only a Backoffice officer may
 *               activate. This page therefore only shows the Activate button
 *               to Backoffice users — but if a Grid Operator reached the
 *               endpoint another way, the API would still refuse with a 403.
 *               BR-6 shows up as the "Requested closure" flag: a prosumer can
 *               ask, and an officer here is the one who acts.
 * ---------------------------------------------------------------------------
 */

import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  activateProsumer,
  deactivateProsumer,
  getProsumers,
  registerProsumer,
} from '../api/prosumersApi';
import { useAuth } from '../auth/AuthContext';
import ConfirmDialog from '../components/ConfirmDialog';
import ProsumerFormModal from '../components/ProsumerFormModal';
import StatusBadge from '../components/StatusBadge';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import TableSkeleton from '../components/TableSkeleton';
import Avatar from '../components/Avatar';
import { IconPlus, IconRefresh } from '../components/Icons';
import { useToast } from '../components/ToastProvider';

const FILTERS = [
  { key: 'all', label: 'All', status: undefined },
  { key: 'pending', label: 'Pending activation', status: 'Pending' },
  { key: 'active', label: 'Active', status: 'Active' },
  { key: 'deactivated', label: 'Deactivated', status: 'Deactivated' },
];

export default function ProsumersPage() {
  // The address /prosumers/pending opens this page already filtered, so the
  // dashboard tile and the navigation menu can both link directly to it.
  const { filter: filterFromUrl } = useParams();
  const navigate = useNavigate();
  const { isBackoffice } = useAuth();
  const { showToast } = useToast();

  const [activeFilter, setActiveFilter] = useState(filterFromUrl ?? 'all');
  const [search, setSearch] = useState('');
  const [prosumers, setProsumers] = useState([]);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  // null = closed, { prosumer: null } = registering, { prosumer } = editing.
  const [formState, setFormState] = useState(null);

  // The action awaiting confirmation, or null when no dialog is open.
  const [pendingAction, setPendingAction] = useState(null);
  const [actionError, setActionError] = useState('');
  const [isActionBusy, setIsActionBusy] = useState(false);

  const load = useCallback(async (filterKey) => {
    setIsLoading(true);
    setError('');

    try {
      const status = FILTERS.find((item) => item.key === filterKey)?.status;
      setProsumers(await getProsumers(status));
    } catch (loadError) {
      setError(loadError.message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load(activeFilter);
  }, [activeFilter, load]);

  function changeFilter(key) {
    setActiveFilter(key);
    // Keep the address in step so the page can be refreshed or shared.
    navigate(key === 'pending' ? '/prosumers/pending' : '/prosumers', { replace: true });
  }

  // Searching is done here rather than by the API because the back office
  // holds a small number of prosumers and filtering an already loaded list is
  // instant. Booking history, which can grow without limit, is searched on the
  // server instead.
  const visibleProsumers = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return prosumers;

    return prosumers.filter(
      (prosumer) =>
        prosumer.nic.toLowerCase().includes(term) ||
        prosumer.fullName.toLowerCase().includes(term) ||
        prosumer.email.toLowerCase().includes(term),
    );
  }, [prosumers, search]);

  async function handleSave(payload) {
    // Registration only. Editing an existing prosumer is not offered here,
    // because their details are theirs to change from their own profile page.
    // Errors propagate to the form modal, which shows them beside the fields.
    await registerProsumer(payload);

    showToast('Prosumer registered and awaiting activation.');

    setFormState(null);
    await load(activeFilter);
  }

  async function confirmAction() {
    setIsActionBusy(true);
    setActionError('');

    try {
      if (pendingAction.type === 'activate') {
        await activateProsumer(pendingAction.prosumer.nic);
        showToast(`${pendingAction.prosumer.fullName} can now sign in.`);
      } else {
        await deactivateProsumer(pendingAction.prosumer.nic);
        showToast(`${pendingAction.prosumer.fullName} has been deactivated.`);
      }

      setPendingAction(null);
      await load(activeFilter);
    } catch (confirmError) {
      // Shown inside the dialog, next to what the user was trying to do.
      setActionError(confirmError.message);
    } finally {
      setIsActionBusy(false);
    }
  }

  const pendingCount = prosumers.filter((p) => p.status === 'Pending').length;

  return (
    <div>
      <PageHeader
        title="Prosumers"
        subtitle="Solar prosumers register on the mobile application and must be activated by a Backoffice officer before they can sign in."
        actions={
          <>
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm d-flex align-items-center gap-2"
              onClick={() => load(activeFilter)}
            >
              <IconRefresh />
              Refresh
            </button>
            <button
              type="button"
              className="btn btn-primary btn-sm d-flex align-items-center gap-2"
              onClick={() => setFormState({ prosumer: null })}
            >
              <IconPlus />
              Register prosumer
            </button>
          </>
        }
      />

      {/* --- Filters and search ------------------------------------------- */}
      <div className="d-flex flex-wrap gap-3 align-items-center mb-3">
        <ul className="nav nav-pills flex-wrap gap-1 mb-0">
          {FILTERS.map((item) => (
            <li className="nav-item" key={item.key}>
              <button
                type="button"
                className={`nav-link py-1 px-3 small${activeFilter === item.key ? ' active' : ''}`}
                onClick={() => changeFilter(item.key)}
              >
                {item.label}
                {item.key === 'pending' && activeFilter === 'all' && pendingCount > 0 && (
                  <span className="badge text-bg-warning ms-2">{pendingCount}</span>
                )}
              </button>
            </li>
          ))}
        </ul>

        <div className="ms-auto" style={{ minWidth: '16rem' }}>
          <input
            type="search"
            className="form-control form-control-sm"
            placeholder="Search by NIC, name or email"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            aria-label="Search prosumers"
          />
        </div>
      </div>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      {/* --- Table --------------------------------------------------------- */}
      <div className="app-card">
        <div className="table-responsive">
          <table className="table app-table">
            <thead className="table-light">
              <tr>
                <th scope="col">NIC</th>
                <th scope="col">Name</th>
                <th scope="col">Contact</th>
                <th scope="col" className="text-end">Array (kW)</th>
                <th scope="col">Status</th>
                <th scope="col" className="text-end">Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && <TableSkeleton columns={6} />}

              {!isLoading && visibleProsumers.length === 0 && (
                <tr>
                  <td colSpan={6} className="p-0">
                    <EmptyState
                      title={search ? 'No matches' : 'Nothing here yet'}
                      message={
                        search
                          ? 'No prosumer matches that NIC, name or email address.'
                          : 'No prosumer accounts in this category.'
                      }
                    />
                  </td>
                </tr>
              )}

              {!isLoading &&
                visibleProsumers.map((prosumer) => (
                  <tr key={prosumer.nic}>
                    <td className="font-monospace small">{prosumer.nic}</td>
                    <td>
                      <div className="d-flex align-items-center gap-2">
                        <Avatar name={prosumer.fullName} small />
                        <div>
                          <div className="fw-semibold">{prosumer.fullName}</div>
                          <div className="text-body-secondary small">{prosumer.address}</div>
                        </div>
                      </div>
                    </td>
                    <td className="small">
                      <div>{prosumer.email}</div>
                      <div className="text-body-secondary">{prosumer.phone}</div>
                    </td>
                    <td className="text-end tabular">
                      {prosumer.solarCapacityKw}
                    </td>
                    <td>
                      <StatusBadge status={prosumer.status} />
                      {prosumer.deactivationRequested && (
                        <div className="small text-warning-emphasis fw-semibold mt-1">
                          Requested closure
                        </div>
                      )}
                    </td>
                    <td className="text-end">
                      <div className="d-inline-flex gap-1">
                        {/* No Edit button. A prosumer's personal details belong
                            to them and are changed from their own profile page.
                            Staff administer the account's LIFECYCLE only, which
                            is what the two buttons below do. The API enforces
                            this: an officer calling the update endpoint for
                            somebody else receives 403. */}

                        {/* BR-5: only a Backoffice officer may activate. */}
                        {isBackoffice && prosumer.status !== 'Active' && (
                          <button
                            type="button"
                            className="btn btn-success btn-sm"
                            onClick={() => {
                              setActionError('');
                              setPendingAction({ type: 'activate', prosumer });
                            }}
                          >
                            Activate
                          </button>
                        )}

                        {prosumer.status !== 'Deactivated' && (
                          <button
                            type="button"
                            className="btn btn-outline-danger btn-sm"
                            onClick={() => {
                              setActionError('');
                              setPendingAction({ type: 'deactivate', prosumer });
                            }}
                          >
                            Deactivate
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
        Showing {visibleProsumers.length} of {prosumers.length} account
        {prosumers.length === 1 ? '' : 's'}.
      </p>

      {formState && (
        <ProsumerFormModal
          prosumer={formState.prosumer}
          onSave={handleSave}
          onCancel={() => setFormState(null)}
        />
      )}

      {/* --- Confirmation --------------------------------------------------- */}
      {pendingAction && (
        <ConfirmDialog
          title={pendingAction.type === 'activate' ? 'Activate account' : 'Deactivate account'}
          message={
            pendingAction.type === 'activate'
              ? `Activate ${pendingAction.prosumer.fullName} (${pendingAction.prosumer.nic})? They will be able to sign in to the mobile application straight away.`
              : `Deactivate ${pendingAction.prosumer.fullName} (${pendingAction.prosumer.nic})? They will be unable to sign in, and only a Backoffice officer can reverse this.`
          }
          confirmLabel={pendingAction.type === 'activate' ? 'Activate' : 'Deactivate'}
          confirmVariant={pendingAction.type === 'activate' ? 'success' : 'danger'}
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
