IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Branches] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [City] nvarchar(100) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [OpeningTime] time NOT NULL,
    [ClosingTime] time NOT NULL,
    [TaxRate] decimal(5,2) NOT NULL,
    [Currency] nvarchar(10) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_Branches] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Customers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [LoyaltyPoints] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Customers_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [InventoryItems] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    [Unit] nvarchar(50) NOT NULL,
    [CurrentStock] decimal(12,2) NOT NULL,
    [MinStockLevel] decimal(12,2) NOT NULL,
    [PurchasePrice] decimal(10,2) NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_InventoryItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryItems_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [MenuCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_MenuCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuCategories_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Permissions] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Roles_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Tables] (
    [Id] int NOT NULL IDENTITY,
    [TableNumber] int NOT NULL,
    [Capacity] int NOT NULL,
    [Status] int NOT NULL,
    [Floor] nvarchar(50) NOT NULL,
    [IsQrEnabled] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_Tables] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tables_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [StockMovements] (
    [Id] int NOT NULL IDENTITY,
    [ItemId] int NOT NULL,
    [Quantity] decimal(12,2) NOT NULL,
    [Type] int NOT NULL,
    [BranchFromId] int NULL,
    [BranchToId] int NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_StockMovements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockMovements_Branches_BranchFromId] FOREIGN KEY ([BranchFromId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockMovements_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockMovements_Branches_BranchToId] FOREIGN KEY ([BranchToId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockMovements_InventoryItems_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [InventoryItems] ([Id])
);
GO

CREATE TABLE [MenuItems] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [Price] decimal(10,2) NOT NULL,
    [CostPrice] decimal(10,2) NOT NULL,
    [TaxPercentage] decimal(5,2) NOT NULL,
    [PreparationTime] int NOT NULL,
    [ImageUrl] nvarchar(500) NOT NULL,
    [IsAvailable] bit NOT NULL,
    [IsVeg] bit NOT NULL,
    [MenuCategoryId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuItems_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MenuItems_MenuCategories_MenuCategoryId] FOREIGN KEY ([MenuCategoryId]) REFERENCES [MenuCategories] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [RoleId] int NOT NULL,
    [Salary] decimal(10,2) NOT NULL,
    [ShiftType] int NOT NULL,
    [Status] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [MenuItemVariants] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Price] decimal(10,2) NOT NULL,
    [MenuItemId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_MenuItemVariants] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuItemVariants_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MenuItemVariants_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Recipes] (
    [Id] int NOT NULL IDENTITY,
    [MenuItemId] int NOT NULL,
    [IngredientId] int NOT NULL,
    [QuantityRequired] decimal(10,2) NOT NULL,
    [Unit] nvarchar(50) NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_Recipes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Recipes_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Recipes_InventoryItems_IngredientId] FOREIGN KEY ([IngredientId]) REFERENCES [InventoryItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Recipes_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [OrderType] int NOT NULL,
    [TableId] int NULL,
    [CustomerId] int NULL,
    [WaiterId] int NULL,
    [Status] int NOT NULL,
    [Notes] nvarchar(500) NOT NULL,
    [Subtotal] decimal(10,2) NOT NULL,
    [Discount] decimal(10,2) NOT NULL,
    [Tax] decimal(10,2) NOT NULL,
    [TotalAmount] decimal(10,2) NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Orders_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Orders_Tables_TableId] FOREIGN KEY ([TableId]) REFERENCES [Tables] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Orders_Users_WaiterId] FOREIGN KEY ([WaiterId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [OrderItems] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [MenuItemId] int NOT NULL,
    [Quantity] int NOT NULL,
    [Price] decimal(10,2) NOT NULL,
    [Discount] decimal(10,2) NOT NULL,
    [Total] decimal(10,2) NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderItems_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItems_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id])
);
GO

CREATE TABLE [Payments] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [Method] nvarchar(50) NOT NULL,
    [Amount] decimal(10,2) NOT NULL,
    [Status] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Payments_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Payments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id])
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'BranchId', N'City', N'ClosingTime', N'Code', N'CreatedById', N'CreatedByName', N'CreatedDate', N'Currency', N'Email', N'IsActive', N'IsDeleted', N'ModifiedById', N'ModifiedByName', N'Name', N'OpeningTime', N'Phone', N'TaxRate', N'UpdatedDate') AND [object_id] = OBJECT_ID(N'[Branches]'))
    SET IDENTITY_INSERT [Branches] ON;
INSERT INTO [Branches] ([Id], [Address], [BranchId], [City], [ClosingTime], [Code], [CreatedById], [CreatedByName], [CreatedDate], [Currency], [Email], [IsActive], [IsDeleted], [ModifiedById], [ModifiedByName], [Name], [OpeningTime], [Phone], [TaxRate], [UpdatedDate])
VALUES (1, N'123 Main Street', 1, N'Default City', '22:00:00', N'MAIN', NULL, NULL, '2026-04-18T11:00:07.2117230Z', N'USD', N'main@restaurant.com', CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, N'Main Branch', '11:00:00', N'+1234567890', 10.0, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'BranchId', N'City', N'ClosingTime', N'Code', N'CreatedById', N'CreatedByName', N'CreatedDate', N'Currency', N'Email', N'IsActive', N'IsDeleted', N'ModifiedById', N'ModifiedByName', N'Name', N'OpeningTime', N'Phone', N'TaxRate', N'UpdatedDate') AND [object_id] = OBJECT_ID(N'[Branches]'))
    SET IDENTITY_INSERT [Branches] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BranchId', N'CreatedById', N'CreatedByName', N'CreatedDate', N'IsDeleted', N'ModifiedById', N'ModifiedByName', N'Name', N'Permissions', N'UpdatedDate') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [BranchId], [CreatedById], [CreatedByName], [CreatedDate], [IsDeleted], [ModifiedById], [ModifiedByName], [Name], [Permissions], [UpdatedDate])
VALUES (1, 1, NULL, NULL, '2026-04-18T11:00:07.2117428Z', CAST(0 AS bit), NULL, NULL, N'Admin', N'all_permissions', NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BranchId', N'CreatedById', N'CreatedByName', N'CreatedDate', N'IsDeleted', N'ModifiedById', N'ModifiedByName', N'Name', N'Permissions', N'UpdatedDate') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BranchId', N'CreatedById', N'CreatedByName', N'CreatedDate', N'Email', N'FullName', N'IsDeleted', N'ModifiedById', N'ModifiedByName', N'PasswordHash', N'Phone', N'RoleId', N'Salary', N'ShiftType', N'Status', N'UpdatedDate', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] ([Id], [BranchId], [CreatedById], [CreatedByName], [CreatedDate], [Email], [FullName], [IsDeleted], [ModifiedById], [ModifiedByName], [PasswordHash], [Phone], [RoleId], [Salary], [ShiftType], [Status], [UpdatedDate], [Username])
VALUES (1, 1, NULL, NULL, '2026-04-18T11:00:07.2117477Z', N'admin@restaurant.com', N'System Administrator', CAST(0 AS bit), NULL, NULL, N'$2a$11$QvHz8.HeIU5ThFqjVPVVe.sTuKqDQI6R0nrPz/Z8KqK8qXyxi3H7O', N'+1234567890', 1, 0.0, 4, 0, NULL, N'admin');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BranchId', N'CreatedById', N'CreatedByName', N'CreatedDate', N'Email', N'FullName', N'IsDeleted', N'ModifiedById', N'ModifiedByName', N'PasswordHash', N'Phone', N'RoleId', N'Salary', N'ShiftType', N'Status', N'UpdatedDate', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] OFF;
GO

CREATE UNIQUE INDEX [idx_branch_code] ON [Branches] ([Code]);
GO

CREATE INDEX [idx_branch_email] ON [Branches] ([Email]);
GO

CREATE INDEX [idx_branch_phone] ON [Branches] ([Phone]);
GO

CREATE UNIQUE INDEX [idx_customer_branch_phone] ON [Customers] ([BranchId], [Phone]);
GO

CREATE INDEX [idx_customer_branchid] ON [Customers] ([BranchId]);
GO

CREATE INDEX [idx_customer_email] ON [Customers] ([Email]);
GO

CREATE INDEX [idx_customer_phone] ON [Customers] ([Phone]);
GO

CREATE UNIQUE INDEX [idx_inventoryitem_branch_name] ON [InventoryItems] ([BranchId], [Name]);
GO

CREATE INDEX [idx_inventoryitem_branchid] ON [InventoryItems] ([BranchId]);
GO

CREATE INDEX [idx_inventoryitem_category] ON [InventoryItems] ([Category]);
GO

CREATE UNIQUE INDEX [idx_menucategory_branch_name] ON [MenuCategories] ([BranchId], [Name]);
GO

CREATE INDEX [idx_menucategory_branchid] ON [MenuCategories] ([BranchId]);
GO

