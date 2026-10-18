using System.Data;
using Microsoft.EntityFrameworkCore;

namespace SaucyMVC.Data;

public static class DatabaseSchemaUpdater
{
    private static readonly (string Name, string Definition)[] DiningTableColumns =
    [
        ("OperationalStatus", "VARCHAR(25) NOT NULL DEFAULT 'Available'"),
        ("OccupiedUntil", "DATETIME(6) NULL"),
        ("CurrentPartySize", "INT NULL")
    ];

    private static readonly (string Name, string Definition)[] OrderWorkflowColumns =
    [
        ("CancellationRequestStatus", "VARCHAR(25) NULL"),
        ("CancellationReason", "VARCHAR(500) NULL"),
        ("CancellationRequestedAt", "DATETIME(6) NULL"),
        ("CancellationRequestedByUserId", "INT NULL")
    ];

    private static readonly (string Name, string Definition)[] ReservationAutomationColumns =
    [
        ("HoldExpiresAt", "DATETIME(6) NULL"),
        ("ConfirmedAt", "DATETIME(6) NULL"),
        ("CheckedInAt", "DATETIME(6) NULL"),
        ("ReleasedAt", "DATETIME(6) NULL")
    ];

    public static async Task EnsureCurrentSchemaAsync(SaucyDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync();

        try
        {
            foreach (var column in DiningTableColumns)
            {
                await EnsureColumnAsync(connection, "DiningTables", column.Name, column.Definition);
            }

            foreach (var column in OrderWorkflowColumns)
            {
                await EnsureColumnAsync(connection, "Orders", column.Name, column.Definition);
            }

            foreach (var column in ReservationAutomationColumns)
            {
                await EnsureColumnAsync(connection, "Reservations", column.Name, column.Definition);
            }

            await using (var backfillReservationAutomation = connection.CreateCommand())
            {
                backfillReservationAutomation.CommandText = """
                    UPDATE `Reservations`
                    SET `HoldExpiresAt` = DATE_ADD(`CreatedAt`, INTERVAL 15 MINUTE)
                    WHERE `Status` = 'Pending' AND `HoldExpiresAt` IS NULL;
                    """;
                await backfillReservationAutomation.ExecuteNonQueryAsync();
            }

            await using (var normalizeAcceptedReservations = connection.CreateCommand())
            {
                normalizeAcceptedReservations.CommandText = """
                    UPDATE `Reservations`
                    SET `Status` = 'Confirmed',
                        `ConfirmedAt` = COALESCE(`ConfirmedAt`, `CreatedAt`),
                        `HoldExpiresAt` = NULL
                    WHERE `Status` = 'Accepted';
                    """;
                await normalizeAcceptedReservations.ExecuteNonQueryAsync();
            }

            await using (var backfillConfirmedAt = connection.CreateCommand())
            {
                backfillConfirmedAt.CommandText = """
                    UPDATE `Reservations`
                    SET `ConfirmedAt` = COALESCE(`ConfirmedAt`, `CreatedAt`),
                        `HoldExpiresAt` = NULL
                    WHERE `Status` = 'Confirmed';
                    """;
                await backfillConfirmedAt.ExecuteNonQueryAsync();
            }

            await using (var createSauces = connection.CreateCommand())
            {
                createSauces.CommandText = """
                    CREATE TABLE IF NOT EXISTS `SauceOptions` (
                      `Id` INT NOT NULL AUTO_INCREMENT,
                      `Name` VARCHAR(80) NOT NULL,
                      `ImagePath` VARCHAR(400) NOT NULL,
                      `IsAvailable` TINYINT(1) NOT NULL DEFAULT 1,
                      `DisplayOrder` INT NOT NULL DEFAULT 0,
                      PRIMARY KEY (`Id`),
                      UNIQUE KEY `IX_SauceOptions_ImagePath` (`ImagePath`)
                    ) ENGINE=InnoDB;
                    """;
                await createSauces.ExecuteNonQueryAsync();
            }

            await EnsureColumnAsync(connection, "OrderItems", "Sauce1Name", "VARCHAR(80) NULL");
            await EnsureColumnAsync(connection, "OrderItems", "Sauce2Name", "VARCHAR(80) NULL");

            await using (var createOrderStatusHistory = connection.CreateCommand())
            {
                createOrderStatusHistory.CommandText = """
                    CREATE TABLE IF NOT EXISTS `OrderStatusHistory` (
                      `Id` INT NOT NULL AUTO_INCREMENT,
                      `OrderId` INT NOT NULL,
                      `FromStatus` VARCHAR(25) NOT NULL DEFAULT '',
                      `ToStatus` VARCHAR(25) NOT NULL,
                      `Action` VARCHAR(50) NOT NULL DEFAULT 'Status changed',
                      `ChangedByUserId` INT NULL,
                      `ChangedByName` VARCHAR(120) NOT NULL DEFAULT 'System',
                      `ChangedByRole` VARCHAR(20) NOT NULL DEFAULT 'System',
                      `ChangedAt` DATETIME(6) NOT NULL,
                      `Note` VARCHAR(500) NULL,
                      PRIMARY KEY (`Id`),
                      KEY `IX_OrderStatusHistory_OrderId` (`OrderId`),
                      CONSTRAINT `FK_OrderStatusHistory_Orders` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE
                    ) ENGINE=InnoDB;
                    """;
                await createOrderStatusHistory.ExecuteNonQueryAsync();
            }

            await using (var recordCashStatusRepairs = connection.CreateCommand())
            {
                recordCashStatusRepairs.CommandText = """
                    INSERT INTO `OrderStatusHistory`
                        (`OrderId`, `FromStatus`, `ToStatus`, `Action`, `ChangedByName`, `ChangedByRole`, `ChangedAt`, `Note`)
                    SELECT `o`.`Id`, `o`.`Status`, 'Pending', 'Cash verification repair', 'System', 'System', NOW(6),
                           'Cash order returned to Pending until counter payment is verified.'
                    FROM `Orders` AS `o`
                    INNER JOIN `Payments` AS `p` ON `p`.`OrderId` = `o`.`Id`
                    WHERE `p`.`Method` = 'Cash' AND `p`.`Status` = 'Pending' AND `o`.`Status` = 'Confirmed';
                    """;
                await recordCashStatusRepairs.ExecuteNonQueryAsync();
            }

            await using (var repairCashOrderStatuses = connection.CreateCommand())
            {
                repairCashOrderStatuses.CommandText = """
                    UPDATE `Orders` AS `o`
                    INNER JOIN `Payments` AS `p` ON `p`.`OrderId` = `o`.`Id`
                    SET `o`.`Status` = 'Pending'
                    WHERE `p`.`Method` = 'Cash' AND `p`.`Status` = 'Pending' AND `o`.`Status` = 'Confirmed';
                    """;
                await repairCashOrderStatuses.ExecuteNonQueryAsync();
            }

            await using (var backfillOrderStatusHistory = connection.CreateCommand())
            {
                backfillOrderStatusHistory.CommandText = """
                    INSERT INTO `OrderStatusHistory`
                        (`OrderId`, `FromStatus`, `ToStatus`, `Action`, `ChangedByName`, `ChangedByRole`, `ChangedAt`, `Note`)
                    SELECT `o`.`Id`, '', `o`.`Status`, 'Existing status', 'System', 'System', `o`.`OrderedAt`,
                           'Order existed before status tracking was enabled.'
                    FROM `Orders` AS `o`
                    WHERE NOT EXISTS (
                        SELECT 1 FROM `OrderStatusHistory` AS `h` WHERE `h`.`OrderId` = `o`.`Id`
                    );
                    """;
                await backfillOrderStatusHistory.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    private static async Task EnsureColumnAsync(
        System.Data.Common.DbConnection connection,
        string tableName,
        string columnName,
        string definition)
    {
        await using var check = connection.CreateCommand();
        check.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = @tableName
              AND column_name = @columnName
            """;
        var tableParameter = check.CreateParameter();
        tableParameter.ParameterName = "@tableName";
        tableParameter.Value = tableName;
        check.Parameters.Add(tableParameter);
        var columnParameter = check.CreateParameter();
        columnParameter.ParameterName = "@columnName";
        columnParameter.Value = columnName;
        check.Parameters.Add(columnParameter);

        if (Convert.ToInt32(await check.ExecuteScalarAsync()) > 0) return;

        await using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE `{tableName}` ADD COLUMN `{columnName}` {definition}";
        await alter.ExecuteNonQueryAsync();
    }
}
