/*
 * ---------------------------------------------------------------------------
 * File        : HomePage.jsx
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-20
 * Description : The public index page. Explains what the system does, sets out
 *               the rules that govern a booking, shows how a transfer works
 *               end to end, and sends each kind of user to the right place.
 *
 * Why this page exists
 *               Three audiences arrive here and need different things: staff
 *               need to sign in, prosumers need to be told the mobile
 *               application is where they belong, and anyone seeing the system
 *               for the first time needs to understand what it is. A bare
 *               login box would answer only the first.
 *
 * Icons       : Drawn as inline SVG rather than using emoji or an icon
 *               library. Emoji render differently on every platform and look
 *               informal in a report screenshot; a library would be a whole
 *               dependency for six small shapes. Inline SVG inherits the text
 *               colour and stays sharp at any size.
 *
 * Signed in visitors
 *               The page stays reachable when somebody is already signed in,
 *               rather than bouncing them to their dashboard. A landing page
 *               that cannot be opened once you have an account is awkward to
 *               show to anyone, and the redirect made it invisible to the very
 *               people most likely to look at it. The calls to action simply
 *               change to point at the dashboard instead of the sign in form.
 * ---------------------------------------------------------------------------
 */

import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

/* --- Small inline icons ---------------------------------------------------
   Each takes its colour from the surrounding text through currentColor, so one
   icon works on both the light cards and the dark hero. */

function IconSun() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth="1.8" strokeLinecap="round" aria-hidden="true">
      <circle cx="12" cy="12" r="4" />
      <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
    </svg>
  );
}

function IconMap() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M21 10c0 6-9 12-9 12s-9-6-9-12a9 9 0 0 1 18 0z" />
      <circle cx="12" cy="10" r="3" />
    </svg>
  );
}

function IconClock() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </svg>
  );
}

function IconQr() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth="1.8" strokeLinejoin="round" aria-hidden="true">
      <rect x="3" y="3" width="7" height="7" rx="1" />
      <rect x="14" y="3" width="7" height="7" rx="1" />
      <rect x="3" y="14" width="7" height="7" rx="1" />
      <path d="M14 14h3v3h-3zM20 14v3M14 20h6" />
    </svg>
  );
}

function IconBattery() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <rect x="2" y="7" width="16" height="10" rx="2" />
      <path d="M22 10v4M6 11v2M10 11v2M14 11v2" />
    </svg>
  );
}

function IconShield() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M12 3l7 3v6c0 4.5-3 8-7 9-4-1-7-4.5-7-9V6z" />
      <path d="M9 12l2 2 4-4" />
    </svg>
  );
}

const FEATURES = [
  {
    icon: <IconMap />,
    title: 'Find a node on the map',
    body: 'Nearby microgrid hubs are plotted from their stored GPS position, nearest first, with the distance worked out by the service.',
  },
  {
    icon: <IconClock />,
    title: 'Reserve a trading window',
    body: 'Book any open window within the next seven days. Change or cancel it freely up to twelve hours before your slot.',
  },
  {
    icon: <IconQr />,
    title: 'Verified by QR at the node',
    body: 'An approved booking issues a secure code. The operator scans it and the server confirms it — the device never decides for itself.',
  },
  {
    icon: <IconBattery />,
    title: 'Live battery availability',
    body: 'Operators keep each node’s storage slots current, so a window is only offered while it genuinely has room.',
  },
  {
    icon: <IconShield />,
    title: 'Accounts vetted before use',
    body: 'Prosumers register with their NIC and a Backoffice officer activates the account before it can make a booking.',
  },
  {
    icon: <IconSun />,
    title: 'Deliver or draw energy',
    body: 'Feed surplus solar generation into the community grid, or draw from it to charge — both directions on the same booking.',
  },
];

const STEPS = [
  {
    title: 'Register with your NIC',
    body: 'Create an account on the Android application. Your National Identity Card number identifies you across the whole system.',
  },
  {
    title: 'Wait for activation',
    body: 'A Backoffice officer reviews the registration and activates the account. You are told as soon as you can sign in.',
  },
  {
    title: 'Pick a node and a window',
    body: 'See nearby nodes on the map, choose a trading window in the next seven days, and say how much energy you are moving.',
  },
  {
    title: 'Show your QR code',
    body: 'Once approved, your booking carries a QR code. Show it at the node and the operator scans it to confirm the transfer.',
  },
];

