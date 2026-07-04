using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Data;

public static class PosRegisterDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync("""
                SET QUOTED_IDENTIFIER ON;
                IF OBJECT_ID(N'[dbo].[PosRegisters]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[PosRegisters] (
                        [Id]                   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [BusinessId]           INT NOT NULL,
                        [BranchId]             INT NOT NULL,
                        [Name]                 NVARCHAR(100) NOT NULL,
                        [LinkedCashAccountId]  INT NOT NULL,
                        [IsActive]             BIT NOT NULL CONSTRAINT [DF_PosRegisters_IsActive] DEFAULT 1,
                        [IsDefault]            BIT NOT NULL CONSTRAINT [DF_PosRegisters_IsDefault] DEFAULT 0,
                        [CreatedDate]          DATETIME2 NOT NULL CONSTRAINT [DF_PosRegisters_CreatedDate] DEFAULT GETUTCDATE(),
                        [CreatedById]          INT NULL,
                        [UpdatedDate]          DATETIME2 NULL,
                        [ModifiedById]         INT NULL,
                        [IsDeleted]            BIT NOT NULL CONSTRAINT [DF_PosRegisters_IsDeleted] DEFAULT 0,
                        CONSTRAINT [FK_PosRegisters_Branches] FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id]),
                        CONSTRAINT [FK_PosRegisters_Accounts] FOREIGN KEY ([LinkedCashAccountId]) REFERENCES [Accounts]([Id])
                    );
                    CREATE UNIQUE INDEX [IX_PosRegisters_Business_Branch_Name]
                        ON [PosRegisters]([BusinessId],[BranchId],[Name]) WHERE [IsDeleted] = 0;
                END

                IF OBJECT_ID(N'[dbo].[RegisterSessions]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[RegisterSessions] (
                        [Id]                     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [BusinessId]             INT NOT NULL,
                        [BranchId]               INT NOT NULL,
                        [PosRegisterId]          INT NOT NULL,
                        [SessionDate]            DATE NOT NULL,
                        [OpeningBalance]         DECIMAL(18,2) NOT NULL,
                        [IsOpeningOverride]      BIT NOT NULL CONSTRAINT [DF_RegisterSessions_IsOpeningOverride] DEFAULT 0,
                        [OpeningOverrideReason]  NVARCHAR(500) NULL,
                        [OpenedBy]               INT NULL,
                        [OpenedAt]               DATETIME2 NOT NULL,
                        [ExpectedClosing]        DECIMAL(18,2) NULL,
                        [PhysicalCash]           DECIMAL(18,2) NULL,
                        [Difference]             DECIMAL(18,2) NULL,
                        [TotalCashSales]         DECIMAL(18,2) NOT NULL CONSTRAINT [DF_RegisterSessions_TotalCashSales] DEFAULT 0,
                        [TotalExpensesCash]      DECIMAL(18,2) NOT NULL CONSTRAINT [DF_RegisterSessions_TotalExpensesCash] DEFAULT 0,
                        [TotalCashIn]            DECIMAL(18,2) NOT NULL CONSTRAINT [DF_RegisterSessions_TotalCashIn] DEFAULT 0,
                        [TotalCashOut]           DECIMAL(18,2) NOT NULL CONSTRAINT [DF_RegisterSessions_TotalCashOut] DEFAULT 0,
                        [TotalAdjustments]       DECIMAL(18,2) NOT NULL CONSTRAINT [DF_RegisterSessions_TotalAdjustments] DEFAULT 0,
                        [IsClosed]               BIT NOT NULL CONSTRAINT [DF_RegisterSessions_IsClosed] DEFAULT 0,
                        [ClosedBy]               INT NULL,
                        [ClosedAt]               DATETIME2 NULL,
                        [CloseMismatchReason]    NVARCHAR(500) NULL,
                        [Notes]                  NVARCHAR(1000) NULL,
                        [CreatedDate]            DATETIME2 NOT NULL CONSTRAINT [DF_RegisterSessions_CreatedDate] DEFAULT GETUTCDATE(),
                        [CreatedById]            INT NULL,
                        [UpdatedDate]            DATETIME2 NULL,
                        [ModifiedById]           INT NULL,
                        [IsDeleted]              BIT NOT NULL CONSTRAINT [DF_RegisterSessions_IsDeleted] DEFAULT 0,
                        CONSTRAINT [FK_RegisterSessions_PosRegisters] FOREIGN KEY ([PosRegisterId]) REFERENCES [PosRegisters]([Id])
                    );
                    CREATE INDEX [IX_RegisterSessions_Register_Date]
                        ON [RegisterSessions]([PosRegisterId],[SessionDate]);
                    CREATE INDEX [IX_RegisterSessions_Branch_Closed]
                        ON [RegisterSessions]([BusinessId],[BranchId],[IsClosed]);
                    CREATE UNIQUE INDEX [UX_RegisterSessions_OpenPerRegister]
                        ON [RegisterSessions]([PosRegisterId]) WHERE [IsClosed] = 0 AND [IsDeleted] = 0;
                END

                -- Upgrade older installs: replace the one-session-per-day unique index
                -- with an open-session-only unique index so multiple sessions per day are allowed.
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RegisterSessions_Register_Date'
                           AND object_id = OBJECT_ID(N'[dbo].[RegisterSessions]') AND is_unique = 1)
                BEGIN
                    DROP INDEX [IX_RegisterSessions_Register_Date] ON [RegisterSessions];
                    CREATE INDEX [IX_RegisterSessions_Register_Date]
                        ON [RegisterSessions]([PosRegisterId],[SessionDate]);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_RegisterSessions_OpenPerRegister'
                               AND object_id = OBJECT_ID(N'[dbo].[RegisterSessions]'))
                BEGIN
                    CREATE UNIQUE INDEX [UX_RegisterSessions_OpenPerRegister]
                        ON [RegisterSessions]([PosRegisterId]) WHERE [IsClosed] = 0 AND [IsDeleted] = 0;
                END
                """);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pos register schema patch failed.");
            throw;
        }
    }

    /// <summary>Creates a default register per branch linked to the main Cash GL account.</summary>
    public static async Task SeedDefaultRegistersAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            var cashAccountId = await context.GlAccounts
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.Name == GlAccountDefaults.Cash)
                .Select(a => a.Id)
                .FirstOrDefaultAsync();

            if (cashAccountId <= 0)
                return;

            var branches = await context.Branches
                .AsNoTracking()
                .Where(b => !b.IsDeleted && b.IsActive)
                .Select(b => new { b.Id, b.BusinessId })
                .ToListAsync();

            foreach (var branch in branches)
            {
                var exists = await context.PosRegisters
                    .AnyAsync(r => !r.IsDeleted && r.BranchId == branch.Id && r.IsDefault);

                if (exists)
                    continue;

                await context.PosRegisters.AddAsync(new PosRegister
                {
                    BusinessId = branch.BusinessId,
                    BranchId = branch.Id,
                    Name = "Main Counter",
                    LinkedCashAccountId = cashAccountId,
                    IsActive = true,
                    IsDefault = true,
                });
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed default POS registers.");
        }
    }
}
