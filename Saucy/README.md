# Saucy Herbs Cuisine — MVC + MySQL rebuild

This is a migration of the original **Saucy Integrated Reservation System** from ASP.NET Web Forms into **ASP.NET Core MVC (.NET 8) + C# + MySQL**.

The migration rule is **preserve the product, replace the architecture**:

- keep the Saucy branding, dark customer navbar/footer, original images and menu presentation;
- keep separate Customer, Admin and Staff experiences;
- keep the existing features and workflows;
- combine duplicate ASPX pages into MVC screens with filters/tabs instead of deleting functionality;
- use one normalized MySQL database instead of several inconsistent SQL Server/LocalDB connection strings.

## Requirements

Install:

1. .NET 8 SDK
2. VS Code
3. C# Dev Kit extension for VS Code
4. MySQL Server 8
5. MySQL Workbench

## 1. Create the database in MySQL Workbench

Open:

`Database/setup_mysql.sql`

Run the whole script. It creates a database named:

`saucy_db`

## 2. Set your MySQL login

Open `appsettings.json` and edit:

```json
"DefaultConnection": "Server=localhost;Port=3306;Database=saucy_db;User=root;Password=CHANGE_ME;"
```

Replace `CHANGE_ME` with the password you use when connecting through MySQL Workbench.

If your MySQL account is not `root`, change `User=root` too.

## 3. Run in VS Code

Open this folder in VS Code, then in the terminal:

```powershell
dotnet restore
dotnet build
dotnet run
```

Default HTTP address from `launchSettings.json`:

`http://localhost:5086`

## Default test accounts

When the database is empty, the application seeds:

- Admin: `admin@saucy.local` / `Admin123!`
- Staff: `staff@saucy.local` / `Staff123!`
- Customer: `customer@saucy.local` / `Customer123!`

Change these before any real deployment.

## Preserved customer features

- Home / Saucy branding and featured offerings
- Full menu with Famous Offerings, Pasta, and Sides & Drinks categories
- Shopping cart
- Customer registration and login
- Table reservation and automatic table assignment
- My Reservations and cancellation
- Checkout
- Payment entry
- Track Orders
- Order History
- Refund request
- About Us

## Preserved admin features

- Reports & Analytics dashboard
- Manage Reservations with status filters/actions
- Add/edit/enable/disable/delete dining tables
- Manage Orders and Order History
- Add/edit/enable/disable menu items
- Track Payments
- Refund Transactions
- Transaction History
- Customer Accounts
- Admin / Staff Accounts
- Inactive Accounts

## Preserved staff features

- Staff login
- Manage Orders
- Change order progress/status
- Order History

## ASPX reduction mapping

Old Web Forms pages that represented the same screen in different states are deliberately consolidated:

- `Reservation-ManageReservation.aspx`, `...Completed.aspx`, `...Cancelled.aspx` -> `Admin/Reservations` with filters
- `Orders-OrderHistory.aspx`, `Orders-OrderHistory-All.aspx` -> `Admin/Orders?status=Completed`
- `Management-ViewTrackPayments.aspx`, `...Completed.aspx` -> `Admin/Payments` with filters
- `Management-RefundTransaction.aspx`, `...Completed.aspx` -> `Admin/Refunds` with filters
- `Management-ViewTransactionHistory.aspx`, `...All.aspx` -> `Admin/Transactions` with Today / All filters
- separate Famous Offerings / Pasta / Sides & Drinks pages -> one `Menu` screen with category tabs
- Staff order variants -> `Staff/Orders` and `Staff/History`

This reduces file/page duplication without removing those user-facing states.

## Visual assets

Original image directories are copied under:

`wwwroot/legacy/`

The customer layout intentionally follows the original `Main.Master` visual direction. The admin/staff layouts intentionally follow the original `Site1.Master` / `Site2.Master` dark sidebar design with green hover state.

## Important

This environment could not run a real .NET build because the .NET SDK is unavailable here. The project has been statically checked for missing local files and basic brace/reference consistency, but your first `dotnet build` on Windows is the authoritative compile test. If it reports errors, send the complete terminal output and they can be corrected without changing the chosen MVC + MySQL architecture.
