# Saucy Integrated Reservation System 🍽️

A restaurant management web application built with ASP.NET Core MVC and MySQL. Saucy brings table reservations, food ordering, payment tracking, and restaurant administration into one system.

## Features

### Customers
- Register and sign in
- Browse the menu and customize sauce selections
- Add items to a shopping cart
- Reserve tables and check reservation status
- Place dine-in or takeout orders
- Track orders and view order history
- Submit payment details and request refunds

### Administrators
- View reports and analytics
- Manage reservations and dining tables
- Manage menu items and availability
- Monitor orders, payments, and refund requests
- Manage customer, admin, and staff accounts

### Staff
- View incoming orders
- Update order progress
- Review order history

## Technology Stack

- **Backend:** ASP.NET Core MVC, C#, .NET 8
- **Database:** MySQL with Entity Framework Core and Pomelo
- **Frontend:** Razor views, HTML, CSS, and JavaScript
- **Deployment:** Docker on Render
- **Database Hosting:** Aiven MySQL

## Requirements

For local development:

- .NET 8 SDK
- MySQL 8
- Git
- VS Code or Visual Studio

## Local Setup

### 1. Clone the repository

git clone https://github.com/raileytimple-dev/Saucy-Integrated-Reservation-System.git
cd Saucy-Integrated-Reservation-System/Saucy

### 2. Set up MySQL

Run `Database/setup_mysql.sql` in MySQL Workbench to create the local database.

Configure `ConnectionStrings:DefaultConnection` using .NET user secrets:

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=saucy_db;User=YOUR_USER;Password=YOUR_PASSWORD;"

Replace the username and password with your local MySQL credentials.

### 3. Start the application

dotnet restore
dotnet build
dotnet run

Open the address printed in the terminal. The default local HTTP address is:

http://localhost:5086

## Online Deployment

The application requires a server that supports ASP.NET Core or Docker, plus a MySQL database.

For Render, use:

| Setting | Value |
|---|---|
| Runtime | Docker |
| Root Directory | Saucy |
| Dockerfile Path | ./Dockerfile |
| Docker Build Context | . |

Configure these environment variables:

| Key | Value |
|---|---|
| ASPNETCORE_ENVIRONMENT | Production |
| ASPNETCORE_URLS | http://0.0.0.0:10000 |
| ConnectionStrings__DefaultConnection | Your MySQL connection string |

Example database connection format:

Server=YOUR_HOST;Port=YOUR_PORT;Database=YOUR_DATABASE;User=YOUR_USER;Password=YOUR_PASSWORD;SslMode=Required;

Use this semicolon-separated format rather than a `mysql://` URI.

GitHub Pages cannot run this application because it requires an ASP.NET backend.

## Project Structure

Saucy/
├── Controllers/       Request handling
├── Data/              Database context, initialization, and seeding
├── Database/          MySQL setup script
├── Models/            Application data models
├── Services/          Cart, order, and reservation logic
├── ViewModels/        Form and page models
├── Views/             Razor pages and layouts
├── wwwroot/           Styles, scripts, and images
├── Dockerfile         Container build instructions
└── Program.cs         Application startup

## Security and Deployment Notes

- Keep database credentials out of GitHub.
- Use hosting environment variables for production secrets.
- Replace or disable seeded demonstration accounts before public use.
- Use HTTPS for the deployed website.
- Test registration, reservations, checkout, and staff workflows before accepting real orders.
- Payment entry and tracking do not by themselves confirm integration with a payment gateway.
- Free hosting and database trial plans have usage and availability limits.

## Author

[raileytimple-dev](https://github.com/raileytimple-dev)