CREATE INDEX [idx_menuitem_branch_available] ON [MenuItems] ([BranchId], [IsAvailable]);
GO

CREATE INDEX [idx_menuitem_branchid] ON [MenuItems] ([BranchId]);
GO

CREATE INDEX [idx_menuitem_categoryid] ON [MenuItems] ([MenuCategoryId]);
GO

CREATE INDEX [idx_variant_branchid] ON [MenuItemVariants] ([BranchId]);
GO

CREATE INDEX [idx_variant_menuitemid] ON [MenuItemVariants] ([MenuItemId]);
GO

CREATE INDEX [idx_orderitem_branchid] ON [OrderItems] ([BranchId]);
GO

CREATE INDEX [idx_orderitem_menuitemid] ON [OrderItems] ([MenuItemId]);
GO

CREATE INDEX [idx_orderitem_orderid] ON [OrderItems] ([OrderId]);
GO

CREATE INDEX [idx_order_branchid] ON [Orders] ([BranchId]);
GO

CREATE INDEX [idx_order_branchid_createddate] ON [Orders] ([BranchId], [CreatedDate]);
GO

CREATE INDEX [idx_order_customerid] ON [Orders] ([CustomerId]);
GO

CREATE INDEX [idx_order_status] ON [Orders] ([Status]);
GO

CREATE INDEX [idx_order_tableid] ON [Orders] ([TableId]);
GO

CREATE INDEX [idx_order_waiterid] ON [Orders] ([WaiterId]);
GO

CREATE INDEX [idx_payment_branch_createddate] ON [Payments] ([BranchId], [CreatedDate]);
GO

CREATE INDEX [idx_payment_branchid] ON [Payments] ([BranchId]);
GO

CREATE INDEX [idx_payment_orderid] ON [Payments] ([OrderId]);
GO

CREATE INDEX [idx_payment_status] ON [Payments] ([Status]);
GO

CREATE INDEX [idx_recipe_branchid] ON [Recipes] ([BranchId]);
GO

CREATE INDEX [idx_recipe_ingredientid] ON [Recipes] ([IngredientId]);
GO

CREATE UNIQUE INDEX [idx_recipe_menuitem_ingredient] ON [Recipes] ([MenuItemId], [IngredientId]);
GO

CREATE INDEX [idx_recipe_menuitemid] ON [Recipes] ([MenuItemId]);
GO

CREATE UNIQUE INDEX [idx_role_branchid_name] ON [Roles] ([BranchId], [Name]);
GO

CREATE INDEX [idx_stockmovement_branch_createddate] ON [StockMovements] ([BranchId], [CreatedDate]);
GO

CREATE INDEX [idx_stockmovement_branchid] ON [StockMovements] ([BranchId]);
GO

CREATE INDEX [idx_stockmovement_itemid] ON [StockMovements] ([ItemId]);
GO

CREATE INDEX [idx_stockmovement_type] ON [StockMovements] ([Type]);
GO

CREATE INDEX [IX_StockMovements_BranchFromId] ON [StockMovements] ([BranchFromId]);
GO

CREATE INDEX [IX_StockMovements_BranchToId] ON [StockMovements] ([BranchToId]);
GO

CREATE UNIQUE INDEX [idx_table_branch_number] ON [Tables] ([BranchId], [TableNumber]);
GO

CREATE INDEX [idx_table_branchid] ON [Tables] ([BranchId]);
GO

CREATE INDEX [idx_table_status] ON [Tables] ([Status]);
GO

CREATE INDEX [idx_user_branchid] ON [Users] ([BranchId]);
GO

CREATE INDEX [idx_user_email] ON [Users] ([Email]);
GO

CREATE INDEX [idx_user_phone] ON [Users] ([Phone]);
GO

CREATE INDEX [idx_user_roleid] ON [Users] ([RoleId]);
GO

CREATE UNIQUE INDEX [idx_user_username] ON [Users] ([Username]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260418110007_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [MenuItemAddons] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Price] decimal(10,2) NOT NULL,
    [MenuItemId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [CreatedByName] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [ModifiedByName] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_MenuItemAddons] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuItemAddons_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MenuItemAddons_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE CASCADE
);
GO

UPDATE [Branches] SET [CreatedDate] = '2026-04-18T11:23:16.2449384Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [Roles] SET [CreatedDate] = '2026-04-18T11:23:16.2449726Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [Users] SET [CreatedDate] = '2026-04-18T11:23:16.2449790Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

CREATE INDEX [idx_addon_branchid] ON [MenuItemAddons] ([BranchId]);
GO

CREATE INDEX [idx_addon_menuitemid] ON [MenuItemAddons] ([MenuItemId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260418112317_AddMenuItemAddon', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [OrderItems] ADD [ModifiersJson] nvarchar(2000) NOT NULL DEFAULT N'';
GO

ALTER TABLE [OrderItems] ADD [Notes] nvarchar(500) NOT NULL DEFAULT N'';
GO

ALTER TABLE [OrderItems] ADD [VariantId] int NULL;
GO

ALTER TABLE [MenuItems] ADD [IsInventoryItem] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [MenuItems] ADD [IsPurchasable] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [MenuItems] ADD [IsRecipeItem] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [MenuItems] ADD [IsSaleable] bit NOT NULL DEFAULT CAST(1 AS bit);
GO

ALTER TABLE [MenuItems] ADD [ProductType] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [MenuCategories] ADD [CategoryType] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [InventoryItems] ADD [IsInventoryItem] bit NOT NULL DEFAULT CAST(1 AS bit);
GO

ALTER TABLE [InventoryItems] ADD [IsPurchasable] bit NOT NULL DEFAULT CAST(1 AS bit);
GO

ALTER TABLE [InventoryItems] ADD [ProductType] int NOT NULL DEFAULT 0;
GO

                UPDATE MenuItems
                SET ProductType = 1,
                    IsSaleable = 1,
                    IsInventoryItem = 0,
                    IsRecipeItem = 0,
                    IsPurchasable = 0
GO

                UPDATE MenuCategories
                SET CategoryType = 0
GO

                UPDATE InventoryItems
                SET ProductType = 0,
                    IsInventoryItem = 1,
                    IsPurchasable = 1
GO

UPDATE [Branches] SET [CreatedDate] = '2026-04-19T11:00:35.2929337Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [Roles] SET [CreatedDate] = '2026-04-19T11:00:35.2929498Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [Users] SET [CreatedDate] = '2026-04-19T11:00:35.2929533Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

CREATE INDEX [idx_menuitem_branch_saleable_type] ON [MenuItems] ([BranchId], [IsSaleable], [ProductType]);
GO

CREATE INDEX [idx_menucategory_branch_type] ON [MenuCategories] ([BranchId], [CategoryType]);
GO

CREATE INDEX [idx_inventoryitem_branch_inventory_type] ON [InventoryItems] ([BranchId], [IsInventoryItem], [ProductType]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260419110038_AddProductTypeAndCategoryWorkflow', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Roles] DROP CONSTRAINT [FK_Roles_Branches_BranchId];
GO

ALTER TABLE [Users] DROP CONSTRAINT [FK_Users_Branches_BranchId];
GO

DROP INDEX [idx_user_email] ON [Users];
GO

DROP INDEX [idx_role_branchid_name] ON [Roles];
GO

DROP INDEX [idx_customer_branch_phone] ON [Customers];
GO

DROP INDEX [idx_customer_email] ON [Customers];
GO

DROP INDEX [idx_customer_phone] ON [Customers];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'CreatedByName');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Users] DROP COLUMN [CreatedByName];
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'ModifiedByName');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Users] DROP COLUMN [ModifiedByName];
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tables]') AND [c].[name] = N'CreatedByName');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Tables] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Tables] DROP COLUMN [CreatedByName];
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tables]') AND [c].[name] = N'ModifiedByName');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Tables] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Tables] DROP COLUMN [ModifiedByName];
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockMovements]') AND [c].[name] = N'CreatedByName');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [StockMovements] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [StockMovements] DROP COLUMN [CreatedByName];
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockMovements]') AND [c].[name] = N'ModifiedByName');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [StockMovements] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [StockMovements] DROP COLUMN [ModifiedByName];
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'BranchId');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Roles] DROP COLUMN [BranchId];
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'CreatedById');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [Roles] DROP COLUMN [CreatedById];
GO

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'CreatedByName');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [Roles] DROP COLUMN [CreatedByName];
GO

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'ModifiedById');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [Roles] DROP COLUMN [ModifiedById];
GO

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'ModifiedByName');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [Roles] DROP COLUMN [ModifiedByName];
GO

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Recipes]') AND [c].[name] = N'CreatedByName');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Recipes] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [Recipes] DROP COLUMN [CreatedByName];
GO

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Recipes]') AND [c].[name] = N'ModifiedByName');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Recipes] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [Recipes] DROP COLUMN [ModifiedByName];
GO

DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'CreatedByName');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var13 + '];');
ALTER TABLE [Payments] DROP COLUMN [CreatedByName];
GO

DECLARE @var14 sysname;
SELECT @var14 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'ModifiedByName');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var14 + '];');
ALTER TABLE [Payments] DROP COLUMN [ModifiedByName];
GO

DECLARE @var15 sysname;
SELECT @var15 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'CreatedByName');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var15 + '];');
ALTER TABLE [Orders] DROP COLUMN [CreatedByName];
GO

DECLARE @var16 sysname;
SELECT @var16 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'ModifiedByName');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var16 + '];');
ALTER TABLE [Orders] DROP COLUMN [ModifiedByName];
GO

DECLARE @var17 sysname;
SELECT @var17 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderItems]') AND [c].[name] = N'CreatedByName');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [OrderItems] DROP CONSTRAINT [' + @var17 + '];');
ALTER TABLE [OrderItems] DROP COLUMN [CreatedByName];
GO

DECLARE @var18 sysname;
SELECT @var18 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderItems]') AND [c].[name] = N'ModifiedByName');
IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [OrderItems] DROP CONSTRAINT [' + @var18 + '];');
ALTER TABLE [OrderItems] DROP COLUMN [ModifiedByName];
GO

DECLARE @var19 sysname;
SELECT @var19 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuItemVariants]') AND [c].[name] = N'CreatedByName');
IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [MenuItemVariants] DROP CONSTRAINT [' + @var19 + '];');
ALTER TABLE [MenuItemVariants] DROP COLUMN [CreatedByName];
GO

DECLARE @var20 sysname;
SELECT @var20 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuItemVariants]') AND [c].[name] = N'ModifiedByName');
IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [MenuItemVariants] DROP CONSTRAINT [' + @var20 + '];');
ALTER TABLE [MenuItemVariants] DROP COLUMN [ModifiedByName];
GO

DECLARE @var21 sysname;
SELECT @var21 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuItems]') AND [c].[name] = N'CreatedByName');
IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [MenuItems] DROP CONSTRAINT [' + @var21 + '];');
ALTER TABLE [MenuItems] DROP COLUMN [CreatedByName];
GO

DECLARE @var22 sysname;
SELECT @var22 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuItems]') AND [c].[name] = N'ModifiedByName');
IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [MenuItems] DROP CONSTRAINT [' + @var22 + '];');
ALTER TABLE [MenuItems] DROP COLUMN [ModifiedByName];
GO

DECLARE @var23 sysname;
SELECT @var23 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuItemAddons]') AND [c].[name] = N'CreatedByName');
IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [MenuItemAddons] DROP CONSTRAINT [' + @var23 + '];');
ALTER TABLE [MenuItemAddons] DROP COLUMN [CreatedByName];
GO

DECLARE @var24 sysname;
SELECT @var24 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuItemAddons]') AND [c].[name] = N'ModifiedByName');
IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [MenuItemAddons] DROP CONSTRAINT [' + @var24 + '];');
ALTER TABLE [MenuItemAddons] DROP COLUMN [ModifiedByName];
GO

DECLARE @var25 sysname;
SELECT @var25 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuCategories]') AND [c].[name] = N'CreatedByName');
IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [MenuCategories] DROP CONSTRAINT [' + @var25 + '];');
ALTER TABLE [MenuCategories] DROP COLUMN [CreatedByName];
GO

DECLARE @var26 sysname;
SELECT @var26 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuCategories]') AND [c].[name] = N'ModifiedByName');
IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [MenuCategories] DROP CONSTRAINT [' + @var26 + '];');
ALTER TABLE [MenuCategories] DROP COLUMN [ModifiedByName];
GO

DECLARE @var27 sysname;
SELECT @var27 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventoryItems]') AND [c].[name] = N'CreatedByName');
IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [InventoryItems] DROP CONSTRAINT [' + @var27 + '];');
ALTER TABLE [InventoryItems] DROP COLUMN [CreatedByName];
GO

DECLARE @var28 sysname;
SELECT @var28 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventoryItems]') AND [c].[name] = N'ModifiedByName');
IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [InventoryItems] DROP CONSTRAINT [' + @var28 + '];');
ALTER TABLE [InventoryItems] DROP COLUMN [ModifiedByName];
GO

DECLARE @var29 sysname;
SELECT @var29 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Customers]') AND [c].[name] = N'CreatedByName');
IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [Customers] DROP CONSTRAINT [' + @var29 + '];');
ALTER TABLE [Customers] DROP COLUMN [CreatedByName];
GO

DECLARE @var30 sysname;
SELECT @var30 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Customers]') AND [c].[name] = N'ModifiedByName');
IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [Customers] DROP CONSTRAINT [' + @var30 + '];');
ALTER TABLE [Customers] DROP COLUMN [ModifiedByName];
GO

DECLARE @var31 sysname;
SELECT @var31 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Branches]') AND [c].[name] = N'City');
IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [Branches] DROP CONSTRAINT [' + @var31 + '];');
ALTER TABLE [Branches] DROP COLUMN [City];
GO

DECLARE @var32 sysname;
SELECT @var32 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Branches]') AND [c].[name] = N'CreatedByName');
IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [Branches] DROP CONSTRAINT [' + @var32 + '];');
ALTER TABLE [Branches] DROP COLUMN [CreatedByName];
GO

DECLARE @var33 sysname;
SELECT @var33 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Branches]') AND [c].[name] = N'Currency');
IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [Branches] DROP CONSTRAINT [' + @var33 + '];');
ALTER TABLE [Branches] DROP COLUMN [Currency];
GO

DECLARE @var34 sysname;
SELECT @var34 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Branches]') AND [c].[name] = N'ModifiedByName');
IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [Branches] DROP CONSTRAINT [' + @var34 + '];');
ALTER TABLE [Branches] DROP COLUMN [ModifiedByName];
GO

DECLARE @var35 sysname;
SELECT @var35 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Branches]') AND [c].[name] = N'TaxRate');
IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [Branches] DROP CONSTRAINT [' + @var35 + '];');
ALTER TABLE [Branches] DROP COLUMN [TaxRate];
GO

EXEC sp_rename N'[Tables].[idx_table_branchid]', N'idx_tables_branchid', N'INDEX';
GO

EXEC sp_rename N'[StockMovements].[idx_stockmovement_branchid]', N'idx_stockmovements_branchid', N'INDEX';
GO

EXEC sp_rename N'[Recipes].[idx_recipe_branchid]', N'idx_recipes_branchid', N'INDEX';
GO

EXEC sp_rename N'[Payments].[idx_payment_branchid]', N'idx_payments_branchid', N'INDEX';
GO

EXEC sp_rename N'[Orders].[idx_order_branchid]', N'idx_orders_branchid', N'INDEX';
GO

EXEC sp_rename N'[OrderItems].[idx_orderitem_branchid]', N'idx_orderitems_branchid', N'INDEX';
GO

EXEC sp_rename N'[MenuItemVariants].[idx_variant_branchid]', N'idx_menuitemvariants_branchid', N'INDEX';
GO

EXEC sp_rename N'[MenuItems].[idx_menuitem_branchid]', N'idx_menuitems_branchid', N'INDEX';
GO

EXEC sp_rename N'[MenuItemAddons].[idx_addon_branchid]', N'idx_menuitemaddons_branchid', N'INDEX';
GO

EXEC sp_rename N'[MenuCategories].[idx_menucategory_branchid]', N'idx_menucategories_branchid', N'INDEX';
GO

EXEC sp_rename N'[InventoryItems].[idx_inventoryitem_branchid]', N'idx_inventoryitems_branchid', N'INDEX';
GO

EXEC sp_rename N'[Customers].[idx_customer_branchid]', N'idx_customers_branchid', N'INDEX';
GO

EXEC sp_rename N'[Branches].[BranchId]', N'CountryId', N'COLUMN';
GO

DECLARE @var36 sysname;
SELECT @var36 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'CreatedDate');
IF @var36 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var36 + '];');
GO

ALTER TABLE [Users] ADD [BusinessId] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Users] ADD [DeletedAt] datetime2 NULL;
GO

ALTER TABLE [Users] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
GO

ALTER TABLE [Tables] ADD [BusinessId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [StockMovements] ADD [BusinessId] int NOT NULL DEFAULT 1;
GO

DECLARE @var37 sysname;
SELECT @var37 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'Permissions');
IF @var37 IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT [' + @var37 + '];');
ALTER TABLE [Roles] ADD DEFAULT N'' FOR [Permissions];
GO

ALTER TABLE [Roles] ADD [Description] nvarchar(500) NOT NULL DEFAULT N'';
GO

ALTER TABLE [Roles] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
GO

