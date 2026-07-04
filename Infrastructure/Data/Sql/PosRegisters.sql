SET QUOTED_IDENTIFIER ON;
GO

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
GO

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
    CREATE UNIQUE INDEX [IX_RegisterSessions_Register_Date]
        ON [RegisterSessions]([PosRegisterId],[SessionDate]) WHERE [IsDeleted] = 0;
    CREATE INDEX [IX_RegisterSessions_Branch_Closed]
        ON [RegisterSessions]([BusinessId],[BranchId],[IsClosed]);
END
GO
