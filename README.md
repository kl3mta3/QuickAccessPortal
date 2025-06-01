#  Remote Access Portal

A streamlined internal web application designed to simplify client management, remote access, and issue tracking. Built for IT teams who need fast, reliable, and secure access to their client infrastructure and support documentation — all in one place.

---

## Features

- **Client List Dashboard**
  - View and manage a full list of clients with intuitive search and filtering.
  - Click to open detailed client modals without navigating away from the page.

- **Knowledge Base Integration**
  - Centralized documentation for each client.
  - Stores credentials, software setups, preferences, and internal notes.
  - Editable in-place with version-safe fields.

- **Remote Access Links**
  - One-click access to RDP, VPN, or browser-based portals.
  - Links can be updated or customized per client.

- **Issue Tracker**
  - Reported issues are logged directly within the client profile.
  - Track issue history, resolution status, and technician notes.
  - Each alert includes timestamps and can be categorized by urgency or type.

- **Multi-Modal Design**
  - Shared modals support both Dashboard and Client Pages.
  - Context-aware controls (e.g., delete button only on full client edit screen).

- **Lightweight Config-Based Startup**
  - All runtime settings are pulled from a local JSON or config file.
  - Supports rapid environment changes without full rebuild.

---

## How It Works

### Adding Clients
1. Use the `Clients` page to open the “Add New Client” form.
2. Fill in:
   - Client Name
   - Knowledge Base info
   - Remote Access URLs
   - Any known issues or alerts
3. Save to immediately populate the client list.

### Accessing Client Info
- From the **Dashboard**, click on any client to open a **modal overlay** with:
  - Remote Access links
  - Internal KB fields
  - Issue logs
- From the **Clients Page**, click to fully edit or delete clients.

### Reporting Issues
- Within any client modal, click **“Report Issue”**.
- Describe the problem and categorize it (e.g., Network, Access, Software).
- Issues are timestamped and stored for reference or handoff.

---

## Configuration

All runtime paths and environment-specific details are loaded from:

```plaintext
app.config
Template Provided for filling in. 
```
This application was built as a functional concept during my time at The IT Support Center, but all code and structure remain my original work. It is shared here as part of my personal portfolio.

