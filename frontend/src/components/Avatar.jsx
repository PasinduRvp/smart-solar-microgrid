/*
 * ---------------------------------------------------------------------------
 * File        : Avatar.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : A person's initials in a circle, used beside names in lists
 *               and in the sidebar.
 *
 * Why initials rather than a photograph
 *               The system stores no images, and inventing a placeholder face
 *               would be worse than none. Initials give each row a stable
 *               anchor for the eye when scanning a long list of names.
 * ---------------------------------------------------------------------------
 */

export default function Avatar({ name, small = false }) {
  const initials = (name ?? '')
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0].toUpperCase())
    .join('');

  return (
    <span className={`app-avatar${small ? ' app-avatar-sm' : ''}`} aria-hidden="true">
      {initials || '?'}
    </span>
  );
}
