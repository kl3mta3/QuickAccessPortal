# Quick Access Portal

A secure, streamlined internal web application for managing clients, credentials, remote systems, and support documentation. Built for IT teams who need fast, reliable, and safe access to infrastructure and client records — all in one place.

---

## Features

### User Authentication & Role Control
- Secure logon system with PBKDF2-hashed passwords and SHA-512-based API key validation.
- Only authenticated users can access the portal.
- Role-based access with admin-only permissions for user management.

### User Management
- Admins can add, edit, or delete users from the **Users** page.
- Password requirements enforced (uppercase, lowercase, number, 8–24 chars).
- Optional “Remember Username” for faster logins on client devices.
- Real-time validation and toast feedback built in.

### Client List Dashboard
- View and manage a full list of clients with intuitive search and filtering.
- Click to open detailed client modals without navigating away from the page.

### Knowledge Base Integration
- Centralized documentation for each client.
- Stores credentials, software setups, preferences, and internal notes.
- Editable in-place with version-safe fields.

### Remote Access Links
- One-click access to RDP, VPN, or browser-based portals.
- Customizable and secure per-client.

### Issue Tracker
- Log and track issues directly within client profiles.
- Each alert includes timestamps and categorization by urgency or issue type.
- Technician notes and resolution history included.

### Multi-Modal UI Design
- Shared modals are used across Dashboard and Client pages.
- Context-aware logic: e.g., Delete button only appears in full Client Edit view.

### ⚙Lightweight Config-Based Startup
- All runtime settings loaded from a local JSON config file (`app.config` or equivalent).
- Supports easy switching between environments without rebuilds.

---

## How It Works

### Adding Users (Admins Only)
1. Navigate to the `Users` page.
2. Click **Add User**.
3. Provide:
   - Full Name
   - Username (email-style recommended)
   - Role (`Is Admin` toggle)
   - Secure Password (meets all policy rules)
4. Save to grant immediate access.

### Logging In
- On app launch, users are prompted to log in.
- The application uses:
  - **Hashed password transmission**
  - **API key header validation**
  - **Username normalization** (case-insensitive matching)
- Users without valid credentials or keys are rejected.

### Editing Users
- Admins can update name, username, or password.
- Admin accounts are protected from deletion.
- Password changes require validation and immediate effect.
---

## Configuration

All runtime paths and environment-specific details are loaded from:

```plaintext
app.config
Template Provided. 
```
## About This Project
This application was designed and built during my time working in internal IT support, and while used in a real environment, all code and architectural decisions are my own. It is shared here as part of my personal portfolio to demonstrate full-stack architecture, secure authentication flows, and a responsive multi-role UI system.

