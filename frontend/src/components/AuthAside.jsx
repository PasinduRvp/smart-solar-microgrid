/*
 * ---------------------------------------------------------------------------
 * File        : AuthAside.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : The dark panel beside the sign in and registration forms,
 *               carrying the brand and a short reminder of what the system
 *               does and which rules govern a booking.
 *
 * Why it is here
 *               A bare form on an empty page tells a visitor nothing about
 *               where they have arrived. The panel keeps the system's identity
 *               and its key rules visible while somebody types, without
 *               crowding the form itself.
 *
 * Hidden on phones
 *               Below 992px the panel is not rendered at all. On a small
 *               screen it would push the form below the fold and force a user
 *               to scroll before they could type, which is exactly the wrong
 *               trade on the screen people are most often in a hurry on.
 * ---------------------------------------------------------------------------
 */

import { Link } from 'react-router-dom';
import { IconCalendar, IconPin, IconShieldUser, IconSun } from './Icons';

/**
 * @param {object} props
 * @param {string} props.heading Short line describing the page's purpose.
 * @param {Array}  props.points  The three reminders shown down the panel.
 */
export default function AuthAside({ heading, points }) {
  return (
    <aside className="auth-aside">
      <Link to="/" className="d-flex align-items-center gap-2 text-decoration-none text-white">
        <span className="text-warning">
          <IconSun size={22} />
        </span>
        <span className="fw-semibold">Smart Solar Microgrid</span>
      </Link>

      <div className="py-4">
        <h2 className="h3 fw-semibold mb-4" style={{ maxWidth: '20ch' }}>
          {heading}
        </h2>

        {points.map((point) => (
          <div className="auth-aside-point" key={point.title}>
            <span className="auth-aside-point-icon">{point.icon}</span>
            <div>
              <div className="fw-semibold" style={{ fontSize: '0.92rem' }}>
                {point.title}
              </div>
              <div style={{ fontSize: '0.82rem', color: 'rgba(247,243,236,0.66)' }}>
                {point.body}
              </div>
            </div>
          </div>
        ))}
      </div>

      <div style={{ fontSize: '0.78rem', color: 'rgba(247,243,236,0.5)' }}>
        SE4040 Enterprise Application Development &middot; Assignment 1
      </div>
    </aside>
  );
}

/** The reminders shown on the sign in page. */
export const SIGN_IN_POINTS = [
  {
    icon: <IconShieldUser size={16} />,
    title: 'Role based access',
    body: 'Backoffice officers and Grid Operators see different tools.',
  },
  {
    icon: <IconPin size={16} />,
    title: 'Every node in one place',
    body: 'Locations, capacity, schedules and battery availability.',
  },
  {
    icon: <IconCalendar size={16} />,
    title: 'Bookings and approvals',
    body: 'Review, approve and verify energy transfers.',
  },
];

/** The reminders shown on the registration page. */
export const REGISTER_POINTS = [
  {
    icon: <IconPin size={16} />,
    title: 'Find a node near you',
    body: 'See nearby microgrid hubs and what they can take.',
  },
  {
    icon: <IconCalendar size={16} />,
    title: 'Book within seven days',
    body: 'Change or cancel freely up to twelve hours before.',
  },
  {
    icon: <IconShieldUser size={16} />,
    title: 'Verified transfers',
    body: 'An approved booking issues a QR code checked at the node.',
  },
];