ALTER TABLE [Recipes] ADD [BusinessId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [Payments] ADD [BusinessId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [Orders] ADD [BusinessId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [OrderItems] ADD [BusinessId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [MenuItemVariants] ADD [BusinessId] int NOT NULL DEFAULT 1;
GO

DECLARE @var38 sysname;
SELECT @var38 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuItems]') AND [c].[name] = N'ProductType');
IF @var38 IS NOT NULL EXEC(N'ALTER TABLE [MenuItems] DROP CONSTRAINT [' + @var38 + '];');
ALTER TABLE [MenuItems] ADD DEFAULT 1 FOR [ProductType];
GO

ALTER TABLE [MenuItems] ADD [BusinessId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [MenuItems] ADD [SubCategoryId] int NULL;
GO

ALTER TABLE [MenuItemAddons] ADD [BusinessId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [MenuCategories] ADD [BusinessId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [MenuCategories] ADD [Code] nvarchar(50) NOT NULL DEFAULT N'';
GO

ALTER TABLE [MenuCategories] ADD [Color] nvarchar(50) NOT NULL DEFAULT N'';
GO

ALTER TABLE [MenuCategories] ADD [DisplayOrder] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [MenuCategories] ADD [Icon] nvarchar(150) NOT NULL DEFAULT N'';
GO

ALTER TABLE [MenuCategories] ADD [Image] varbinary(max) NULL;
GO

ALTER TABLE [MenuCategories] ADD [ImageContentType] nvarchar(100) NULL;
GO

ALTER TABLE [MenuCategories] ADD [ImageFileName] nvarchar(255) NULL;
GO

ALTER TABLE [MenuCategories] ADD [ImageUrl] nvarchar(500) NOT NULL DEFAULT N'';
GO

ALTER TABLE [MenuCategories] ADD [Status] bit NOT NULL DEFAULT CAST(1 AS bit);
GO

ALTER TABLE [InventoryItems] ADD [BusinessId] int NOT NULL DEFAULT 1;
GO

DECLARE @var39 sysname;
SELECT @var39 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Customers]') AND [c].[name] = N'Phone');
IF @var39 IS NOT NULL EXEC(N'ALTER TABLE [Customers] DROP CONSTRAINT [' + @var39 + '];');
ALTER TABLE [Customers] ALTER COLUMN [Phone] nvarchar(20) NULL;
GO

DECLARE @var40 sysname;
SELECT @var40 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Customers]') AND [c].[name] = N'Email');
IF @var40 IS NOT NULL EXEC(N'ALTER TABLE [Customers] DROP CONSTRAINT [' + @var40 + '];');
ALTER TABLE [Customers] ALTER COLUMN [Email] nvarchar(150) NULL;
GO

DECLARE @var41 sysname;
SELECT @var41 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Customers]') AND [c].[name] = N'Address');
IF @var41 IS NOT NULL EXEC(N'ALTER TABLE [Customers] DROP CONSTRAINT [' + @var41 + '];');
ALTER TABLE [Customers] ALTER COLUMN [Address] nvarchar(500) NULL;
GO

ALTER TABLE [Customers] ADD [BusinessId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [Customers] ADD [CNIC] nvarchar(20) NULL;
GO

ALTER TABLE [Customers] ADD [CityId] int NULL;
GO

ALTER TABLE [Customers] ADD [CountryId] int NULL;
GO

ALTER TABLE [Customers] ADD [CreditLimit] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [Customers] ADD [CustomerCode] nvarchar(50) NOT NULL DEFAULT N'';
GO

ALTER TABLE [Customers] ADD [CustomerType] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Customers] ADD [IsWalkIn] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Customers] ADD [OpeningBalance] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [Customers] ADD [Status] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Branches] ADD [BusinessId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [Branches] ADD [CityId] int NOT NULL DEFAULT 0;
GO

CREATE TABLE [Brands] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [ImageData] varbinary(max) NULL,
    [ImageContentType] nvarchar(100) NULL,
    [ImageFileName] nvarchar(255) NULL,
    [Status] bit NOT NULL DEFAULT CAST(1 AS bit),
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Brands] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Brands_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Businesses] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [LegalName] nvarchar(250) NOT NULL,
    [Logo] varbinary(max) NULL,
    [LogoFileName] nvarchar(255) NULL,
    [LogoContentType] nvarchar(100) NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [TaxNumber] nvarchar(50) NOT NULL,
    [Currency] nvarchar(10) NOT NULL,
    [TimeZone] nvarchar(100) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Businesses] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [CashFlowTransactions] (
    [Id] int NOT NULL IDENTITY,
    [TransactionType] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentMethod] int NOT NULL,
    [ReferenceId] int NULL,
    [ReferenceNo] nvarchar(100) NULL,
    [Description] nvarchar(500) NULL,
    [TransactionDate] datetime2 NOT NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_CashFlowTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CashFlowTransactions_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [CashRegisters] (
    [Id] int NOT NULL IDENTITY,
    [RegisterDate] datetime2 NOT NULL,
    [OpeningCash] decimal(18,2) NOT NULL,
    [ClosingCash] decimal(18,2) NULL,
    [ExpectedCash] decimal(18,2) NULL,
    [ActualCash] decimal(18,2) NULL,
    [Difference] decimal(18,2) NULL,
    [IsClosed] bit NOT NULL,
    [Notes] nvarchar(500) NULL,
    [ClosedBy] int NULL,
    [ClosedAt] datetime2 NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_CashRegisters] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CashRegisters_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Cities] (
    [Id] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [CountryId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Cities] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [CodeSequences] (
    [Id] int NOT NULL IDENTITY,
    [ModuleName] nvarchar(50) NOT NULL,
    [BranchId] int NULL,
    [Prefix] nvarchar(20) NOT NULL,
    [LastNumber] bigint NOT NULL,
    [ResetType] int NOT NULL,
    [LastResetDate] datetime2 NULL,
    CONSTRAINT [PK_CodeSequences] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Countries] (
    [Id] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(10) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Countries] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Currencies] (
    [Id] int NOT NULL,
    [Code] nvarchar(10) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Symbol] nvarchar(10) NOT NULL,
    [ExchangeRateToPKR] decimal(18,6) NOT NULL,
    [IsBase] bit NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Currencies] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ExpenseCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Status] bit NOT NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_ExpenseCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExpenseCategories_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Menus] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Route] nvarchar(200) NULL,
    [Icon] nvarchar(50) NULL,
    [ModuleName] nvarchar(100) NULL,
    [ParentId] int NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Menus] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Menus_Menus_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Menus] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Modules] (
    [Id] int NOT NULL IDENTITY,
    [ModuleName] nvarchar(100) NOT NULL,
    [ModuleKey] nvarchar(100) NOT NULL,
    [ParentModuleId] int NULL,
    [Route] nvarchar(200) NULL,
    [Icon] nvarchar(50) NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_Modules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Modules_Modules_ParentModuleId] FOREIGN KEY ([ParentModuleId]) REFERENCES [Modules] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [SubCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [Status] bit NOT NULL DEFAULT CAST(1 AS bit),
    [Icon] nvarchar(150) NOT NULL,
    [ImageData] varbinary(max) NULL,
    [ImageContentType] nvarchar(100) NULL,
    [CategoryId] int NOT NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_SubCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SubCategories_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SubCategories_MenuCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [MenuCategories] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Suppliers] (
    [Id] int NOT NULL IDENTITY,
    [SupplierCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [ContactPerson] nvarchar(150) NOT NULL,
    [Phone] nvarchar(30) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [TaxNumber] nvarchar(50) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Suppliers_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Units] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [ConversionFactor] decimal(18,4) NOT NULL DEFAULT 1.0,
    [Status] bit NOT NULL DEFAULT CAST(1 AS bit),
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Units] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Units_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [UserBranches] (
    [UserId] int NOT NULL,
    [BranchId] int NOT NULL,
    CONSTRAINT [PK_UserBranches] PRIMARY KEY ([UserId], [BranchId]),
    CONSTRAINT [FK_UserBranches_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserBranches_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Warehouses] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Warehouses_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Expenses] (
    [Id] int NOT NULL IDENTITY,
    [ExpenseCategoryId] int NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentMethod] int NOT NULL,
    [ExpenseDate] datetime2 NOT NULL,
    [ReferenceNo] nvarchar(100) NULL,
    [Notes] nvarchar(500) NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Expenses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Expenses_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Expenses_ExpenseCategories_ExpenseCategoryId] FOREIGN KEY ([ExpenseCategoryId]) REFERENCES [ExpenseCategories] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ModuleForms] (
    [Id] int NOT NULL IDENTITY,
    [ModuleId] int NOT NULL,
    [FormName] nvarchar(100) NOT NULL,
    [FormCode] nvarchar(100) NOT NULL,
    [Route] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_ModuleForms] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ModuleForms_Modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RolePermissions] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [ModuleId] int NULL,
    [ModuleName] nvarchar(100) NOT NULL,
    [CanView] bit NOT NULL,
    [CanCreate] bit NOT NULL,
    [CanEdit] bit NOT NULL,
    [CanDelete] bit NOT NULL,
    [CanExport] bit NOT NULL,
    [CanUpload] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RolePermissions_Modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [ProductName] nvarchar(200) NOT NULL,
    [ProductCode] nvarchar(50) NOT NULL,
    [SKU] nvarchar(100) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [Status] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CategoryId] int NOT NULL,
    [SubCategoryId] int NULL,
    [BrandId] int NULL,
    [CostPrice] decimal(18,2) NOT NULL,
    [SellingPrice] decimal(18,2) NOT NULL,
    [WholesalePrice] decimal(18,2) NOT NULL,
    [IsVariantEnabled] bit NOT NULL,
    [IsDiscountAllowed] bit NOT NULL,
    [DiscountType] int NULL,
    [DiscountValue] decimal(18,2) NOT NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Products_Brands_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brands] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Products_MenuCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [MenuCategories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Products_SubCategories_SubCategoryId] FOREIGN KEY ([SubCategoryId]) REFERENCES [SubCategories] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Purchases] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceNo] nvarchar(100) NOT NULL,
    [SupplierId] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [PurchaseDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [TotalAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [Status] int NOT NULL DEFAULT 0,
    [Notes] nvarchar(1000) NOT NULL,
    [VoidedAt] datetime2 NULL,
    [VoidedByName] nvarchar(max) NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Purchases] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Purchases_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Purchases_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Purchases_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [SaleInvoices] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceNo] nvarchar(100) NOT NULL,
    [CustomerId] int NULL,
    [WarehouseId] int NOT NULL,
    [SaleDate] datetime2 NOT NULL,
    [SubTotal] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [GrandTotal] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [ReturnAmount] decimal(18,2) NOT NULL,
    [PaymentMethod] int NOT NULL,
    [CardAmount] decimal(18,2) NOT NULL,
    [CashAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [PricingType] int NOT NULL,
    [Notes] nvarchar(1000) NULL,
    [HeldNote] nvarchar(500) NULL,
    [CashierName] nvarchar(200) NULL,
    [VoidedAt] datetime2 NULL,
    [VoidedByName] nvarchar(max) NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_SaleInvoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SaleInvoices_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SaleInvoices_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SaleInvoices_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [RoleFormPermissions] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [FormId] int NOT NULL,
    [CanView] bit NOT NULL,
    [CanCreate] bit NOT NULL,
    [CanEdit] bit NOT NULL,
    [CanDelete] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_RoleFormPermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoleFormPermissions_ModuleForms_FormId] FOREIGN KEY ([FormId]) REFERENCES [ModuleForms] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RoleFormPermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ProductImages] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [FileName] nvarchar(255) NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [ImageData] varbinary(max) NOT NULL,
    [IsPrimary] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ProductUnits] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [UnitName] nvarchar(100) NOT NULL,
    [ConversionFactor] decimal(18,4) NOT NULL,
    [IsBaseUnit] bit NOT NULL,
    [CostPrice] decimal(18,2) NULL,
    [SellingPrice] decimal(18,2) NULL,
    [WholesalePrice] decimal(18,2) NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_ProductUnits] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductUnits_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ProductVariants] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [VariantName] nvarchar(150) NOT NULL,
    [Size] nvarchar(50) NOT NULL,
    [Color] nvarchar(50) NOT NULL,
    [SKU] nvarchar(100) NOT NULL,
    [AdditionalPrice] decimal(18,2) NOT NULL,
    [CostPriceOverride] decimal(18,2) NULL,
    [SellingPriceOverride] decimal(18,2) NULL,
    [Status] bit NOT NULL DEFAULT CAST(1 AS bit),
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_ProductVariants] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductVariants_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ProductBarcodes] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [ProductUnitId] int NULL,
    [ProductVariantId] int NULL,
    [BarcodeValue] nvarchar(100) NOT NULL,
    [IsPrimary] bit NOT NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_ProductBarcodes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductBarcodes_ProductUnits_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnits] ([Id]),
    CONSTRAINT [FK_ProductBarcodes_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [ProductVariants] ([Id]),
    CONSTRAINT [FK_ProductBarcodes_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [PurchaseItems] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseId] int NOT NULL,
    [ProductId] int NOT NULL,
    [VariantId] int NULL,
    [UnitId] int NOT NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [ConversionFactor] decimal(18,4) NOT NULL DEFAULT 1.0,
    [BaseQuantity] decimal(18,4) NOT NULL,
    [CostPrice] decimal(18,2) NOT NULL,
    [TotalCost] decimal(18,2) NOT NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_PurchaseItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseItems_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
    CONSTRAINT [FK_PurchaseItems_ProductUnits_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [ProductUnits] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseItems_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseItems_Purchases_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [Purchases] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [SaleInvoiceItems] (
    [Id] int NOT NULL IDENTITY,
    [SaleInvoiceId] int NOT NULL,
    [ProductId] int NOT NULL,
    [VariantId] int NULL,
    [UnitId] int NOT NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [ConversionFactor] decimal(18,6) NOT NULL,
    [BaseQuantity] decimal(18,4) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [DiscountPercent] decimal(8,4) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [TaxPercent] decimal(8,4) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [LineTotal] decimal(18,2) NOT NULL,
    [ItemNote] nvarchar(500) NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_SaleInvoiceItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SaleInvoiceItems_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SaleInvoiceItems_ProductUnits_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [ProductUnits] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SaleInvoiceItems_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SaleInvoiceItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SaleInvoiceItems_SaleInvoices_SaleInvoiceId] FOREIGN KEY ([SaleInvoiceId]) REFERENCES [SaleInvoices] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [StockLedger] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [VariantId] int NULL,
    [WarehouseId] int NOT NULL,
    [Type] int NOT NULL,
    [ReferenceId] int NULL,
    [QuantityInBaseUnit] decimal(18,4) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL DEFAULT 0.0,
    [TotalAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [Date] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [Remarks] nvarchar(500) NOT NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_StockLedger] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockLedger_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
    CONSTRAINT [FK_StockLedger_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockLedger_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockLedger_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO

UPDATE [Branches] SET [BusinessId] = 1, [CityId] = 5, [CountryId] = 3, [CreatedDate] = '2026-06-16T15:31:34.4871018Z', [CreatedById] = 1, [Email] = N'info@akhsoft.com', [Phone] = N'+923432998052'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'CreatedDate', N'CreatedById', N'Currency', N'Email', N'IsActive', N'IsDeleted', N'LegalName', N'Logo', N'LogoContentType', N'LogoFileName', N'UpdatedDate', N'ModifiedById', N'Name', N'Phone', N'TaxNumber', N'TimeZone') AND [object_id] = OBJECT_ID(N'[Businesses]'))
    SET IDENTITY_INSERT [Businesses] ON;
INSERT INTO [Businesses] ([Id], [Address], [CreatedDate], [CreatedById], [Currency], [Email], [IsActive], [IsDeleted], [LegalName], [Logo], [LogoContentType], [LogoFileName], [UpdatedDate], [ModifiedById], [Name], [Phone], [TaxNumber], [TimeZone])
VALUES (1, N'123 Main Street', '2026-06-16T15:31:34.4870264Z', 1, N'PKR', N'info@akhsoft.com', CAST(1 AS bit), CAST(0 AS bit), N'AKHSOFT', NULL, NULL, NULL, NULL, NULL, N'AKHSOFT', N'+923432998052', N'NTN-0001', N'Asia/Karachi');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'CreatedDate', N'CreatedById', N'Currency', N'Email', N'IsActive', N'IsDeleted', N'LegalName', N'Logo', N'LogoContentType', N'LogoFileName', N'UpdatedDate', N'ModifiedById', N'Name', N'Phone', N'TaxNumber', N'TimeZone') AND [object_id] = OBJECT_ID(N'[Businesses]'))
    SET IDENTITY_INSERT [Businesses] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CountryId', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Cities]'))
    SET IDENTITY_INSERT [Cities] ON;