export default function HomePage() {
  const { isAuthenticated, isStaff, auth } = useAuth();

  // A signed in member of staff is offered their dashboard; everyone else is
  // offered the sign in form. One page, two sensible destinations.
  const isSignedInStaff = isAuthenticated && isStaff;
  const primaryHref = isSignedInStaff ? '/dashboard' : '/login';
  const primaryLabel = isSignedInStaff ? 'Go to dashboard' : 'Sign in to the back office';

  return (
    <div className="d-flex flex-column min-vh-100 bg-white">
      {/* --- Top bar ------------------------------------------------------- */}
      <nav className="navbar navbar-dark landing-hero py-3">
        <div className="container">
          <span className="navbar-brand d-flex align-items-center gap-2 mb-0">
            {/* The real logo. A white plate behind it, because the logo is
                drawn in navy and green and the navbar is navy. */}
            <img src="/logo.png" alt="" width="34" height="34" className="brand-logo" />
            <span className="fw-semibold">Smart Solar Microgrid</span>
          </span>
          {isSignedInStaff ? (
            <div className="d-flex align-items-center gap-2">
              <span className="small d-none d-sm-inline" style={{ color: 'rgba(247,243,236,0.7)' }}>
                {auth.fullName}
              </span>
              <Link to="/profile" className="btn btn-outline-light btn-sm px-3">
                My Account
              </Link>
              <Link to="/dashboard" className="btn btn-primary btn-sm px-3">
                Dashboard
              </Link>
            </div>
          ) : (
            <div className="d-flex align-items-center gap-2">
              <Link to="/register" className="btn btn-outline-light btn-sm px-3 d-none d-sm-inline-block">
                Register
              </Link>
              <Link to="/login" className="btn btn-primary btn-sm px-3">
                Sign in
              </Link>
            </div>
          )}
        </div>
      </nav>

      {/* --- Hero ---------------------------------------------------------- */}
      <header className="landing-hero pt-5 pb-5">
        <div className="container py-4">
          <div className="row align-items-center g-5">
            <div className="col-lg-7">
              <p className="landing-eyebrow mb-3">Community energy trading</p>

              <h1 className="landing-hero-title mb-4">
                Solar power, traded across the neighbourhood grid
              </h1>

              <p className="landing-lead fs-5 mb-4">
                Households with solar panels reserve energy transfer slots at nearby microgrid
                hubs. Operators verify every transfer on site. The whole exchange is scheduled,
                approved and recorded in one place.
              </p>

              {/* Two actions only. One primary, one secondary.
                  A third choice of equal weight makes a visitor stop and
                  read instead of act, and the features section is directly
                  below this, so a "see how it works" link only repeated a
                  scroll they were going to make anyway. */}
              <div className="d-flex flex-wrap gap-2 mb-5">
                <Link to={primaryHref} className="btn btn-primary btn-lg px-4">
                  {primaryLabel}
                </Link>
                {!isSignedInStaff && (
                  <Link to="/register" className="btn btn-outline-light btn-lg px-4">
                    Register as a prosumer
                  </Link>
                )}
              </div>

              {/* The rules that actually govern a booking, stated plainly. */}
              <div className="row g-4 border-top border-secondary border-opacity-25 pt-4">
                <div className="col-4">
                  <div className="landing-figure">7 days</div>
                  <div className="landing-figure-label">Booking window ahead</div>
                </div>
                <div className="col-4">
                  <div className="landing-figure">12 hrs</div>
                  <div className="landing-figure-label">Notice to change or cancel</div>
                </div>
                <div className="col-4">
                  <div className="landing-figure">NIC</div>
                  <div className="landing-figure-label">Prosumer identity key</div>
                </div>
              </div>
            </div>

            {/* An animated solar scene, in place of a static picture.

                It is drawn with CSS transforms and one SVG. No 3D library is
                pulled in for it, because the whole scene is a handful of
                boxes and the page has to stay quick to load.

                Everything here is decoration, so it is hidden from screen
                readers and it stops moving for anyone who has asked their
                system to reduce motion. */}
            <div className="col-lg-5">
              <div className="solar-scene" aria-hidden="true">

                {/* The sun. The rays turn, the glow breathes. */}
                <div className="solar-sun">
                  <span className="solar-sun-glow" />
                  <span className="solar-sun-rays" />
                  <span className="solar-sun-core" />
                </div>

                {/* The panel, tilted back in 3D and floating gently. */}
                <div className="solar-stage">
                  <div className="solar-panel">
                    {Array.from({ length: 24 }, (unused, cell) => (
                      <span
                        key={cell}
                        className="solar-cell"
                        style={{ animationDelay: `${(cell % 6) * 0.18}s` }}
                      />
                    ))}
                    <span className="solar-sheen" />
                  </div>
                  <span className="solar-mast" />
                </div>

                {/* Energy running from the panel down to the grid node. */}
                <div className="solar-flow">
                  <span className="solar-spark" />
                  <span className="solar-spark" />
                  <span className="solar-spark" />
                </div>

                <div className="solar-node">
                  <span className="solar-node-pulse" />
                </div>

              </div>
            </div>
          </div>
        </div>
      </header>

      <main className="flex-grow-1">
        {/* --- Features --------------------------------------------------- */}
        <section className="container py-5">
          <div className="row mb-4">
            <div className="col-lg-7">
              <p className="landing-section-label mb-2">What the system does</p>
              <h2 className="h1 fw-semibold mb-3">
                Everything a community energy exchange needs
              </h2>
              <p className="text-body-secondary mb-0">
                From finding a hub to confirming the transfer, each step is scheduled, checked
                against the trading rules, and recorded against the prosumer&rsquo;s account.
              </p>
            </div>
          </div>

          <div className="row g-4">
            {FEATURES.map((feature) => (
              <div className="col-md-6 col-lg-4" key={feature.title}>
                <div className="landing-card p-4">
                  <div className="landing-icon mb-3">{feature.icon}</div>
                  <h3 className="h6 fw-semibold mb-2">{feature.title}</h3>
                  <p className="text-body-secondary small mb-0">{feature.body}</p>
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* --- How it works ------------------------------------------------ */}
        <section className="bg-body-tertiary py-5" id="how">
          <div className="container py-3">
            <div className="row g-5">
              <div className="col-lg-4">
                <p className="landing-section-label mb-2">How it works</p>
                <h2 className="h1 fw-semibold mb-3">From sign up to energy transfer</h2>
                <p className="text-body-secondary">
                  Four steps, in this order. A booking cannot be made before the account is
                  activated, and a QR code is not issued until the booking is approved.
                </p>
                <Link to={primaryHref} className="btn btn-primary mt-2">
                  {primaryLabel}
                </Link>
              </div>

              <div className="col-lg-8">
                {STEPS.map((step, index) => (
                  <div className="d-flex gap-3 mb-4" key={step.title}>
                    <div className="landing-step-number">{index + 1}</div>
                    <div>
                      <h3 className="h6 fw-semibold mb-1">{step.title}</h3>
                      <p className="text-body-secondary small mb-0">{step.body}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </section>

        {/* --- Roles -------------------------------------------------------- */}
        <section className="container py-5">
          <div className="text-center mb-4">
            <p className="landing-section-label mb-2">Who uses it</p>
            <h2 className="h1 fw-semibold mb-0">Three roles, one system</h2>
          </div>

          <div className="row g-4">
            <div className="col-md-4">
              <div className="landing-card p-4 text-center">
                <h3 className="h6 fw-semibold mb-2">Backoffice officers</h3>
                <p className="text-body-secondary small mb-3">
                  Register microgrid nodes with their location and capacity, maintain operating
                  schedules, create staff accounts, and activate new prosumers.
                </p>
                <Link to={primaryHref} className="btn btn-outline-primary btn-sm">
                  Web application
                </Link>
              </div>
            </div>

            <div className="col-md-4">
              <div className="landing-card p-4 text-center">
                <h3 className="h6 fw-semibold mb-2">Grid operators</h3>
                <p className="text-body-secondary small mb-3">
                  Keep battery slot availability current, monitor bookings at each node, and
                  confirm transfers on site by scanning the prosumer&rsquo;s QR code.
                </p>
                <Link to={primaryHref} className="btn btn-outline-primary btn-sm">
                  Web &amp; mobile
                </Link>
              </div>
            </div>

            <div className="col-md-4">
              <div className="landing-card p-4 text-center">
                <h3 className="h6 fw-semibold mb-2">Solar prosumers</h3>
                <p className="text-body-secondary small mb-3">
                  Register with your NIC, find nearby nodes on the map, reserve a transfer
                  window, and review your booking history.
                </p>
                <span className="badge text-bg-light border fw-normal">
                  Android application
                </span>
              </div>
            </div>
          </div>
        </section>
      </main>

      {/* --- Footer --------------------------------------------------------- */}
      <footer className="landing-hero py-4">
        <div className="container d-flex flex-wrap justify-content-between align-items-center gap-2">
          <div className="d-flex align-items-center gap-2">
            <img src="/logo.png" alt="" width="28" height="28" className="brand-logo" />
            <span className="fw-semibold">Smart Solar Microgrid Trading System</span>
          </div>
          <span className="small" style={{ color: 'rgba(247,243,236,0.6)' }}>
            SE4040 Enterprise Application Development &middot; Assignment 1
          </span>
        </div>
      </footer>
    </div>
  );
}
