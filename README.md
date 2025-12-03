# SOEEApp

**SOEEApp** is an ASP.NET MVC web application for managing Service Order Entry and Estimates (SOEE). The application allows users to create, edit, and track service orders, including detailed service items, charges, and taxes. It also supports dynamic calculation of service charges based on service type and slab mappings.

---

## Features

- **SOEE Management**
  - Create, edit, and view Service Order Entries
  - Add multiple service items per order
  - Soft-delete items while preserving history

- **Automatic Calculations**
  - Subtotal, service charge, CGST, SGST, and total calculation per item
  - Dynamic service charge percentage based on project and service type slabs
  - Grand total calculation for all active items

- **Dynamic UI**
  - Add/remove service items dynamically
  - Real-time calculation of totals in the UI
  - Auto-reindexing of item rows

- **Project and Customer Management**
  - Dropdown selection for projects and customers
  - Integration with service type and slab mappings

- **Audit Friendly**
  - Tracks deleted items without losing historical data

---

## Technology Stack

- **Backend:** ASP.NET MVC 5  
- **Database:** SQL Server (Entity Framework)  
- **Frontend:** HTML, CSS, JavaScript, jQuery, Bootstrap  
- **Version Control:** Git / GitHub

---

## Installation

1. Clone the repository:

```bash
git clone https://github.com/vndjkhr/SOEEApp.git
