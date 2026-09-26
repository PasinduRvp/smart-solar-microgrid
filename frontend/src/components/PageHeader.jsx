/*
 * ---------------------------------------------------------------------------
 * File        : PageHeader.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : The title block at the top of every screen: heading, a line
 *               explaining what the screen is for, and the actions belonging
 *               to it.
 *
 * Code smell  : Each page previously built its own header with its own
 *               spacing and heading size. Written once, every screen now lines
 *               up exactly, and a change to the house style happens in one
 *               place instead of five.
 * ---------------------------------------------------------------------------
 */

export default function PageHeader({ title, subtitle, actions }) {
  return (
    <div className="page-header">
      <div>
        <h1 className="page-title">{title}</h1>
        {subtitle && <p className="page-subtitle">{subtitle}</p>}
      </div>
      {actions && <div className="d-flex flex-wrap gap-2">{actions}</div>}
    </div>
  );
}