INSERT INTO [Cities] ([Id], [CountryId], [IsActive], [Name])
VALUES (1, 1, CAST(1 AS bit), N'New York'),
(2, 1, CAST(1 AS bit), N'Los Angeles'),
(3, 2, CAST(1 AS bit), N'London'),
(4, 2, CAST(1 AS bit), N'Manchester'),
(5, 3, CAST(1 AS bit), N'Karachi'),
(6, 3, CAST(1 AS bit), N'Lahore'),
(7, 3, CAST(1 AS bit), N'Islamabad'),
(8, 4, CAST(1 AS bit), N'Dubai'),
(9, 4, CAST(1 AS bit), N'Abu Dhabi');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CountryId', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Cities]'))
    SET IDENTITY_INSERT [Cities] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Countries]'))
    SET IDENTITY_INSERT [Countries] ON;
INSERT INTO [Countries] ([Id], [Code], [IsActive], [Name])
VALUES (1, N'US', CAST(1 AS bit), N'United States'),
(2, N'GB', CAST(1 AS bit), N'United Kingdom'),
(3, N'PK', CAST(1 AS bit), N'Pakistan'),
(4, N'AE', CAST(1 AS bit), N'United Arab Emirates');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Countries]'))
    SET IDENTITY_INSERT [Countries] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'ExchangeRateToPKR', N'IsActive', N'IsBase', N'Name', N'Symbol') AND [object_id] = OBJECT_ID(N'[Currencies]'))
    SET IDENTITY_INSERT [Currencies] ON;
