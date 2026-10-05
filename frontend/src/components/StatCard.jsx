/*
 * ---------------------------------------------------------------------------
 * File        : StatCard.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : A single headline figure on the dashboard: a large number, a
 *               label, and an optional note underneath.
 *
 * Why a tile and not a chart
 *               One number has no shape to plot. Drawing a bar of length one
 *               adds ink without adding information, so the figure is simply
 *               shown at a size that lets it be read at a glance.
 *
 * Use of colour
 *               Colour here means STATE, not decoration. A tile turns amber
 *               only when it represents work waiting for a person, so a
 *               coloured tile always means "this needs attention". The figure
 *               itself stays in normal text colour so it is legible either way,
 *               and the state is also carried by the wording — never by colour
 *               alone, which would be invisible to a colourblind reader.
 * ---------------------------------------------------------------------------
 */

import { Link } from 'react-router-dom';

/**
 * @param {object} props
 * @param {string} props.label    What the figure counts.
 * @param {number} props.value    The figure itself.
 * @param {string} [props.note]   Optional explanation underneath.
 * @param {boolean} [props.needsAttention] True when the figure represents outstanding work.
 * @param {string} [props.to]     Optional page this tile links to.
 */
export default function StatCard({ label, value, note, needsAttention = false, to }) {
  // A tile only becomes amber when it is non-zero AND represents work waiting.
  // "0 pending approvals" is good news and should look calm, not alarming.
  const isHighlighted = needsAttention && value > 0;

  const card = (
    <div
      className={`card h-100 border-0 shadow-sm${isHighlighted ? ' border-start border-4 border-warning' : ''}`}
    >
      <div className="card-body">
        <div className="text-body-secondary text-uppercase fw-semibold" style={{ fontSize: '0.7rem', letterSpacing: '0.08em' }}>
          {label}
        </div>
        <div className="display-6 fw-semibold lh-1 mt-2" style={{ fontVariantNumeric: 'tabular-nums' }}>
          {value}
        </div>
        {note && <div className="text-body-secondary small mt-2">{note}</div>}
        {isHighlighted && (
          <div className="text-warning-emphasis small fw-semibold mt-2">Needs attention</div>
        )}
      </div>
    </div>
  );

  if (!to) {
    return card;
  }

  return (
    <Link to={to} className="text-decoration-none text-reset d-block h-100">
      {card}
    </Link>
  );
}
