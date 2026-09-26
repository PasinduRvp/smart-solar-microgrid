/*
 * ---------------------------------------------------------------------------
 * File        : UsersPage.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Staff account management: the list of Backoffice officers and
 *               Grid Operators, and the actions to create, edit and remove
 *               them. Backoffice officers only.
 *
 * Deletion    : The API refuses three cases, and each refusal is shown here as
 *               it arrives rather than being predicted in the browser:
 *                 - a prosumer cannot be deleted, only deactivated, because
 *                   their bookings still refer to them;
 *                 - an officer cannot delete their own account, which would
 *                   lock them out mid-session;
 *                 - the last Backoffice account cannot be deleted, which would
 *                   leave nobody able to administer the system.
 *               The third is the interesting one at a viva: it is a rule about
 *               the system as a whole rather than about one record, which is
 *               exactly why it belongs in the service and not in a screen.
 * ---------------------------------------------------------------------------
 */

import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  createStaffUser,
  deleteStaffUser,
  getStaffUsers,
} from '../api/usersApi';
import { useAuth } from '../auth/AuthContext';
import ConfirmDialog from '../components/ConfirmDialog';
import StaffUserFormModal from '../components/StaffUserFormModal';
import StatusBadge from '../components/StatusBadge';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import TableSkeleton from '../components/TableSkeleton';
import Avatar from '../components/Avatar';
import { IconPlus, IconRefresh } from '../components/Icons';
import { useToast } from '../components/ToastProvider';

export default function UsersPage() {
  const { auth } = useAuth();
  const { showToast } = useToast();

  const [users, setUsers] = useState([]);
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  // null = closed, { user: null } = creating, { user } = editing.
  const [formState, setFormState] = useState(null);
  const [userToDelete, setUserToDelete] = useState(null);
  const [deleteError, setDeleteError] = useState('');
  const [isDeleting, setIsDeleting] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError('');

    try {
      setUsers(await getStaffUsers());
    } catch (loadError) {
      setError(loadError.message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const visibleUsers = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return users;

    return users.filter(
      (user) =>
        user.fullName.toLowerCase().includes(term) ||
        user.email.toLowerCase().includes(term) ||
        user.nic.toLowerCase().includes(term),
    );
  }, [users, search]);

  async function handleSave(payload) {
    // Creation only. A colleague's details are theirs to edit, from their own
    // profile page. Errors are deliberately allowed to propagate: the form
    // modal catches them and shows the message beside the offending field.
    await createStaffUser(payload);

    showToast('Staff account created.');
    setFormState(null);
    await load();
  }

  async function confirmDelete() {
    setIsDeleting(true);
    setDeleteError('');

    try {
      await deleteStaffUser(userToDelete.id);
      showToast(userToDelete.fullName + ' was removed.');
      setUserToDelete(null);
      await load();
    } catch (confirmError) {
      setDeleteError(confirmError.message);
    } finally {
      setIsDeleting(false);
    }
  }

  return (
    <div>
      <PageHeader
        title="Staff Users"
        subtitle="Backoffice officers and Grid Operators who can use the web application."
        actions={
          <>
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm d-flex align-items-center gap-2"
              onClick={load}
            >
              <IconRefresh />
              Refresh
            </button>
            <button
              type="button"
              className="btn btn-primary btn-sm d-flex align-items-center gap-2"
              onClick={() => setFormState({ user: null })}
            >
              <IconPlus />
              New staff account
            </button>
          </>
        }
      />

      <div className="d-flex justify-content-end mb-3">
        <input
          type="search"
          className="form-control form-control-sm"
          style={{ maxWidth: '18rem' }}
          placeholder="Search by name, email or NIC"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          aria-label="Search staff users"
        />
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
                <th scope="col">NIC</th>
                <th scope="col">Name</th>
                <th scope="col">Email</th>
                <th scope="col">Role</th>
                <th scope="col">Status</th>
                <th scope="col" className="text-end">Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && <TableSkeleton columns={6} />}

              {!isLoading && visibleUsers.length === 0 && (
                <tr>
                  <td colSpan={6} className="p-0">
                    <EmptyState
                      title={search ? 'No matches' : 'No staff accounts yet'}
                      message={
                        search
                          ? 'No account matches that name, email or NIC.'
                          : 'Create the first Backoffice or Grid Operator account.'
                      }
                    />
                  </td>
                </tr>
              )}

              {!isLoading &&
                visibleUsers.map((user) => {
                  const isSelf = user.id === auth?.userId;

                  return (
                    <tr key={user.id}>
                      <td className="font-monospace small">{user.nic}</td>
                      <td>
                        <span className="d-inline-flex align-items-center gap-2">
                          <Avatar name={user.fullName} small />
                          <span className="fw-semibold">{user.fullName}</span>
                          {isSelf && (
                            <span className="badge text-bg-light border fw-normal">You</span>
                          )}
                        </span>
                      </td>
                      <td className="small">{user.email}</td>
                      <td>
                        <span
                          className={`badge ${
                            user.role === 'Backoffice' ? 'text-bg-dark' : 'text-bg-info'
                          }`}
                        >
                          {user.role === 'Backoffice' ? 'Backoffice' : 'Grid Operator'}
                        </span>
                      </td>
                      <td>
                        <StatusBadge status={user.status} />
                      </td>
                      <td className="text-end">
                        <div className="d-inline-flex gap-1">
                          {/* Editing is offered only on your own row, and
                              it opens your profile page. Personal details
                              belong to the person they describe, so even a
                              Backoffice officer cannot rewrite a colleague's
                              name — the API returns 403 if they try. */}
                          {isSelf && (
                            <Link to="/profile" className="btn btn-outline-secondary btn-sm">
                              Edit mine
                            </Link>
                          )}
                          <button
                            type="button"
                            className="btn btn-outline-danger btn-sm"
                            onClick={() => {
                              setDeleteError('');
                              setUserToDelete(user);
                            }}
                            // Deleting your own account is refused by the API
                            // anyway; disabling it here avoids offering an
                            // action that can only fail.
                            disabled={isSelf}
                            title={isSelf ? 'You cannot delete your own account' : undefined}
                          >
                            Delete
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
            </tbody>
          </table>
        </div>
      </div>

      <p className="text-body-secondary small mt-3 mb-0">
        Showing {visibleUsers.length} of {users.length} account{users.length === 1 ? '' : 's'}.
      </p>

      {formState && (
        <StaffUserFormModal
          user={formState.user}
          onSave={handleSave}
          onCancel={() => setFormState(null)}
        />
      )}

      {userToDelete && (
        <ConfirmDialog
          title="Delete staff account"
          message={`Permanently delete ${userToDelete.fullName} (${userToDelete.email})? This cannot be undone.`}
          confirmLabel="Delete"
          confirmVariant="danger"
          isBusy={isDeleting}
          error={deleteError}
          onConfirm={confirmDelete}
          onCancel={() => {
            setUserToDelete(null);
            setDeleteError('');
          }}
        />
      )}
    </div>
  );
}