INSERT INTO [Currencies] ([Id], [Code], [ExchangeRateToPKR], [IsActive], [IsBase], [Name], [Symbol])
VALUES (1, N'PKR', 1.0, CAST(1 AS bit), CAST(1 AS bit), N'Pakistani Rupee', N'₨'),
(2, N'USD', 278.0, CAST(1 AS bit), CAST(0 AS bit), N'US Dollar', N'$'),
(3, N'GBP', 350.0, CAST(1 AS bit), CAST(0 AS bit), N'British Pound', N'£'),
(4, N'AED', 75.7, CAST(1 AS bit), CAST(0 AS bit), N'UAE Dirham', N'د.إ'),
(5, N'EUR', 300.0, CAST(1 AS bit), CAST(0 AS bit), N'Euro', N'€');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'ExchangeRateToPKR', N'IsActive', N'IsBase', N'Name', N'Symbol') AND [object_id] = OBJECT_ID(N'[Currencies]'))
    SET IDENTITY_INSERT [Currencies] OFF;
GO

UPDATE [Roles] SET [CreatedDate] = '2026-01-01T00:00:00.0000000Z', [Description] = N'Full system access', [IsActive] = CAST(1 AS bit), [Name] = N'System Admin', [Permissions] = N'all'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedDate', N'Description', N'IsActive', N'IsDeleted', N'Name', N'Permissions', N'UpdatedDate') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [CreatedDate], [Description], [IsActive], [IsDeleted], [Name], [Permissions], [UpdatedDate])
VALUES (2, '2026-01-01T00:00:00.0000000Z', N'All branches access', CAST(1 AS bit), CAST(0 AS bit), N'Super Admin', N'all', NULL),
(3, '2026-01-01T00:00:00.0000000Z', N'Branch-level management', CAST(1 AS bit), CAST(0 AS bit), N'Admin', N'branch_admin', NULL),
(4, '2026-01-01T00:00:00.0000000Z', N'Operations control', CAST(1 AS bit), CAST(0 AS bit), N'Manager', N'operations', NULL),
(5, '2026-01-01T00:00:00.0000000Z', N'POS billing access', CAST(1 AS bit), CAST(0 AS bit), N'Cashier', N'pos', NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedDate', N'Description', N'IsActive', N'IsDeleted', N'Name', N'Permissions', N'UpdatedDate') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'BranchId', N'UserId') AND [object_id] = OBJECT_ID(N'[UserBranches]'))
    SET IDENTITY_INSERT [UserBranches] ON;
INSERT INTO [UserBranches] ([BranchId], [UserId])
VALUES (1, 1);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'BranchId', N'UserId') AND [object_id] = OBJECT_ID(N'[UserBranches]'))
    SET IDENTITY_INSERT [UserBranches] OFF;
GO

UPDATE [Users] SET [BusinessId] = 1, [CreatedDate] = '2026-01-01T00:00:00.0000000Z', [CreatedById] = 1, [DeletedAt] = NULL, [Email] = N'info@infoakhsoft.com', [FullName] = N'Muhammad Akmal', [IsActive] = CAST(1 AS bit), [PasswordHash] = N'$2a$11$W7Mi6nl3DiHePG3yDxDRv.VuEY5uE2Jfa2VizDS.h78g1bjFEYuuu', [Phone] = N'+923432998052', [Username] = N'makmal'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

CREATE INDEX [idx_user_businessid] ON [Users] ([BusinessId]);
GO

CREATE UNIQUE INDEX [idx_user_email] ON [Users] ([Email]);
GO

CREATE INDEX [idx_tables_business_branch] ON [Tables] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_tables_businessid] ON [Tables] ([BusinessId]);
GO

CREATE INDEX [idx_stockmovements_business_branch] ON [StockMovements] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_stockmovements_businessid] ON [StockMovements] ([BusinessId]);
GO

CREATE UNIQUE INDEX [idx_role_name] ON [Roles] ([Name]);
GO

CREATE INDEX [idx_recipes_business_branch] ON [Recipes] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_recipes_businessid] ON [Recipes] ([BusinessId]);
GO

CREATE INDEX [idx_payments_business_branch] ON [Payments] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_payments_businessid] ON [Payments] ([BusinessId]);
GO

CREATE INDEX [idx_orders_business_branch] ON [Orders] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_orders_businessid] ON [Orders] ([BusinessId]);
GO

CREATE INDEX [idx_orderitems_business_branch] ON [OrderItems] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_orderitems_businessid] ON [OrderItems] ([BusinessId]);
GO

CREATE INDEX [idx_menuitemvariants_business_branch] ON [MenuItemVariants] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_menuitemvariants_businessid] ON [MenuItemVariants] ([BusinessId]);
GO

CREATE INDEX [idx_menuitem_subcategoryid] ON [MenuItems] ([SubCategoryId]);
GO

CREATE INDEX [idx_menuitems_business_branch] ON [MenuItems] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_menuitems_businessid] ON [MenuItems] ([BusinessId]);
GO

CREATE INDEX [idx_menuitemaddons_business_branch] ON [MenuItemAddons] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_menuitemaddons_businessid] ON [MenuItemAddons] ([BusinessId]);
GO

CREATE INDEX [idx_menucategories_business_branch] ON [MenuCategories] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_menucategories_businessid] ON [MenuCategories] ([BusinessId]);
GO

CREATE UNIQUE INDEX [idx_menucategory_branch_code] ON [MenuCategories] ([BranchId], [Code]) WHERE [Code] IS NOT NULL AND [Code] <> '';
GO

CREATE INDEX [idx_inventoryitems_business_branch] ON [InventoryItems] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_inventoryitems_businessid] ON [InventoryItems] ([BusinessId]);
GO

CREATE UNIQUE INDEX [idx_customer_branch_code] ON [Customers] ([BusinessId], [BranchId], [CustomerCode]) WHERE [CustomerCode] <> '' AND [IsDeleted] = 0;
GO

CREATE UNIQUE INDEX [idx_customer_branch_phone_unique] ON [Customers] ([BusinessId], [BranchId], [Phone]) WHERE [Phone] IS NOT NULL AND [IsDeleted] = 0;
GO

CREATE INDEX [idx_customer_cityid] ON [Customers] ([CityId]);
GO

CREATE INDEX [idx_customer_countryid] ON [Customers] ([CountryId]);
GO

CREATE INDEX [idx_customer_walkin] ON [Customers] ([BusinessId], [BranchId], [IsWalkIn]);
GO

CREATE INDEX [idx_customers_business_branch] ON [Customers] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_customers_businessid] ON [Customers] ([BusinessId]);
GO

CREATE INDEX [idx_branch_cityid] ON [Branches] ([CityId]);
GO

CREATE INDEX [idx_branch_countryid] ON [Branches] ([CountryId]);
GO

CREATE INDEX [idx_branches_businessid] ON [Branches] ([BusinessId]);
GO

CREATE UNIQUE INDEX [idx_brand_branch_name] ON [Brands] ([BranchId], [Name]);
GO

CREATE INDEX [idx_brands_branchid] ON [Brands] ([BranchId]);
GO

CREATE INDEX [idx_brands_business_branch] ON [Brands] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_brands_businessid] ON [Brands] ([BusinessId]);
GO

CREATE INDEX [idx_business_email] ON [Businesses] ([Email]);
GO

CREATE INDEX [idx_business_name] ON [Businesses] ([Name]);
GO

CREATE INDEX [idx_cashflowtransactions_branchid] ON [CashFlowTransactions] ([BranchId]);
GO

CREATE INDEX [idx_cashflowtransactions_business_branch] ON [CashFlowTransactions] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_cashflowtransactions_businessid] ON [CashFlowTransactions] ([BusinessId]);
GO

CREATE INDEX [IX_CashFlowTransactions_BusinessId_BranchId_TransactionDate] ON [CashFlowTransactions] ([BusinessId], [BranchId], [TransactionDate]);
GO

CREATE INDEX [IX_CashFlowTransactions_BusinessId_BranchId_TransactionType] ON [CashFlowTransactions] ([BusinessId], [BranchId], [TransactionType]);
GO

CREATE INDEX [idx_cashregisters_branchid] ON [CashRegisters] ([BranchId]);
GO

CREATE INDEX [idx_cashregisters_business_branch] ON [CashRegisters] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_cashregisters_businessid] ON [CashRegisters] ([BusinessId]);
GO

