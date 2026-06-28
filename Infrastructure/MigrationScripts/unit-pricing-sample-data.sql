-- Sample multi-unit pricing data (Cement + Pipe business cases)
-- Run AFTER multi-unit-inventory-alter.sql and with valid BusinessId / BranchId.
-- Adjust @BusinessId and @BranchId before executing.

SET NOCOUNT ON;

DECLARE @BusinessId INT = 1;
DECLARE @BranchId INT = 1;
DECLARE @CategoryId INT = (SELECT TOP 1 [Id] FROM [dbo].[Categories] WHERE [BusinessId] = @BusinessId AND [BranchId] = @BranchId AND [IsDeleted] = 0);

IF @CategoryId IS NULL
BEGIN
    RAISERROR('No category found for the given BusinessId/BranchId.', 16, 1);
    RETURN;
END

-- Unit Master (skip if already exists)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Units] WHERE [BusinessId]=@BusinessId AND [BranchId]=@BranchId AND [Name]=N'Bori' AND [IsDeleted]=0)
    INSERT INTO [dbo].[Units] ([BusinessId],[BranchId],[Name],[Code],[DefaultConversionFactor],[Status],[IsDeleted],[CreatedAt])
    VALUES (@BusinessId,@BranchId,N'Bori',N'BORI',1,1,0,GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Units] WHERE [BusinessId]=@BusinessId AND [BranchId]=@BranchId AND [Name]=N'KG' AND [IsDeleted]=0)
    INSERT INTO [dbo].[Units] ([BusinessId],[BranchId],[Name],[Code],[DefaultConversionFactor],[Status],[IsDeleted],[CreatedAt])
    VALUES (@BusinessId,@BranchId,N'KG',N'KG',50,1,0,GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Units] WHERE [BusinessId]=@BusinessId AND [BranchId]=@BranchId AND [Name]=N'Pipe' AND [IsDeleted]=0)
    INSERT INTO [dbo].[Units] ([BusinessId],[BranchId],[Name],[Code],[DefaultConversionFactor],[Status],[IsDeleted],[CreatedAt])
    VALUES (@BusinessId,@BranchId,N'Pipe',N'PIPE',1,1,0,GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Units] WHERE [BusinessId]=@BusinessId AND [BranchId]=@BranchId AND [Name]=N'Feet' AND [IsDeleted]=0)
    INSERT INTO [dbo].[Units] ([BusinessId],[BranchId],[Name],[Code],[DefaultConversionFactor],[Status],[IsDeleted],[CreatedAt])
    VALUES (@BusinessId,@BranchId,N'Feet',N'FT',20,1,0,GETUTCDATE());

DECLARE @BoriUnitId INT = (SELECT [Id] FROM [dbo].[Units] WHERE [BusinessId]=@BusinessId AND [BranchId]=@BranchId AND [Name]=N'Bori' AND [IsDeleted]=0);
DECLARE @KgUnitId INT = (SELECT [Id] FROM [dbo].[Units] WHERE [BusinessId]=@BusinessId AND [BranchId]=@BranchId AND [Name]=N'KG' AND [IsDeleted]=0);
DECLARE @PipeUnitId INT = (SELECT [Id] FROM [dbo].[Units] WHERE [BusinessId]=@BusinessId AND [BranchId]=@BranchId AND [Name]=N'Pipe' AND [IsDeleted]=0);
DECLARE @FeetUnitId INT = (SELECT [Id] FROM [dbo].[Units] WHERE [BusinessId]=@BusinessId AND [BranchId]=@BranchId AND [Name]=N'Feet' AND [IsDeleted]=0);

-- ─── Cement: Bori @ 1250, KG auto 25, override 28 ───────────────────────────
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [ProductCode]=N'CEMENT-001' AND [BusinessId]=@BusinessId AND [BranchId]=@BranchId AND [IsDeleted]=0)
BEGIN
    INSERT INTO [dbo].[Products] (
        [BusinessId],[BranchId],[ProductName],[ProductCode],[SKU],[CategoryId],
        [CostPrice],[SellingPrice],[WholesalePrice],[UseAutoUnitPricing],[Status],[IsDeleted],[CreatedAt]
    )
    VALUES (@BusinessId,@BranchId,N'Portland Cement',N'CEMENT-001',N'CEM-001',@CategoryId,
        1000,1250,1200,1,1,0,GETUTCDATE());

    DECLARE @CementId INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[ProductUnits] (
        [BusinessId],[BranchId],[ProductId],[UnitId],[UnitName],[ConversionFactor],
        [IsBaseUnit],[CostPrice],[SellingPrice],[WholesalePrice],[IsPriceOverridden],[IsDeleted],[CreatedAt]
    )
    VALUES
        (@BusinessId,@BranchId,@CementId,@BoriUnitId,N'Bori',1,1,1000,1250,1200,0,0,GETUTCDATE()),
        (@BusinessId,@BranchId,@CementId,@KgUnitId,N'KG',50,0,25,28,27,1,0,GETUTCDATE());

    UPDATE [dbo].[Products]
    SET [BaseUnitId] = (SELECT TOP 1 [Id] FROM [dbo].[ProductUnits] WHERE [ProductId]=@CementId AND [IsBaseUnit]=1)
    WHERE [Id]=@CementId;
END

-- ─── Pipe: Pipe @ 1000, Feet auto 50 ─────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [ProductCode]=N'PIPE-001' AND [BusinessId]=@BusinessId AND [BranchId]=@BranchId AND [IsDeleted]=0)
BEGIN
    INSERT INTO [dbo].[Products] (
        [BusinessId],[BranchId],[ProductName],[ProductCode],[SKU],[CategoryId],
        [CostPrice],[SellingPrice],[WholesalePrice],[UseAutoUnitPricing],[Status],[IsDeleted],[CreatedAt]
    )
    VALUES (@BusinessId,@BranchId,N'PVC Pipe 2in',N'PIPE-001',N'PIPE-001',@CategoryId,
        800,1000,950,1,1,0,GETUTCDATE());

    DECLARE @PipeProdId INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[ProductUnits] (
        [BusinessId],[BranchId],[ProductId],[UnitId],[UnitName],[ConversionFactor],
        [IsBaseUnit],[CostPrice],[SellingPrice],[WholesalePrice],[IsPriceOverridden],[IsDeleted],[CreatedAt]
    )
    VALUES
        (@BusinessId,@BranchId,@PipeProdId,@PipeUnitId,N'Pipe',1,1,800,1000,950,0,0,GETUTCDATE()),
        (@BusinessId,@BranchId,@PipeProdId,@FeetUnitId,N'Feet',20,0,40,50,47.5,0,0,GETUTCDATE());

    UPDATE [dbo].[Products]
    SET [BaseUnitId] = (SELECT TOP 1 [Id] FROM [dbo].[ProductUnits] WHERE [ProductId]=@PipeProdId AND [IsBaseUnit]=1)
    WHERE [Id]=@PipeProdId;
END

PRINT 'Unit pricing sample data inserted (Cement + Pipe).';
PRINT 'Cement: 1 Bori = 1250, KG auto = 25, KG override = 28';
PRINT 'Pipe: 1 Pipe = 1000, Feet auto = 50 (factor 20 feet per pipe)';
