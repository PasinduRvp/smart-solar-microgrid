/*
 * ---------------------------------------------------------------------------
 * File        : ConfirmDialog.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Asks the user to confirm an action before it is carried out,
 *               and shows the outcome if the service refuses it.
 *
 * Why confirm : Activating, deactivating, approving and cancelling all change
 *               something a person depends on, and none of them is undone by a
 *               page refresh. A deliberate second click is the difference
 *               between an intended action and a mis-click on the wrong row.
 *
 * Error handling
 *               When the service refuses — "3 active reservations must be
 *               cancelled first" — the message is shown inside this dialog and
 *               the dialog stays open, so the user reads why it failed next to
 *               the thing they were trying to do. Closing the dialog and
 *               showing the error elsewhere loses that connection.
 * ---------------------------------------------------------------------------
 */

export default function ConfirmDialog({
  title,
  message,
  confirmLabel = 'Confirm',
  confirmVariant = 'primary',
  isBusy = false,
  error = '',
  onConfirm,
  onCancel,
  children,
}) {
  return (
    <>
      {/* Bootstrap's modal markup, shown directly rather than through its
          JavaScript, so React stays in charge of when it appears. */}
      <div className="modal fade show d-block" tabIndex="-1" role="dialog" aria-modal="true">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content border-0 shadow">
            <div className="modal-header">
              <h5 className="modal-title h6 fw-semibold">{title}</h5>
              <button
                type="button"
                className="btn-close"
                aria-label="Close"
                onClick={onCancel}
                disabled={isBusy}
              />
            </div>

            <div className="modal-body">
              <p className="mb-0 small">{message}</p>

              {/* Anything the caller needs to collect before confirming, such
                  as a cancellation reason, is rendered here inside the dialog
                  rather than floated over it. */}
              {children && <div className="mt-3">{children}</div>}

              {error && (
                <div className="alert alert-danger py-2 small mt-3 mb-0" role="alert">
                  {error}
                </div>
              )}
            </div>

            <div className="modal-footer">
              <button
                type="button"
                className="btn btn-outline-secondary btn-sm"
                onClick={onCancel}
                disabled={isBusy}
              >
                Cancel
              </button>
              <button
                type="button"
                className={`btn btn-${confirmVariant} btn-sm`}
                onClick={onConfirm}
                disabled={isBusy}
              >
                {isBusy ? (
                  <>
                    <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />
                    Working...
                  </>
                ) : (
                  confirmLabel
                )}
              </button>
            </div>
          </div>
        </div>
      </div>

      <div className="modal-backdrop fade show" />
    </>
  );
}
