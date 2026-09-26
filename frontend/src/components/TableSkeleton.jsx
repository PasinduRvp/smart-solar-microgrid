/*
 * ---------------------------------------------------------------------------
 * File        : TableSkeleton.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : Placeholder rows shown while a table is loading.
 *
 * Why not a spinner
 *               A spinner tells the user only that something is happening. Grey
 *               bars in the shape of the table say what is about to appear and
 *               keep the page from jumping when the data lands, because the
 *               layout is already the right height.
 * ---------------------------------------------------------------------------
 */

export default function TableSkeleton({ columns, rows = 4 }) {
  return (
    <>
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <tr key={rowIndex}>
          {Array.from({ length: columns }).map((__, columnIndex) => (
            <td key={columnIndex}>
              {/* Varying widths look like real content rather than a grid. */}
              <div className="skeleton" style={{ width: `${55 + ((rowIndex + columnIndex) % 4) * 12}%` }} />
            </td>
          ))}
        </tr>
      ))}
    </>
  );
}
