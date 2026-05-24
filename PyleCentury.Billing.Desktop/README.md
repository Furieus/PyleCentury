# Pyle Century Billing Desktop

A first-pass WPF billing/PRO creation app with dummy/local data.

## Features

- Locked dummy shipper:
  - A Duie Pyle
  - 87 Aircraft Rd
  - Southington, CT 06489

- Consignee lookup by account code:
  - Uses Render Customer Account API if configured in `appsettings.json`
  - Falls back to built-in dummy accounts if API URL is blank or unreachable

- Dummy account codes included:
  - 
  - ABME17
  - HONH08

- Shipment entry:
  - Product description
  - Pieces
  - Skids
  - Weight
  - Freight class
  - NMFC / commodity code
  - Special instructions

- Service requirements:
  - Foodstuffs
  - Liftgate
  - Straight truck
  - Appointment required
  - Limited access
  - Call before delivery
  - Inside delivery

- Hazmat:
  - Hazardous shipment checkbox
  - UN / NA number
  - Hazmat class
  - Packing group
  - Container type
  - Proper shipping name

- PRO:
  - Generates random unique 9-digit PRO
  - Saves shipments to a local JSON ledger
  - Can look up saved shipments by PRO

## Optional API connection

Open `appsettings.json` and set:

```json
{
  "CustomerApiBaseUrl": "https://your-render-service.onrender.com"
}
```

Leave blank to use dummy customer data.

## Run

```powershell
dotnet run
```

## Publish release

```powershell
.\publish-windows-release.bat
```

Output:

```text
release\win-x64
```

## Local data storage

Saved shipments are stored here:

```text
%LOCALAPPDATA%\PyleCentury\Billing\shipments.json
```

## Extra fields added

I added a few operational fields beyond your initial list:

- Pieces
- Skids
- Weight
- Freight class
- NMFC / commodity code
- Appointment required
- Limited access
- Call before delivery
- Inside delivery
- Special instructions
- Billing status

These help the PRO record act like a real shipment record that can later feed Dock Commander, route sequencing, billing, and tracking.

## Render/Supabase shipment mode

This build is designed for multi-PC testing.

Set `appsettings.json`:

```json
{
  "ApiBaseUrl": "https://YOUR-RENDER-SERVICE.onrender.com"
}
```

Then Billing will use Render/Supabase for:

- customer lookup
- 9-digit PRO generation
- shipment save
- PRO lookup

Local JSON storage is no longer used by this build.

Before testing on other PCs, update your Render backend using the files in:

```text
backend-render-update/
```

Run the SQL file in Supabase:

```text
backend-render-update/supabase_billing_shipments.sql
```

Replace your backend `main.py` with:

```text
backend-render-update/main.py
```

Commit/push to GitHub, then let Render redeploy.
