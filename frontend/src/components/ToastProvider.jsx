/*
 * ---------------------------------------------------------------------------
 * File        : ToastProvider.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Brief confirmation messages in the corner of the screen, and
 *               the hook screens use to raise one.
 *
 * Why this exists
 *               Until now an action gave no feedback at all. An officer would
 *               activate a prosumer, the dialog would close, the list would
 *               quietly reload, and nothing confirmed that anything had
 *               happened. The user is left checking the row to see whether
 *               their click registered. A short confirmation removes that
 *               doubt without interrupting them.
 *
 * Failures still shown in place
 *               Only successes are announced here. A refusal — the twelve hour
 *               rule, a node with live bookings — stays inside the dialog or
 *               form it came from, next to the thing the user was trying to do.
 *               A message that disappears after four seconds is the wrong place
 *               for something the user has to read and act on.
 *
 * Accessibility
 *               The container is a live region, so a screen reader announces a
 *               new message without the focus being moved away from whatever
 *               the user was doing.
 * ---------------------------------------------------------------------------
 */

import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import { IconAlert, IconCheck } from './Icons';

const ToastContext = createContext(null);

/** How long a message stays on screen. */
const DISMISS_AFTER_MS = 4000;

export function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([]);

  const dismiss = useCallback((id) => {
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const showToast = useCallback(
    (message, variant = 'success') => {
      // Date.now alone could collide when two actions finish in the same
      // millisecond, which would give two toasts the same React key.
      const id = `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;

      setToasts((current) => [...current, { id, message, variant }]);
      window.setTimeout(() => dismiss(id), DISMISS_AFTER_MS);
    },
    [dismiss],
  );

  const value = useMemo(() => ({ showToast }), [showToast]);

  return (
    <ToastContext.Provider value={value}>
      {children}

      <div className="app-toasts" role="status" aria-live="polite">
        {toasts.map((toast) => (
          <div key={toast.id} className={`app-toast is-${toast.variant}`}>
            <span className={toast.variant === 'danger' ? 'text-danger' : 'text-success'}>
              {toast.variant === 'danger' ? <IconAlert /> : <IconCheck />}
            </span>
            <span className="flex-grow-1">{toast.message}</span>
            <button
              type="button"
              className="btn-close btn-close-sm"
              style={{ fontSize: '0.6rem' }}
              aria-label="Dismiss"
              onClick={() => dismiss(toast.id)}
            />
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

/**
 * Gives a screen the showToast function.
 * Throws outside the provider, so a wiring mistake fails immediately and
 * obviously rather than silently doing nothing.
 */
export function useToast() {
  const context = useContext(ToastContext);

  if (!context) {
    throw new Error('useToast must be used inside a ToastProvider.');
  }

  return context;
}
