/*
 * ---------------------------------------------------------------------------
 * File        : Icons.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : The small set of line icons used across the interface, drawn
 *               as inline SVG.
 *
 * Why inline SVG
 *               Emoji render differently on every platform and look informal in
 *               a report screenshot. An icon library would be a whole
 *               dependency for a dozen small shapes, and most of it unused.
 *               Inline SVG inherits the surrounding text colour through
 *               currentColor, so one icon works on the dark sidebar and on a
 *               light card without a second version.
 *
 * Accessibility
 *               Every icon is aria-hidden. They sit beside a text label rather
 *               than replacing one, so a screen reader should skip them instead
 *               of announcing a shape.
 * ---------------------------------------------------------------------------
 */

/** Shared attributes, so every icon is drawn with the same weight and joins. */
const base = {
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.7,
  strokeLinecap: 'round',
  strokeLinejoin: 'round',
  'aria-hidden': true,
};

export function IconSun({ size = 18 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" {...base}>
      <circle cx="12" cy="12" r="4" />
      <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
    </svg>
  );
}

export function IconGrid({ size = 18 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" {...base}>
      <rect x="3" y="3" width="7" height="7" rx="1.5" />
      <rect x="14" y="3" width="7" height="7" rx="1.5" />
      <rect x="3" y="14" width="7" height="7" rx="1.5" />
      <rect x="14" y="14" width="7" height="7" rx="1.5" />
    </svg>
  );
}

export function IconPin({ size = 18 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" {...base}>
      <path d="M20 10c0 5.5-8 12-8 12s-8-6.5-8-12a8 8 0 0 1 16 0z" />
      <circle cx="12" cy="10" r="2.75" />
    </svg>
  );
}

export function IconCalendar({ size = 18 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" {...base}>
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M3 10h18M8 3v4M16 3v4" />
    </svg>
  );
}

export function IconUsers({ size = 18 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" {...base}>
      <path d="M16 20v-1.5a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4V20" />
      <circle cx="9" cy="7" r="3.25" />
      <path d="M22 20v-1.5a4 4 0 0 0-3-3.85M16.5 4.15a4 4 0 0 1 0 7.7" />
    </svg>
  );
}

export function IconShieldUser({ size = 18 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" {...base}>
      <path d="M12 3l7 3v6c0 4.5-3 8-7 9-4-1-7-4.5-7-9V6z" />
      <circle cx="12" cy="10" r="2.1" />
      <path d="M8.6 16.2a3.6 3.6 0 0 1 6.8 0" />
    </svg>
  );
}

export function IconMenu({ size = 20 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" {...base}>
      <path d="M4 6h16M4 12h16M4 18h16" />
    </svg>
  );
}

export function IconRefresh({ size = 15 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" {...base}>
      <path d="M3 12a9 9 0 0 1 15.3-6.4L21 8" />
      <path d="M21 3v5h-5" />
      <path d="M21 12a9 9 0 0 1-15.3 6.4L3 16" />
      <path d="M3 21v-5h5" />
    </svg>
  );
}

export function IconPlus({ size = 15 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" {...base}>
      <path d="M12 5v14M5 12h14" />
    </svg>
  );
}

export function IconSignOut({ size = 16 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" {...base}>
      <path d="M15 17l5-5-5-5" />
      <path d="M20 12H9M12 20H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h6" />
    </svg>
  );
}

export function IconInbox({ size = 22 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" {...base}>
      <path d="M3 13h5l1.5 3h5L16 13h5" />
      <path d="M5.5 5h13l2.5 8v5a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-5z" />
    </svg>
  );
}

export function IconCheck({ size = 16 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" {...base}>
      <path d="M20 6L9 17l-5-5" />
    </svg>
  );
}

export function IconAlert({ size = 16 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" {...base}>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7.5v5M12 16h.01" />
    </svg>
  );
}
