using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoubleEntryAccountingPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[Accounts]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[Accounts] (
                        [Id]           INT            IDENTITY(1,1) NOT NULL,
                        [BusinessId]   INT            NOT NULL CONSTRAINT [DF_Accounts_BusinessId] DEFAULT 1,
                        [BranchId]     INT            NOT NULL CONSTRAINT [DF_Accounts_BranchId] DEFAULT 1,
                        [Name]         NVARCHAR(200)  NOT NULL,
                        [Type]         INT            NOT NULL,
                        [ParentId]     INT            NULL,
                        [IsActive]     BIT            NOT NULL CONSTRAINT [DF_Accounts_IsActive] DEFAULT 1,
                        [CreatedDate]  DATETIME2      NOT NULL CONSTRAINT [DF_Accounts_CreatedDate] DEFAULT GETUTCDATE(),
                        [CreatedById]  INT            NULL,
                        [UpdatedDate]  DATETIME2      NULL,
                        [ModifiedById] INT            NULL,
                        [IsDeleted]    BIT            NOT NULL CONSTRAINT [DF_Accounts_IsDeleted] DEFAULT 0,
                        CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Accounts_Branches] FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id]),
                        CONSTRAINT [FK_Accounts_Parent] FOREIGN KEY ([ParentId]) REFERENCES [Accounts]([Id])
                    );
                    CREATE INDEX [idx_accounts_businessid] ON [Accounts]([BusinessId]);
                    CREATE INDEX [idx_accounts_branchid] ON [Accounts]([BranchId]);
                    CREATE INDEX [idx_accounts_business_branch] ON [Accounts]([BusinessId], [BranchId]);
                    CREATE INDEX [idx_accounts_business_branch_name] ON [Accounts]([BusinessId], [BranchId], [Name]);
                    CREATE INDEX [idx_accounts_parentid] ON [Accounts]([ParentId]);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[Transactions] (
                        [Id]              INT              IDENTITY(1,1) NOT NULL,
                        [Date]            DATETIME2        NOT NULL,
                        [AccountId]       INT              NOT NULL,
                        [DebitAmount]     DECIMAL(18,2)    NOT NULL CONSTRAINT [DF_Transactions_DebitAmount] DEFAULT 0,
                        [CreditAmount]    DECIMAL(18,2)    NOT NULL CONSTRAINT [DF_Transactions_CreditAmount] DEFAULT 0,
                        [TransactionType] INT              NOT NULL,
                        [ReferenceId]     INT              NULL,
                        [GroupId]         UNIQUEIDENTIFIER NOT NULL,
                        [Description]     NVARCHAR(500)    NULL,
                        [CreatedAt]       DATETIME2        NOT NULL CONSTRAINT [DF_Transactions_CreatedAt] DEFAULT GETUTCDATE(),
                        CONSTRAINT [PK_Transactions] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Transactions_Accounts] FOREIGN KEY ([AccountId]) REFERENCES [Accounts]([Id])
                    );
                    CREATE INDEX [idx_transactions_accountid] ON [Transactions]([AccountId]);
                    CREATE INDEX [idx_transactions_date] ON [Transactions]([Date]);
                    CREATE INDEX [idx_transactions_groupid] ON [Transactions]([GroupId]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
                    DROP TABLE [dbo].[Transactions];
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[Accounts]', N'U') IS NOT NULL
                    DROP TABLE [dbo].[Accounts];
                """);
        }
    }
}