CREATE UNIQUE INDEX [IX_CashRegisters_BusinessId_BranchId_RegisterDate] ON [CashRegisters] ([BusinessId], [BranchId], [RegisterDate]) WHERE [IsDeleted] = 0;
GO

CREATE UNIQUE INDEX [idx_city_country_name] ON [Cities] ([CountryId], [Name]);
GO

CREATE INDEX [idx_city_countryid] ON [Cities] ([CountryId]);
GO

CREATE UNIQUE INDEX [idx_codesequence_module_branch] ON [CodeSequences] ([ModuleName], [BranchId]) WHERE [BranchId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [idx_country_code] ON [Countries] ([Code]);
GO

CREATE UNIQUE INDEX [idx_currency_code] ON [Currencies] ([Code]);
GO

CREATE INDEX [idx_expensecategories_branchid] ON [ExpenseCategories] ([BranchId]);
GO

CREATE INDEX [idx_expensecategories_business_branch] ON [ExpenseCategories] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_expensecategories_businessid] ON [ExpenseCategories] ([BusinessId]);
GO

CREATE UNIQUE INDEX [idx_expensecategory_branch_name] ON [ExpenseCategories] ([BusinessId], [BranchId], [Name]) WHERE [IsDeleted] = 0;
GO

CREATE INDEX [idx_expenses_branchid] ON [Expenses] ([BranchId]);
GO

CREATE INDEX [idx_expenses_business_branch] ON [Expenses] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_expenses_businessid] ON [Expenses] ([BusinessId]);
GO

CREATE INDEX [IX_Expenses_BusinessId_BranchId_ExpenseCategoryId] ON [Expenses] ([BusinessId], [BranchId], [ExpenseCategoryId]);
GO

CREATE INDEX [IX_Expenses_BusinessId_BranchId_ExpenseDate] ON [Expenses] ([BusinessId], [BranchId], [ExpenseDate]);
GO

CREATE INDEX [IX_Expenses_ExpenseCategoryId] ON [Expenses] ([ExpenseCategoryId]);
GO

CREATE INDEX [idx_menus_displayorder] ON [Menus] ([DisplayOrder]);
GO

CREATE INDEX [idx_menus_parentid] ON [Menus] ([ParentId]);
GO

CREATE UNIQUE INDEX [idx_module_form_code] ON [ModuleForms] ([FormCode]);
GO

CREATE INDEX [IX_ModuleForms_ModuleId] ON [ModuleForms] ([ModuleId]);
GO

CREATE UNIQUE INDEX [idx_module_key] ON [Modules] ([ModuleKey]) WHERE [ModuleKey] <> '' AND [IsDeleted] = 0;
GO

CREATE INDEX [IX_Modules_ParentModuleId] ON [Modules] ([ParentModuleId]);
GO

CREATE UNIQUE INDEX [idx_productbarcode_value] ON [ProductBarcodes] ([BarcodeValue]) WHERE [IsDeleted] = 0;
GO

CREATE INDEX [idx_productbarcodes_branchid] ON [ProductBarcodes] ([BranchId]);
GO

CREATE INDEX [idx_productbarcodes_business_branch] ON [ProductBarcodes] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_productbarcodes_businessid] ON [ProductBarcodes] ([BusinessId]);
GO

CREATE INDEX [IX_ProductBarcodes_ProductId] ON [ProductBarcodes] ([ProductId]);
GO

CREATE INDEX [IX_ProductBarcodes_ProductUnitId] ON [ProductBarcodes] ([ProductUnitId]);
GO

CREATE INDEX [IX_ProductBarcodes_ProductVariantId] ON [ProductBarcodes] ([ProductVariantId]);
GO

CREATE INDEX [idx_productimage_product_primary] ON [ProductImages] ([ProductId], [IsPrimary]);
GO

CREATE INDEX [idx_productimages_branchid] ON [ProductImages] ([BranchId]);
GO

CREATE INDEX [idx_productimages_business_branch] ON [ProductImages] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_productimages_businessid] ON [ProductImages] ([BusinessId]);
GO

CREATE INDEX [idx_product_business_branch_category] ON [Products] ([BusinessId], [BranchId], [CategoryId]);
GO

CREATE UNIQUE INDEX [idx_product_business_branch_code] ON [Products] ([BusinessId], [BranchId], [ProductCode]);
GO

CREATE INDEX [idx_product_business_branch_sku] ON [Products] ([BusinessId], [BranchId], [SKU]);
GO

CREATE INDEX [idx_products_branchid] ON [Products] ([BranchId]);
GO

CREATE INDEX [idx_products_business_branch] ON [Products] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_products_businessid] ON [Products] ([BusinessId]);
GO

CREATE INDEX [IX_Products_BrandId] ON [Products] ([BrandId]);
GO

CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
GO

CREATE INDEX [IX_Products_SubCategoryId] ON [Products] ([SubCategoryId]);
GO

CREATE INDEX [idx_productunit_product_name] ON [ProductUnits] ([ProductId], [UnitName]);
GO

CREATE INDEX [idx_productunits_branchid] ON [ProductUnits] ([BranchId]);
GO

CREATE INDEX [idx_productunits_business_branch] ON [ProductUnits] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_productunits_businessid] ON [ProductUnits] ([BusinessId]);
GO

CREATE INDEX [idx_productvariant_product_sku] ON [ProductVariants] ([ProductId], [SKU]);
GO

CREATE INDEX [idx_productvariants_branchid] ON [ProductVariants] ([BranchId]);
GO

CREATE INDEX [idx_productvariants_business_branch] ON [ProductVariants] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_productvariants_businessid] ON [ProductVariants] ([BusinessId]);
GO

CREATE INDEX [idx_purchaseitems_branchid] ON [PurchaseItems] ([BranchId]);
GO

CREATE INDEX [idx_purchaseitems_business_branch] ON [PurchaseItems] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_purchaseitems_businessid] ON [PurchaseItems] ([BusinessId]);
GO

CREATE INDEX [IX_PurchaseItems_ProductId] ON [PurchaseItems] ([ProductId]);
GO

CREATE INDEX [IX_PurchaseItems_PurchaseId] ON [PurchaseItems] ([PurchaseId]);
GO

CREATE INDEX [IX_PurchaseItems_UnitId] ON [PurchaseItems] ([UnitId]);
GO

CREATE INDEX [IX_PurchaseItems_VariantId] ON [PurchaseItems] ([VariantId]);
GO

CREATE UNIQUE INDEX [idx_purchase_business_branch_invoice] ON [Purchases] ([BusinessId], [BranchId], [InvoiceNo]);
GO

CREATE INDEX [idx_purchases_branchid] ON [Purchases] ([BranchId]);
GO

CREATE INDEX [idx_purchases_business_branch] ON [Purchases] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_purchases_businessid] ON [Purchases] ([BusinessId]);
GO

CREATE INDEX [IX_Purchases_SupplierId] ON [Purchases] ([SupplierId]);
GO

CREATE INDEX [IX_Purchases_WarehouseId] ON [Purchases] ([WarehouseId]);
GO

CREATE UNIQUE INDEX [idx_role_form_permission] ON [RoleFormPermissions] ([RoleId], [FormId]);
GO

CREATE INDEX [IX_RoleFormPermissions_FormId] ON [RoleFormPermissions] ([FormId]);
GO

CREATE UNIQUE INDEX [idx_rolepermission_role_module] ON [RolePermissions] ([RoleId], [ModuleName]) WHERE [IsDeleted] = 0;
GO

CREATE INDEX [IX_RolePermissions_ModuleId] ON [RolePermissions] ([ModuleId]);
GO

CREATE INDEX [idx_saleinvoiceitems_branchid] ON [SaleInvoiceItems] ([BranchId]);
GO

CREATE INDEX [idx_saleinvoiceitems_business_branch] ON [SaleInvoiceItems] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_saleinvoiceitems_businessid] ON [SaleInvoiceItems] ([BusinessId]);
GO

CREATE INDEX [IX_SaleInvoiceItems_ProductId] ON [SaleInvoiceItems] ([ProductId]);
GO

CREATE INDEX [IX_SaleInvoiceItems_SaleInvoiceId] ON [SaleInvoiceItems] ([SaleInvoiceId]);
GO

CREATE INDEX [IX_SaleInvoiceItems_UnitId] ON [SaleInvoiceItems] ([UnitId]);
GO

CREATE INDEX [IX_SaleInvoiceItems_VariantId] ON [SaleInvoiceItems] ([VariantId]);
GO

CREATE INDEX [idx_saleinvoices_branchid] ON [SaleInvoices] ([BranchId]);
GO

CREATE INDEX [idx_saleinvoices_business_branch] ON [SaleInvoices] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_saleinvoices_businessid] ON [SaleInvoices] ([BusinessId]);
GO

