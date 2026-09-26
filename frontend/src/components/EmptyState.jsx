/*
 * ---------------------------------------------------------------------------
 * File        : EmptyState.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Shown in place of a table when there is nothing to list.
 *
 * Why not just blank
 *               An empty table looks broken: the user cannot tell whether
 *               nothing matched, something failed, or the page is still
 *               loading. Saying which, and offering the action that would fill
 *               it, turns a dead end into a next step.
 * ---------------------------------------------------------------------------
 */

import { IconInbox } from './Icons';

export default function EmptyState({ title, message, action }) {
  return (
    <div className="app-empty">
      <div className="app-empty-icon">
        <IconInbox />
      </div>
      <div className="fw-semibold text-body">{title}</div>
      {message && <p className="small mt-1 mb-0">{message}</p>}
      {action && <div className="mt-3">{action}</div>}
    </div>
  );
}
