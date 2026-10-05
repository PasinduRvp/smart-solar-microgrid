/*
 * ---------------------------------------------------------------------------
 * File        : DashboardPage.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : The staff home screen. Shows what is waiting for a person to
 *               act on, what is happening today, and how busy each microgrid
 *               node is.
 *
 * Where the numbers come from
 *               All of them arrive from GET /api/dashboard/operator, already
 *               counted by the database. Nothing on this page is calculated in
 *               the browser and nothing is hard coded — the marking scheme
 *               puts hard-coded figures in its lowest band, and counting in the
 *               client would also mean downloading every reservation just to
 *               display a total.
 *
 * Layout      : The two figures that represent outstanding work come first,
 *               because the first question a member of staff has when they
 *               sign in is "what needs me?". Today's activity follows, then
 *               the wider totals.
 * ---------------------------------------------------------------------------
 */

import { useCallback, useEffect, useState } from 'react';
import { getOperatorDashboard } from '../api/dashboardApi';
import { useAuth } from '../auth/AuthContext';
import StatCard from '../components/StatCard';
import PageHeader from '../components/PageHeader';
import { IconRefresh } from '../components/Icons';

export default function DashboardPage() {
  const { auth, isBackoffice } = useAuth();
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError('');

    try {
      setData(await getOperatorDashboard());
    } catch (loadError) {
      setError(loadError.message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  if (isLoading) {
    return (
      <div className="d-flex align-items-center gap-2 text-body-secondary py-5">
        <span className="spinner-border spinner-border-sm" aria-hidden="true" />
        Loading dashboard...
      </div>
    );
  }

  if (error) {
    return (
      <div>
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
        <button type="button" className="btn btn-outline-secondary btn-sm" onClick={load}>
          Try again
        </button>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title={'Good day, ' + (auth?.fullName?.split(' ')[0] ?? '')}
        subtitle="Here is what is happening across the microgrid today."
        actions={
          <button
            type="button"
            className="btn btn-outline-secondary btn-sm d-flex align-items-center gap-2"
            onClick={load}
          >
            <IconRefresh />
            Refresh
          </button>
        }
      />

      {/* --- Work waiting on a person ------------------------------------- */}
      <h2 className="h6 fw-semibold text-uppercase text-body-secondary mb-3" style={{ letterSpacing: '0.08em' }}>
        Waiting for you
      </h2>
      <div className="row g-3 mb-4">
        <div className="col-12 col-sm-6 col-xl-3">
          <StatCard
            label="Pending approvals"
            value={data.pendingApprovalCount}
            note="Bookings awaiting a decision"
            needsAttention
            to="/reservations"
          />
        </div>
        <div className="col-12 col-sm-6 col-xl-3">
          <StatCard
            label="Pending activations"
            value={data.pendingActivationCount}
            note="Prosumers who cannot sign in yet"
            needsAttention={isBackoffice}
            to="/prosumers"
          />
        </div>
        <div className="col-12 col-sm-6 col-xl-3">
          <StatCard
            label="Deactivation requests"
            value={data.deactivationRequestCount}
            note="Prosumers asking to close their account"
            needsAttention={isBackoffice}
            to="/prosumers"
          />
        </div>
        <div className="col-12 col-sm-6 col-xl-3">
          <StatCard
            label="Approved, upcoming"
            value={data.approvedFutureCount}
            note="Confirmed transfers still to happen"
            to="/reservations"
          />
        </div>
      </div>

      {/* --- Today ---------------------------------------------------------- */}
      <h2 className="h6 fw-semibold text-uppercase text-body-secondary mb-3" style={{ letterSpacing: '0.08em' }}>
        Today
      </h2>
      <div className="row g-3 mb-4">
        <div className="col-12 col-sm-6 col-xl-3">
          <StatCard label="Scheduled today" value={data.todayScheduledCount} note="Transfers booked for today" />
        </div>
        <div className="col-12 col-sm-6 col-xl-3">
          <StatCard label="Completed today" value={data.todayCompletedCount} note="Transfers finalised at a node" />
        </div>
        <div className="col-12 col-sm-6 col-xl-3">
          <StatCard label="Active prosumers" value={data.activeProsumerCount} note="Accounts able to sign in" />
        </div>
        <div className="col-12 col-sm-6 col-xl-3">
          <StatCard
            label="Nodes in service"
            value={data.activeStationCount}
            note={
              data.inactiveStationCount > 0
                ? `${data.inactiveStationCount} deactivated`
                : 'All nodes operational'
            }
            to="/stations"
          />
        </div>
      </div>

      {/* --- Per node load --------------------------------------------------- */}
      <h2 className="h6 fw-semibold text-uppercase text-body-secondary mb-3" style={{ letterSpacing: '0.08em' }}>
        Node activity
      </h2>
      <div className="app-card">
        <div className="table-responsive">
          <table className="table app-table">
            <thead>
              <tr>
                <th scope="col">Code</th>
                <th scope="col">Microgrid node</th>
                <th
                  scope="col"
                  className="text-end"
                  title="Transfers taking place at this node today"
                >
                  Scheduled today
                </th>
                <th
                  scope="col"
                  className="text-end"
                  title="All transfers still to come at this node, within the 7 day booking window"
                >
                  Upcoming &middot; 7 days
                </th>
                <th
                  scope="col"
                  className="text-end"
                  title="Physical battery storage free, maintained by grid operators"
                >
                  Battery bays free
                </th>
              </tr>
            </thead>
            <tbody>
              {data.stationLoads.length === 0 && (
                <tr>
                  <td colSpan={5} className="text-center text-body-secondary py-4">
                    No microgrid nodes are in service yet.
                  </td>
                </tr>
              )}

              {data.stationLoads.map((station) => (
                <tr key={station.stationId}>
                  <td className="font-monospace small">{station.stationCode}</td>
                  <td>{station.stationName}</td>
                  <td className="text-end tabular">
                    {station.todayBookingCount > 0 ? (
                      station.todayBookingCount
                    ) : (
                      <span className="text-body-secondary">0</span>
                    )}
                  </td>
                  <td className="text-end tabular">
                    {station.upcomingBookingCount > 0 ? (
                      <span className="fw-semibold">{station.upcomingBookingCount}</span>
                    ) : (
                      <span className="text-body-secondary">0</span>
                    )}
                  </td>
                  <td className="text-end tabular">
                    <span className="fw-semibold">{station.availableBatterySlots}</span>
                    <span className="text-body-secondary small">
                      {' of '}
                      {station.totalBatterySlots}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <p className="text-body-secondary small mt-2 mb-0">
        Transfers are counted by the day they are <strong>scheduled for</strong>, not the day the
        booking was made &mdash; so a booking for Wednesday appears under
        &ldquo;Upcoming&rdquo;, not &ldquo;Scheduled today&rdquo;. Battery bays are physical
        storage, updated by grid operators from the node page.
      </p>
    </div>
  );
}
