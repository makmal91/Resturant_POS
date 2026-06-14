# Business + Branch Tenant Integration Guide

## 1. Tenant Headers
All APIs now support automatic tenant context via headers:

- `X-Business-Id`
- `X-Branch-Id`

The React app now sends these automatically for every request.

## 2. Sample Payloads

### Create Business
`POST /api/businesses`

```json
{
  "name": "AKHS Foods",
  "legalName": "AKHS Foods Private Limited",
  "logoUrl": "https://cdn.example.com/logo.png",
  "phone": "+92-300-0000000",
  "email": "admin@akhsfoods.com",
  "address": "Main Boulevard, Lahore",
  "taxNumber": "NTN-1234567",
  "currency": "PKR",
  "timeZone": "Asia/Karachi",
  "isActive": true
}
```

### Create Branch (Backward Compatible)
`POST /api/branches`

```json
{
  "name": "Gulberg Branch",
  "code": "GLB",
  "address": "MM Alam Road",
  "city": "Lahore",
  "phone": "+92-300-1111111",
  "email": "gulberg@akhsfoods.com",
  "taxRate": 16,
  "currency": "PKR",
  "businessId": 1,
  "companyId": 1,
  "isActive": true
}
```

### Create Product (Business + Branch attached)
`POST /api/products`

```json
{
  "name": "Chicken Burger",
  "description": "Grilled chicken burger",
  "price": 850,
  "tax": 16,
  "preparationTime": 12,
  "menuCategoryId": 4,
  "businessId": 1,
  "branchId": 2,
  "productType": "FinishedGood",
  "variants": [],
  "addons": []
}
```

### Create Order
`POST /api/orders/create`

```json
{
  "orderType": "DineIn",
  "tableId": 10,
  "waiterId": 5,
  "notes": "No onions",
  "businessId": 1,
  "branchId": 2
}
```

### Inventory Purchase
`POST /api/inventory/purchase`

```json
{
  "itemId": 12,
  "quantity": 30,
  "businessId": 1,
  "branchId": 2
}
```

## 3. Reports

### Branch Sales Report
`GET /api/reports/sales?businessId=1&branchId=2&from=2026-04-01&to=2026-04-30`

### Business Aggregate Sales
`GET /api/reports/sales-by-business?businessId=1&from=2026-04-01&to=2026-04-30`

### Branch Inventory
`GET /api/reports/inventory?businessId=1&branchId=2`

## 4. Migration Files

- EF migration: `Infrastructure/Migrations/20260419123845_AddBusinessTenantIsolation.cs`
- Generated SQL script: `Infrastructure/MigrationScripts/20260419123845_AddBusinessTenantIsolation.sql`

## 5. Default Existing Data Strategy

- Existing rows receive `BusinessId = 1` by migration defaults.
- Seeded default business has `Id = 1`.
- Existing branch seed is linked to `BusinessId = 1`.
