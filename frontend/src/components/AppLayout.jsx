/*
 * ---------------------------------------------------------------------------
 * File        : AppLayout.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : The frame every signed in screen sits inside: a fixed sidebar
 *               for navigation, a top bar showing where you are, and the
 *               current page rendered into the content area.
 *
 * Why a sidebar rather than a top bar
 *               This is a tool someone works in, moving between nodes,
 *               bookings and accounts repeatedly. A sidebar keeps every section
 *               visible and one click away, and shows at a glance which one is
 *               open. A top bar hides that behind a menu on narrow screens and
 *               gives the eye no stable anchor.
 *
 * Responsive  : Below large screens the sidebar slides off and is opened by the
 *               menu button, with a scrim behind it. The interface therefore
 *               still works on a tablet, which matters because a Grid Operator
 *               may well be using one at a node.
 *
 * Note        : The menu is built from the signed in role, so a Grid Operator
 *               is never shown a link the API would refuse them. As set out in
 *               ProtectedRoute, this shapes the interface; the API enforces it.
 * ---------------------------------------------------------------------------
 */

import { useEffect, useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { ROLES } from '../config';
import Avatar from './Avatar';
import {
  IconCalendar,
  IconGrid,
  IconMenu,
  IconPin,
  IconShieldUser,
  IconSignOut,
  IconSun,
  IconUsers,
} from './Icons';

export default function AppLayout() {
  const { auth, isBackoffice, signOut } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  // Close the sidebar whenever the page changes, otherwise on a phone it stays
  // covering the screen the user just navigated to.
  useEffect(() => {
    setIsSidebarOpen(false);
  }, [location.pathname]);

  function handleSignOut() {
    signOut();
    navigate('/login', { replace: true });
  }

  const navItems = [
    { to: '/dashboard', label: 'Dashboard', icon: <IconGrid /> },
    { to: '/stations', label: 'Microgrid Nodes', icon: <IconPin /> },
    { to: '/reservations', label: 'Reservations', icon: <IconCalendar /> },
    { to: '/prosumers', label: 'Prosumers', icon: <IconUsers /> },
  ];

  // Staff administration is a Backoffice duty, so operators are not offered it.
  if (isBackoffice) {
    navItems.push({ to: '/users', label: 'Staff Users', icon: <IconShieldUser /> });
  }

  const currentSection =
    navItems.find((item) => location.pathname.startsWith(item.to))?.label ?? '';

  return (
    <div className="app-shell">
      {/* --- Sidebar ------------------------------------------------------ */}
      <aside className={`app-sidebar${isSidebarOpen ? ' is-open' : ''}`}>
        {/* The brand goes to the public home page, which is where a user
            expects a logo to take them. The dashboard has its own entry in
            the menu directly below. */}
        <Link to="/" className="app-sidebar-brand" title="Go to the public home page">
          <span className="text-warning">
            <IconSun size={20} />
          </span>
          <span className="fw-semibold" style={{ fontSize: '0.95rem', lineHeight: 1.15 }}>
            Smart Solar
            <br />
            <span style={{ fontWeight: 400, opacity: 0.7, fontSize: '0.8rem' }}>Microgrid</span>
          </span>
        </Link>

        <nav className="app-nav">
          <div className="app-nav-label">Operations</div>
          {navItems.slice(0, 4).map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => `app-nav-link${isActive ? ' active' : ''}`}
            >
              {item.icon}
              {item.label}
            </NavLink>
          ))}

          {isBackoffice && (
            <>
              <div className="app-nav-label">Administration</div>
              <NavLink
                to="/users"
                className={({ isActive }) => `app-nav-link${isActive ? ' active' : ''}`}
              >
                <IconShieldUser />
                Staff Users
              </NavLink>
            </>
          )}
        </nav>

        <div className="app-sidebar-user">
          {/* The whole block is a link to My Account, which is where people
              look for their own settings. */}
          <NavLink
            to="/profile"
            className={({ isActive }) =>
              `app-nav-link mb-2${isActive ? ' active' : ''}`
            }
          >
            <Avatar name={auth?.fullName} />
            <span className="flex-grow-1 min-w-0">
              <span className="d-block fw-semibold text-truncate" style={{ fontSize: '0.85rem' }}>
                {auth?.fullName}
              </span>
              <span className="d-block" style={{ fontSize: '0.72rem', opacity: 0.7 }}>
                {formatRole(auth?.role)}
              </span>
            </span>
          </NavLink>

          <button
            type="button"
            className="btn btn-outline-light btn-sm w-100 d-flex align-items-center justify-content-center gap-2"
            onClick={handleSignOut}
          >
            <IconSignOut />
            Sign out
          </button>
        </div>
      </aside>

      {/* Tapping outside the open sidebar closes it, which is what a phone
          user expects instead of hunting for the button again. */}
      {isSidebarOpen && (
        <div
          className="app-scrim d-lg-none"
          onClick={() => setIsSidebarOpen(false)}
          aria-hidden="true"
        />
      )}

      {/* --- Main column --------------------------------------------------- */}
      <div className="app-main">
        <header className="app-topbar">
          <button
            type="button"
            className="btn btn-sm btn-outline-secondary d-lg-none"
            onClick={() => setIsSidebarOpen((open) => !open)}
            aria-label="Toggle navigation"
            aria-expanded={isSidebarOpen}
          >
            <IconMenu />
          </button>

          <div className="flex-grow-1">
            <span className="text-body-secondary small">Smart Solar Microgrid</span>
            {currentSection && (
              <>
                <span className="text-body-secondary small mx-2">/</span>
                <span className="small fw-semibold">{currentSection}</span>
              </>
            )}
          </div>

          <Link to="/profile" className="btn btn-sm btn-outline-secondary d-flex align-items-center gap-2">
            <Avatar name={auth?.fullName} small />
            <span className="d-none d-sm-inline">My Account</span>
          </Link>
        </header>

        <main className="app-content">
          <Outlet />
        </main>
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
