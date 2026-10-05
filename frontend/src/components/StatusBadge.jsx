/*
 * ---------------------------------------------------------------------------
 * File        : StatusBadge.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Shows an account or booking state as a small coloured label.
 *
 * Accessibility
 *               The state is always spelled out in words as well as colour.
 *               A badge that relied on colour alone would be meaningless to a
 *               colourblind reader and invisible in a printed report.
 *
 * Code smell  : Written once and reused everywhere a state is displayed, so a
 *               status cannot end up green on one screen and grey on another.
 * ---------------------------------------------------------------------------
 */

// Each state maps to one Bootstrap colour, chosen for meaning rather than
// appearance: green means usable, amber means waiting on a person, grey means
// closed, red means refused or cancelled.
const STATUS_STYLES = {
  Active: 'text-bg-success',
  Pending: 'text-bg-warning',
  Deactivated: 'text-bg-secondary',
  Approved: 'text-bg-success',
  Completed: 'text-bg-primary',
  Cancelled: 'text-bg-danger',
};

export default function StatusBadge({ status }) {
  const className = STATUS_STYLES[status] ?? 'text-bg-light';
  return <span className={`badge ${className}`}>{status}</span>;
}