CREATE UNIQUE INDEX [IX_SaleInvoices_BusinessId_BranchId_InvoiceNo] ON [SaleInvoices] ([BusinessId], [BranchId], [InvoiceNo]) WHERE [IsDeleted] = 0;
GO

CREATE INDEX [IX_SaleInvoices_BusinessId_BranchId_SaleDate] ON [SaleInvoices] ([BusinessId], [BranchId], [SaleDate]);
GO

CREATE INDEX [IX_SaleInvoices_BusinessId_BranchId_Status] ON [SaleInvoices] ([BusinessId], [BranchId], [Status]);
GO

CREATE INDEX [IX_SaleInvoices_CustomerId] ON [SaleInvoices] ([CustomerId]);
GO

CREATE INDEX [IX_SaleInvoices_WarehouseId] ON [SaleInvoices] ([WarehouseId]);
GO

CREATE INDEX [idx_ledger_business_branch_date] ON [StockLedger] ([BusinessId], [BranchId], [Date]);
GO

CREATE INDEX [idx_ledger_business_branch_product_variant_warehouse] ON [StockLedger] ([BusinessId], [BranchId], [ProductId], [VariantId], [WarehouseId]);
GO

CREATE INDEX [idx_ledger_business_branch_product_warehouse] ON [StockLedger] ([BusinessId], [BranchId], [ProductId], [WarehouseId]);
GO

CREATE INDEX [idx_stockledger_branchid] ON [StockLedger] ([BranchId]);
GO

CREATE INDEX [idx_stockledger_business_branch] ON [StockLedger] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_stockledger_businessid] ON [StockLedger] ([BusinessId]);
GO

CREATE INDEX [IX_StockLedger_ProductId] ON [StockLedger] ([ProductId]);
GO

CREATE INDEX [IX_StockLedger_VariantId] ON [StockLedger] ([VariantId]);
GO

CREATE INDEX [IX_StockLedger_WarehouseId] ON [StockLedger] ([WarehouseId]);
GO

CREATE INDEX [idx_subcategories_branchid] ON [SubCategories] ([BranchId]);
GO

CREATE INDEX [idx_subcategories_business_branch] ON [SubCategories] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_subcategories_businessid] ON [SubCategories] ([BusinessId]);
GO

CREATE INDEX [idx_subcategory_branch_category] ON [SubCategories] ([BranchId], [CategoryId]);
GO

CREATE UNIQUE INDEX [idx_subcategory_branch_code] ON [SubCategories] ([BranchId], [Code]) WHERE [Code] IS NOT NULL AND [Code] <> '' AND [IsDeleted] = 0;
GO

CREATE UNIQUE INDEX [idx_subcategory_category_name] ON [SubCategories] ([CategoryId], [Name]);
GO

CREATE UNIQUE INDEX [idx_supplier_branch_code] ON [Suppliers] ([BusinessId], [BranchId], [SupplierCode]) WHERE [SupplierCode] <> '' AND [IsDeleted] = 0;
GO

CREATE INDEX [idx_supplier_business_branch_name] ON [Suppliers] ([BusinessId], [BranchId], [Name]);
GO

CREATE INDEX [idx_suppliers_branchid] ON [Suppliers] ([BranchId]);
GO

CREATE INDEX [idx_suppliers_business_branch] ON [Suppliers] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_suppliers_businessid] ON [Suppliers] ([BusinessId]);
GO

CREATE INDEX [idx_unit_business_branch_code] ON [Units] ([BusinessId], [BranchId], [Code]);
GO

CREATE UNIQUE INDEX [idx_unit_business_branch_name] ON [Units] ([BusinessId], [BranchId], [Name]);
GO

CREATE INDEX [idx_units_branchid] ON [Units] ([BranchId]);
GO

CREATE INDEX [idx_units_business_branch] ON [Units] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_units_businessid] ON [Units] ([BusinessId]);
GO

CREATE INDEX [idx_userbranch_branchid] ON [UserBranches] ([BranchId]);
GO

CREATE INDEX [idx_userbranch_userid] ON [UserBranches] ([UserId]);
GO

CREATE UNIQUE INDEX [idx_warehouse_business_branch_name] ON [Warehouses] ([BusinessId], [BranchId], [Name]);
GO

CREATE INDEX [idx_warehouses_branchid] ON [Warehouses] ([BranchId]);
GO

CREATE INDEX [idx_warehouses_business_branch] ON [Warehouses] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_warehouses_businessid] ON [Warehouses] ([BusinessId]);
GO

ALTER TABLE [MenuItems] ADD CONSTRAINT [FK_MenuItems_SubCategories_SubCategoryId] FOREIGN KEY ([SubCategoryId]) REFERENCES [SubCategories] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260616153136_FullSchemaSync', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [idx_module_key] ON [Modules];
GO

UPDATE [Branches] SET [CreatedDate] = '2026-06-16T15:43:46.1464665Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [Businesses] SET [CreatedDate] = '2026-06-16T15:43:46.1463814Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

CREATE UNIQUE INDEX [idx_module_key] ON [Modules] ([ModuleKey]) WHERE [ModuleKey] <> '' AND [IsDeleted] = 0;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260616154348_FixModuleKeyUniqueIndex', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ExceptionLogs] (
    [Id] bigint NOT NULL IDENTITY,
    [UserId] bigint NULL,
    [BranchId] bigint NULL,
    [Module] nvarchar(100) NOT NULL,
    [FormName] nvarchar(100) NULL,
    [ActionName] nvarchar(100) NULL,
    [ExceptionMessage] nvarchar(max) NOT NULL,
    [StackTrace] nvarchar(max) NULL,
    [InnerException] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_ExceptionLogs] PRIMARY KEY ([Id])
);
GO

UPDATE [Branches] SET [CreatedDate] = '2026-06-17T04:26:32.8379008Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [Businesses] SET [CreatedDate] = '2026-06-17T04:26:32.8378483Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

CREATE INDEX [IX_ExceptionLogs_CreatedAt] ON [ExceptionLogs] ([CreatedAt]);
GO

CREATE INDEX [IX_ExceptionLogs_Module] ON [ExceptionLogs] ([Module]);
GO

CREATE INDEX [IX_ExceptionLogs_UserId] ON [ExceptionLogs] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260617042637_AddExceptionLogTable', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Products] ADD [AllowNegativeStock] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Products] ADD [EnableLowStockAlert] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Products] ADD [LowStockAlertLevel] decimal(18,4) NULL;
GO

ALTER TABLE [Products] ADD [OpeningStock] decimal(18,4) NOT NULL DEFAULT 0.0;
GO

CREATE TABLE [LowStockAlerts] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [VariantId] int NULL,
    [WarehouseId] int NOT NULL,
    [CurrentStock] decimal(18,4) NOT NULL,
    [AlertLevel] decimal(18,4) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [LastTriggeredAt] datetime2 NOT NULL,
    [BusinessId] int NOT NULL DEFAULT 1,
    [BranchId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedById] int NULL,
    [UpdatedDate] datetime2 NULL,
    [ModifiedById] int NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_LowStockAlerts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LowStockAlerts_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LowStockAlerts_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LowStockAlerts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LowStockAlerts_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO

UPDATE [Branches] SET [CreatedDate] = '2026-06-17T11:39:16.8914398Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [Businesses] SET [CreatedDate] = '2026-06-17T11:39:16.8913759Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

CREATE UNIQUE INDEX [idx_lowstockalert_product_variant_warehouse] ON [LowStockAlerts] ([BusinessId], [BranchId], [ProductId], [VariantId], [WarehouseId]) WHERE [IsDeleted] = 0;
GO

CREATE INDEX [idx_lowstockalerts_branchid] ON [LowStockAlerts] ([BranchId]);
GO

CREATE INDEX [idx_lowstockalerts_business_branch] ON [LowStockAlerts] ([BusinessId], [BranchId]);
GO

CREATE INDEX [idx_lowstockalerts_businessid] ON [LowStockAlerts] ([BusinessId]);
GO

CREATE INDEX [IX_LowStockAlerts_ProductId] ON [LowStockAlerts] ([ProductId]);
GO

CREATE INDEX [IX_LowStockAlerts_VariantId] ON [LowStockAlerts] ([VariantId]);
GO

CREATE INDEX [IX_LowStockAlerts_WarehouseId] ON [LowStockAlerts] ([WarehouseId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260617113918_AddProductStockFieldsAndLowStockAlerts', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF COL_LENGTH(N'dbo.Products', N'OpeningStockVariantWise') IS NULL
    ALTER TABLE [Products] ADD [OpeningStockVariantWise] bit NOT NULL CONSTRAINT [DF_Products_OpeningStockVariantWise] DEFAULT CAST(0 AS bit);
GO

UPDATE [Branches] SET [CreatedDate] = '2026-06-17T11:51:17.5964870Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [Businesses] SET [CreatedDate] = '2026-06-17T11:51:17.5963985Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260617115119_AddOpeningStockVariantWise', N'8.0.0');
GO

COMMIT;
GO

