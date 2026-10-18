-- Saucy Herbs Cuisine - MySQL 8 setup
-- Run this in MySQL Workbench before starting the ASP.NET Core MVC app.
CREATE DATABASE IF NOT EXISTS saucy_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE saucy_db;

CREATE TABLE IF NOT EXISTS Users (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  FullName VARCHAR(120) NOT NULL,
  Email VARCHAR(160) NOT NULL,
  PhoneNumber VARCHAR(30) NOT NULL DEFAULT '',
  PasswordHash LONGTEXT NOT NULL,
  Role VARCHAR(20) NOT NULL DEFAULT 'Customer',
  IsActive TINYINT(1) NOT NULL DEFAULT 1,
  CreatedAt DATETIME(6) NOT NULL,
  LastLoginAt DATETIME(6) NULL,
  UNIQUE KEY IX_Users_Email (Email)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS DiningTables (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  TableNumber VARCHAR(30) NOT NULL,
  Capacity INT NOT NULL,
  IsActive TINYINT(1) NOT NULL DEFAULT 1,
  OperationalStatus VARCHAR(25) NOT NULL DEFAULT 'Available',
  OccupiedUntil DATETIME(6) NULL,
  CurrentPartySize INT NULL,
  UNIQUE KEY IX_DiningTables_TableNumber (TableNumber)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS MenuItems (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  Name VARCHAR(120) NOT NULL,
  Category VARCHAR(60) NOT NULL,
  Description VARCHAR(500) NOT NULL DEFAULT '',
  Price DECIMAL(10,2) NOT NULL,
  IsAvailable TINYINT(1) NOT NULL DEFAULT 1,
  ImagePath VARCHAR(400) NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS SauceOptions (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  Name VARCHAR(80) NOT NULL,
  ImagePath VARCHAR(400) NOT NULL,
  IsAvailable TINYINT(1) NOT NULL DEFAULT 1,
  DisplayOrder INT NOT NULL DEFAULT 0,
  UNIQUE KEY IX_SauceOptions_ImagePath (ImagePath)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Reservations (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  UserId INT NOT NULL,
  DiningTableId INT NULL,
  ReservationDate DATETIME(6) NOT NULL,
  GuestCount INT NOT NULL,
  Status VARCHAR(25) NOT NULL DEFAULT 'Pending',
  Notes VARCHAR(500) NULL,
  PaymentProofPath VARCHAR(400) NULL,
  CreatedAt DATETIME(6) NOT NULL,
  HoldExpiresAt DATETIME(6) NULL,
  ConfirmedAt DATETIME(6) NULL,
  CheckedInAt DATETIME(6) NULL,
  ReleasedAt DATETIME(6) NULL,
  CONSTRAINT FK_Reservations_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
  CONSTRAINT FK_Reservations_DiningTables FOREIGN KEY (DiningTableId) REFERENCES DiningTables(Id) ON DELETE SET NULL,
  KEY IX_Reservations_UserId (UserId), KEY IX_Reservations_DiningTableId (DiningTableId)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Orders (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  UserId INT NOT NULL,
  ReservationId INT NULL,
  OrderedAt DATETIME(6) NOT NULL,
  Status LONGTEXT NOT NULL,
  TotalAmount DECIMAL(10,2) NOT NULL,
  OrderType LONGTEXT NOT NULL,
  Notes LONGTEXT NULL,
  CancellationRequestStatus VARCHAR(25) NULL,
  CancellationReason VARCHAR(500) NULL,
  CancellationRequestedAt DATETIME(6) NULL,
  CancellationRequestedByUserId INT NULL,
  CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
  CONSTRAINT FK_Orders_Reservations FOREIGN KEY (ReservationId) REFERENCES Reservations(Id) ON DELETE SET NULL,
  KEY IX_Orders_UserId (UserId), KEY IX_Orders_ReservationId (ReservationId)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS OrderStatusHistory (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  OrderId INT NOT NULL,
  FromStatus VARCHAR(25) NOT NULL DEFAULT '',
  ToStatus VARCHAR(25) NOT NULL,
  Action VARCHAR(50) NOT NULL DEFAULT 'Status changed',
  ChangedByUserId INT NULL,
  ChangedByName VARCHAR(120) NOT NULL DEFAULT 'System',
  ChangedByRole VARCHAR(20) NOT NULL DEFAULT 'System',
  ChangedAt DATETIME(6) NOT NULL,
  Note VARCHAR(500) NULL,
  CONSTRAINT FK_OrderStatusHistory_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
  KEY IX_OrderStatusHistory_OrderId (OrderId)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS OrderItems (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  OrderId INT NOT NULL,
  MenuItemId INT NOT NULL,
  Quantity INT NOT NULL,
  UnitPrice DECIMAL(10,2) NOT NULL,
  Sauce1Name VARCHAR(80) NULL,
  Sauce2Name VARCHAR(80) NULL,
  CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
  CONSTRAINT FK_OrderItems_MenuItems FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id) ON DELETE CASCADE,
  KEY IX_OrderItems_OrderId (OrderId), KEY IX_OrderItems_MenuItemId (MenuItemId)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Payments (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  UserId INT NOT NULL,
  OrderId INT NULL,
  ReservationId INT NULL,
  Amount DECIMAL(10,2) NOT NULL,
  Method LONGTEXT NOT NULL,
  Status LONGTEXT NOT NULL,
  ReferenceNumber LONGTEXT NULL,
  CreatedAt DATETIME(6) NOT NULL,
  CONSTRAINT FK_Payments_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
  CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE SET NULL,
  CONSTRAINT FK_Payments_Reservations FOREIGN KEY (ReservationId) REFERENCES Reservations(Id) ON DELETE SET NULL,
  KEY IX_Payments_UserId (UserId), KEY IX_Payments_OrderId (OrderId), KEY IX_Payments_ReservationId (ReservationId)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS RefundRequests (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  UserId INT NOT NULL,
  OrderId INT NOT NULL,
  Amount DECIMAL(10,2) NOT NULL,
  Description LONGTEXT NOT NULL,
  Status LONGTEXT NOT NULL,
  RequestedAt DATETIME(6) NOT NULL,
  CONSTRAINT FK_RefundRequests_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
  CONSTRAINT FK_RefundRequests_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
  KEY IX_RefundRequests_UserId (UserId), KEY IX_RefundRequests_OrderId (OrderId)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS TransactionHistory (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  UserId INT NOT NULL,
  OrderId INT NULL,
  TotalAmount DECIMAL(10,2) NOT NULL,
  OrderItems LONGTEXT NOT NULL,
  PaymentMethod LONGTEXT NOT NULL,
  TransactionDate DATETIME(6) NOT NULL,
  CONSTRAINT FK_TransactionHistory_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
  KEY IX_TransactionHistory_UserId (UserId)
) ENGINE=InnoDB;

-- The MVC app seeds its default admin/staff/customer and sample menu/table rows
-- automatically when these tables are empty.
