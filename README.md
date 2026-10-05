# Smart Solar Microgrid Trading System

**SE4040 Enterprise Application Development — Assignment 1**

A system that lets households with solar panels (prosumers) book time windows at microgrid
trading stations to sell or buy energy. Staff approve the bookings, and a QR code confirms each
energy transfer at the station.

## Submission links

| Item | Link |
|---|---|
| **Git repository** | https://github.com/PasinduRvp/smart-solar-microgrid.git |
| **Demo video (YouTube)** | https://youtu.be/rrjLQFWK2VQ |
| **Demo video (OneDrive)** | [Watch on OneDrive](https://mysliit-my.sharepoint.com/:v:/g/personal/it23380196_my_sliit_lk/IQDV5CornBdoR5yWI848sQz0ARE0SjM719lJY1jdQ-C--2U?nav=eyJyZWZlcnJhbEluZm8iOnsicmVmZXJyYWxBcHAiOiJPbmVEcml2ZUZvckJ1c2luZXNzIiwicmVmZXJyYWxBcHBQbGF0Zm9ybSI6IldlYiIsInJlZmVycmFsTW9kZSI6InZpZXciLCJyZWZlcnJhbFZpZXciOiJNeUZpbGVzTGlua0NvcHkifX0&e=6mCrfd) |
| **Contributors and commits** | https://github.com/PasinduRvp/smart-solar-microgrid/graphs/contributors |

The video (under 5 minutes) shows how the web app, the mobile app and the API work together.
Each member's individual contribution is listed in [section 12](#12-individual-contributions).

---

The system has three parts that share one database:

| Part | Folder | Used by |
|---|---|---|
| Web API (C#, ASP.NET Core 8) | [`backend/`](backend/) | Both client apps |
| Web app (React) | [`frontend/`](frontend/) | Backoffice and Grid Operator |
| Mobile app (native Android, Java) | [`android/`](android/) | Prosumer and Grid Operator |

---

## Contents

1. [Features](#1-features)
2. [Architecture](#2-architecture)
3. [Technology stack](#3-technology-stack)
4. [Project structure](#4-project-structure)
5. [Database](#5-database)
6. [Business rules](#6-business-rules)
7. [API endpoints](#7-api-endpoints)
8. [Getting started](#8-getting-started)
9. [Configuration](#9-configuration)
10. [Troubleshooting](#10-troubleshooting)
11. [Documentation](#11-documentation)
12. [Individual contributions](#12-individual-contributions)

---

## 1. Features

### User roles

| Role | Works on | What they do |
|---|---|---|
| **Backoffice** | Web | Everything. Manages staff accounts, activates prosumers, creates stations, generates time slots, approves bookings. |
| **Grid Operator** | Web and phone | Daily work. Approves bookings, scans QR codes, completes energy transfers, deactivates prosumers. |
| **Prosumer** | Phone | Registers, books energy windows, changes or cancels bookings, shows a QR code at the station. |

### Web app (React)

- Sign in with role-based pages. Each role only sees its own menu items.
- Dashboard with live figures from the API.
- **User management** (Backoffice): create, edit and delete Backoffice and Grid Operator accounts.
- **Prosumer management**: list prosumers, activate pending accounts, deactivate accounts, handle
  deactivation requests.
- **Station management**: create and edit stations, set the weekly opening schedule, activate or
  deactivate a station, generate bookable time slots.
- **Reservation management**: search bookings, approve pending ones, create and change bookings for
  a prosumer.
- Own profile and password change.

### Mobile app (Android)

- Register as a prosumer and sign in. The session is kept in SQLite.
- Energy dashboard with pending and approved bookings and the next booking.
- Find stations on a map (OpenStreetMap, or Google Maps when a key is set) and list nearby stations
  using the phone's location.
- Book an energy window, view the summary, change or cancel it.
- Show the booking's QR code once it is approved.
- **Grid Operator**: scan a prosumer's QR code with the camera and complete the energy transfer.
- Own profile, password change and an account deactivation request.

The full use case diagram is in [docs/use-case-diagram.md](docs/use-case-diagram.md).

---

## 2. Architecture

```
   +------------------------+          +--------------------------+
   |   React Web App        |          |   Android App            |
   |   (browser)            |          |   (phone)                |
   |   Backoffice           |          |   Prosumer               |
   |   Grid Operator        |          |   Grid Operator          |
   +-----------+------------+          +------------+-------------+
               |   HTTP + JSON, Bearer token (JWT)  |
               +----------------+-------------------+
                                |
                                v
              +---------------------------------------+
              |   ASP.NET Core Web API (C#)           |
              |   hosted on IIS, port 8081            |
              |                                       |
              |   Controllers  -> read the request    |
              |   Services     -> ALL business rules  |
              |   Repositories -> talk to MongoDB     |
              +-------------------+-------------------+
                                  |
                                  v
              +---------------------------------------+
              |   MongoDB Atlas (cloud)               |
              |   database: SmartSolarMicrogrid       |
              +---------------------------------------+
```

**The main design rule: every business rule lives in the API.** The client apps only show data
and check for empty fields. When the API refuses something, it sends a clear message, and the
client shows it as it is. So the web app and the phone can never disagree about a rule.

Local copies that can be deleted at any time:

- The browser keeps the session in `localStorage`.
- The phone keeps the session, the profile and the last station list in SQLite.

---

## 3. Technology stack

| Layer | Technology |
|---|---|
| Web API | C#, .NET 8, ASP.NET Core Web API, Swagger (Swashbuckle) |
| Database | MongoDB Atlas, MongoDB .NET Driver 2.28 |
| Security | JWT bearer tokens (HMAC-SHA256), BCrypt password hashing, rate limiting |
| Hosting | IIS on Windows, with the .NET 8 Hosting Bundle |
| Web app | React 19, Vite, React Router, Axios, Bootstrap 5 |
| Mobile app | Java, Android SDK 35, Retrofit, OkHttp, Gson, SQLite |
| Maps | OpenStreetMap (osmdroid), Google Maps (optional) |
| QR codes | ZXing (draw), ZXing Android Embedded (scan) |

---

## 4. Project structure

```
smart-solar-microgrid/
├── backend/                     ASP.NET Core Web API
│   ├── Controllers/             HTTP endpoints, no rules
│   ├── Services/                business rules
│   ├── Repositories/            MongoDB queries
│   ├── Models/                  database documents and enums
│   ├── Dtos/                    request and response shapes
│   ├── Mappings/                model <-> DTO conversion
│   ├── Security/                JWT, BCrypt, QR token, current user
│   ├── Middleware/              error handling, security headers
│   ├── Data/                    Mongo context, indexes, first admin seed
│   ├── Common/                  booking rules, roles, time zone helpers
│   ├── Configuration/           typed settings classes
│   └── Program.cs               startup and dependency injection
│
├── frontend/                    React web app
│   └── src/
│       ├── api/                 one file per API area (Axios)
│       ├── auth/                sign-in state
│       ├── components/          shared UI and form dialogs
│       ├── pages/               one file per screen
│       └── routes/              role-protected routes
│
├── android/                     native Android app
│   └── app/src/main/java/lk/sliit/solarmicrogrid/
│       ├── data/remote/         Retrofit APIs, interceptors, DTOs
│       ├── data/local/          SQLite stores
│       ├── data/repository/     data access for the screens
│       ├── model/               app models
│       ├── ui/                  screens: auth, home, booking, map, operator, profile
│       └── util/                dates, QR images, validation
│
├── docs/                        guides, diagrams, report material
├── deploy.ps1                   publishes the API to IIS
└── configure-server.ps1         copies secrets to the IIS server, once
```

---

## 5. Database

MongoDB Atlas, one database (`SmartSolarMicrogrid`), four collections.

```
   Users  (NIC is the prosumer key)
     |
     | ProsumerNic
     v
   SolarStationInfo ---> EnergyBookingSlots ---> EnergyReservation
   (a station)           (a bookable window)     (one booking)
```

| Collection | Holds | Key fields |
|---|---|---|
| `Users` | All accounts | `Nic` (unique), `Email` (unique), `PasswordHash`, `Role`, `Status` |
| `SolarStationInfo` | Trading stations | `StationCode`, `Location`, `CapacityKwh`, `Schedule`, `IsActive` |
| `EnergyBookingSlots` | Bookable time windows | `StationId`, `SlotDate`, `StartTime`, `EndTime`, `TotalCapacitySlots`, `BookedCount` |
| `EnergyReservation` | Bookings | `ReservationNo`, `ProsumerNic`, `SlotId`, `EnergyKwh`, `Direction`, `Status`, `QrToken` |

- **Account status:** `Pending` → `Active` → `Deactivated`
- **Reservation status:** `Pending` → `Approved` → `Completed`, or `Cancelled`
- **Energy direction:** `Deliver` (selling) or `Draw` (buying)

Indexes are created automatically at startup by `Data/DatabaseInitializer.cs`. The first
Backoffice account is created by `Data/BackofficeSeeder.cs` when the database has none.

All times are stored in UTC. Opening hours are local time, converted using `Grid:TimeZoneId`
(`Asia/Colombo`).

---

## 6. Business rules

All rules are checked in the API service layer. The limits 7 and 12 are defined once, in
`backend/Common/BookingRules.cs`.

| Rule | What it says | Enforced in |
|---|---|---|
| **BR-1** | A booking must be in the future and within 7 days. | `ReservationService` |
| **BR-2** | A booking can be changed only 12 or more hours before it starts. | `ReservationService.UpdateAsync` |
| **BR-3** | A booking can be cancelled only 12 or more hours before it starts. | `ReservationService.CancelAsync` |
| **BR-4** | A station cannot be deactivated while it has pending or approved future bookings. | `StationService.DeactivateAsync` |
| **BR-5** | Only Backoffice can activate or reactivate a prosumer account. | `ProsumersController` role check |
| **BR-6** | A prosumer can request deactivation, but staff do the deactivation. | `ProsumerService` |
| **BR-7** | A new prosumer starts as Pending and cannot sign in until activated. | `AuthService` |
| **BR-8** | A QR code is issued only when a booking is approved, and checked on the server. | `ReservationService` |
| **BR-9** | A time window cannot be overbooked. | Atomic update in `SlotRepository` |
| **BR-10** | Every endpoint checks the caller's role. | `[Authorize]` on every controller |

When a rule refuses a request, the API returns `409 Conflict` (or `403 Forbidden`) with a message
in the `detail` field. Both apps show that message to the user.

---

## 7. API endpoints

Base URL: `http://<server>:8081/api`. Full, live documentation is at `/swagger`.

| Area | Method and path | Who |
|---|---|---|
| **Auth** | `POST /auth/login` | Anyone |
| | `POST /auth/register` | Anyone (creates a Pending prosumer) |
| **Profile** | `GET /profile`, `PUT /profile` | Signed in |
| | `PUT /profile/password` | Signed in |
| **Users** | `GET /users`, `GET /users/{id}` | Backoffice |
| | `POST /users`, `PUT /users/{id}`, `DELETE /users/{id}` | Backoffice |
| **Prosumers** | `GET /prosumers` | Staff |
| | `GET /prosumers/pending` | Backoffice |
| | `GET /prosumers/{nic}`, `PUT /prosumers/{nic}` | Signed in (owner or staff) |
| | `PATCH /prosumers/{nic}/activate` | Backoffice |
| | `PATCH /prosumers/{nic}/deactivate` | Staff |
| | `PATCH /prosumers/{nic}/request-deactivation` | Prosumer |
| **Stations** | `GET /stations`, `GET /stations/{id}`, `GET /stations/nearby` | Signed in |
| | `POST /stations`, `PUT /stations/{id}` | Backoffice |
| | `PUT /stations/{id}/schedule` | Backoffice |
| | `PATCH /stations/{id}/deactivate`, `PATCH /stations/{id}/reactivate` | Backoffice |
| **Slots** | `GET /stations/{stationId}/slots`, `GET /slots/{id}` | Signed in |
| | `POST /stations/{stationId}/slots/generate` | Backoffice |
| | `PATCH /slots/{id}/availability` | Staff |
| **Reservations** | `GET /reservations`, `GET /reservations/{id}` | Signed in |
| | `POST /reservations`, `PUT /reservations/{id}` | Signed in |
| | `PATCH /reservations/{id}/cancel` | Signed in |
| | `GET /reservations/pending` | Staff |
| | `PATCH /reservations/{id}/approve` | Staff |
| | `GET /reservations/{id}/qr` | Prosumer (owner only) |
| | `POST /reservations/verify-qr` | Staff |
| | `PATCH /reservations/{id}/complete` | Staff |
| **Dashboard** | `GET /dashboard/prosumer/{nic}` | Signed in |
| | `GET /dashboard/operator` | Staff |
| **Admin** | `POST /admin/seed-demo-data` | Backoffice |
| **Health** | `GET /health` | Anyone |

"Staff" means Backoffice or Grid Operator. Where a prosumer can call an endpoint, the service also
checks that the record belongs to them.

---

## 8. Getting started

### Requirements

- .NET 8 SDK
- Node.js 20 or newer
- Android Studio (with Android SDK 35)
- A MongoDB Atlas cluster (the free tier works)
- For hosting: IIS and the .NET 8 Hosting Bundle

### 8.1 Web API

1. Create your local settings file. It is never committed.

   ```powershell
   cd backend
   Copy-Item appsettings.Example.json appsettings.Development.json
   ```

2. In `appsettings.Development.json`, fill in:

   | Setting | Value |
   |---|---|
   | `MongoDb:ConnectionString` | Your Atlas connection string |
   | `Jwt:SecretKey` | At least 32 random characters |
   | `Seed:Email`, `Seed:Password` | The first Backoffice sign-in |

3. Run it.

   ```powershell
   dotnet restore
   dotnet run
   ```

   | What | Address |
   |---|---|
   | API | `http://localhost:5202` |
   | Swagger | `http://localhost:5202/swagger` |
   | Health check | `http://localhost:5202/api/health` |

### 8.2 Host the API on IIS

One-time setup: turn on IIS, install the .NET 8 Hosting Bundle, create a site named
`SolarMicrogridApi` on port **8081** pointing to `C:\inetpub\SolarApi`, and set its application
pool to **No Managed Code**.

Then, in PowerShell **as Administrator**, from the project folder:

```powershell
.\deploy.ps1             # publish the API to IIS (run after every code change)
.\configure-server.ps1   # copy secrets to the server (run once)
```

Allow the phone to reach the API through the firewall (once):

```powershell
New-NetFirewallRule -DisplayName "SolarApi 8081" -Direction Inbound -Protocol TCP -LocalPort 8081 -Action Allow -Profile Any
```

Check it: `http://localhost:8081/api/health`.

Port 8081 is used because Oracle already uses 8080 on the development machine and the IIS
Default Web Site uses 80.

### 8.3 Web app

```powershell
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`.

The app calls `http://localhost:8081/api` (IIS) by default. To use `dotnet run` instead, create
`frontend/.env.local`:

```
VITE_API_BASE_URL=http://localhost:5202/api
```

Build for release with `npm run build`. The output goes to `frontend/dist/`.

### 8.4 Android app

1. Find the laptop's IP address with `ipconfig`. Use the IPv4 address of the network the phone
   is on.
2. Set it in `android/local.properties` (copy `local.properties.example` if the line is missing):

   ```
   API_BASE_URL=http://192.168.137.1:8081/
   ```

   | Situation | Address |
   |---|---|
   | Real phone on the same Wi-Fi or hotspot | The laptop's IPv4 address |
   | Android emulator | `http://10.0.2.2:8081/` |

   Keep the slash at the end.
3. Open `android/` in Android Studio, let Gradle sync, then **Rebuild** and **Run**.

The address is built into the app, so **rebuild after every change** to `local.properties`.
Debug builds allow plain HTTP; release builds do not.

---

## 9. Configuration

Each part has one local file for secrets and machine settings. None of them are committed.

| Part | Local file | Holds |
|---|---|---|
| Web API (local) | `backend/appsettings.Development.json` | Mongo connection string, JWT key, first admin |
| Web API (IIS) | `C:\inetpub\SolarApi\appsettings.Production.json` | The same, for the server |
| Web app | `frontend/.env.local` | API address |
| Android app | `android/local.properties` | SDK path, API address |

Shared, committed settings are in `backend/appsettings.json`: token lifetime (120 minutes),
issuer, audience, time zone and Swagger on or off.

### Ports

| Service | Port |
|---|---|
| API on IIS | 8081 |
| API with `dotnet run` | 5202 (HTTP), 7227 (HTTPS) |
| React dev server | 5173 |

---

## 10. Troubleshooting

| Problem | Cause | Fix |
|---|---|---|
| Phone: `ConnectException` or `failed to connect` | Wrong IP in `local.properties` | Run `ipconfig`, update the address, rebuild |
| Phone: `SocketTimeoutException` | Firewall blocks port 8081, or the phone is on a different network | Add the firewall rule, put both devices on the same network |
| Works on the laptop, not on the phone | `localhost` used as the address | Use the laptop's LAN IP |
| Works on the phone, not in the browser | CORS | Add the web app's address to `Cors:AllowedOrigins`, restart the API |
| `401 Unauthorized` | Token missing or expired (120 minutes) | Sign in again |
| `403 Forbidden` | Wrong role, or the account is Pending or Deactivated | Use the right account, or ask Backoffice to activate it |
| `409 Conflict` | A business rule refused the request | Read the message in `detail` |
| `429 Too Many Requests` | More than 10 sign-in attempts in a minute | Wait one minute |
| IIS `500.30` | Bad settings, or the app pool is not "No Managed Code" | Fix the pool, check `appsettings.Production.json` |
| IIS `502.5` | Hosting Bundle missing | Install it again, then run `iisreset` |
| Settings changed on IIS, nothing happens | Settings are read only at startup | Recycle the application pool |

---

## 11. Documentation

| Document | What it covers |
|---|---|
| [docs/SYSTEM_FLOW_GUIDE.md](docs/SYSTEM_FLOW_GUIDE.md) | Full end-to-end guide: request flow, security, setup, viva answers |
| [docs/use-case-diagram.md](docs/use-case-diagram.md) | Actors, use cases and relationships |
| [docs/use-case-diagram.drawio](docs/use-case-diagram.drawio) | Use case diagram (draw.io) |
| [docs/diagrams/](docs/diagrams/) | Architecture, use case, DFD and database diagrams (draw.io and PNG) |
| [docs/TEAM_WORK_DIVISION.md](docs/TEAM_WORK_DIVISION.md) | Who owns which part |
| [docs/VIVA_GUIDE_SI_EN.md](docs/VIVA_GUIDE_SI_EN.md) | Viva preparation |
| [docs/VIVA_WEB_UI_GUIDE.md](docs/VIVA_WEB_UI_GUIDE.md) | Web app walkthrough |
| [frontend/README.md](frontend/README.md) | Web app notes |

> Note: section 13 of `SYSTEM_FLOW_GUIDE.md` says the Android address is set in `ApiConfig.java`
> and `network_security_config.xml`. That is out of date. It now lives only in
> `android/local.properties`, as described in [8.4](#84-android-app).

---

## 12. Individual contributions

Each member owns one feature area **end to end**: the API, the web screens and the Android
screens. Every source file names its author in the header comment, and each member committed
their own work from their own GitHub account.

| Member | IT number | Feature area |
|---|---|---|
| M T A J Yapa | IT23278530 | User Management and Authentication |
| Vidvanga W A U | IT23293694 | Prosumer Management |
| Nimsara R V P P | IT23215306 | Microgrid Node Management and project setup |
| L K P Yasith | IT23380196 | Energy Slot Reservation Management and Dashboards |

### M T A J Yapa (IT23278530): User Management and Authentication

- **API:** sign in and JWT tokens (`AuthController`, `AuthService`, `JwtTokenService`), BCrypt
  password hashing, staff user management (`UsersController`, `UserService`, `UserRepository`),
  the `User` model, roles, account status, and sign-in rate limiting.
- **Web:** Axios client with the token interceptor (`apiClient.js`), sign-in state
  (`AuthContext.jsx`), and role-protected routes (`ProtectedRoute.jsx`).
- **Android:** sign in (`LoginActivity`), SQLite session and profile storage (`DbHelper`,
  `SessionStore`, `ProfileStore`), the Retrofit client and token interceptors, session expiry
  handling, and the role-based home screens.

### Vidvanga W A U (IT23293694): Prosumer Management

- **Web:** prosumer management page (list, activate, deactivate, edit), prosumer self-registration
  page, and the profile page (`ProsumersPage.jsx`, `ProsumerFormModal.jsx`, `RegisterPage.jsx`,
  `ProfilePage.jsx`).
- **Android:** prosumer registration (`RegisterActivity`), profile view and edit, password change
  and deactivation request (`ProfileActivity`, `ProfileRepository`, `ProfileApi`).

### Nimsara R V P P (IT23215306): Microgrid Node Management and project setup

- **API:** stations and time slots (`StationsController`, `SlotsController`, `StationService`,
  `SlotService`, their repositories and models), nearby search (`GeoDistance`), time zone handling
  (`GridTime`), and BR-4.
- **Shared API base:** `Program.cs`, MongoDB context and indexes, the generic repository,
  `ServiceResult` and error handling, security headers, the first-admin seeder, demo data and
  the health check.
- **Web:** station list and detail pages, station form, app layout and shared components
  (`StationsPage.jsx`, `StationDetailPage.jsx`, `StationFormModal.jsx`, `AppLayout.jsx`).
- **Android:** map and nearby stations (`OsmMapActivity`, `MapActivity`, `NearbyNodesActivity`,
  `NodeDetailActivity`), station cache, QR scan result and transfer summary for the Grid
  Operator, app theme and network security settings.
- **Deployment:** IIS scripts (`deploy.ps1`, `configure-server.ps1`).

### L K P Yasith (IT23380196): Energy Slot Reservation Management and Dashboards

- **API:** prosumer and operator dashboards (`DashboardController`, `DashboardService` and their
  DTOs).
- **Web:** dashboard page, reservation management page and reservation form
  (`DashboardPage.jsx`, `ReservationsPage.jsx`, `ReservationFormModal.jsx`), status badges and
  confirm dialogs.
- **Android:** new booking, slot picker, booking summary, my bookings, change and cancel, and the
  booking QR code screen (`NewBookingActivity`, `MyBookingsActivity`, `BookingSummaryActivity`,
  `BookingQrActivity`, `BookingRepository`, `QrImages`).

The full plan of who owns what is in [docs/TEAM_WORK_DIVISION.md](docs/TEAM_WORK_DIVISION.md).
