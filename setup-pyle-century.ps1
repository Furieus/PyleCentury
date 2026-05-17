Write-Host "Setting up clean Pyle Century Render-first backend..." -ForegroundColor Cyan

$folders = @(
    "backend/customer-account-api",
    "database/supabase",
    "docs"
)

foreach ($folder in $folders) {
    New-Item -ItemType Directory -Force -Path $folder | Out-Null
}

@"
# Pyle Century

Pyle Century is a suite of internal logistics tools.

## Current modules

- Customer Account API
- Supabase database schema
- Future Dock Commander desktop/mobile apps
- Future Route Sequencer
- Future Billing integration

## Current architecture

C# Desktop / Mobile Apps
        ↓
Python FastAPI Backend on Render
        ↓
Supabase PostgreSQL
"@ | Set-Content "README.md"

@"
# Python
.env
.venv/
venv/
__pycache__/
*.pyc
*.pyo
*.pyd

# C# / .NET
bin/
obj/
.vs/
*.user
*.suo

# Build output
release/
publish/

# OS files
.DS_Store
Thumbs.db

# Secrets
*.key
*.pem
appsettings.Production.json
"@ | Set-Content ".gitignore"

@"
fastapi
uvicorn[standard]
python-dotenv
pydantic
httpx
"@ | Set-Content "backend/customer-account-api/requirements.txt"

@"
services:
  - type: web
    name: pyle-century-customer-account-api
    env: python
    rootDir: backend/customer-account-api
    buildCommand: pip install -r requirements.txt
    startCommand: uvicorn main:app --host 0.0.0.0 --port `$PORT
    envVars:
      - key: SUPABASE_URL
        sync: false
      - key: SUPABASE_SERVICE_ROLE_KEY
        sync: false
"@ | Set-Content "backend/customer-account-api/render.yaml"

@"
import os
from datetime import datetime, timezone
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import httpx

SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_SERVICE_ROLE_KEY = os.getenv("SUPABASE_SERVICE_ROLE_KEY")

if not SUPABASE_URL or not SUPABASE_SERVICE_ROLE_KEY:
    raise RuntimeError("Missing Supabase environment variables.")

SUPABASE_REST_URL = f"{SUPABASE_URL.rstrip('/')}/rest/v1"

HEADERS = {
    "apikey": SUPABASE_SERVICE_ROLE_KEY,
    "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
    "Content-Type": "application/json",
    "Prefer": "return=representation"
}

app = FastAPI(title="Pyle Century Customer Account API")


class CustomerCreate(BaseModel):
    business_name: str
    address1: str
    address2: str | None = None
    city: str
    state: str
    zip_code: str
    phone: str | None = None
    contact_name: str | None = None
    contact_email: str | None = None

    requires_liftgate: bool = False
    requires_straight_truck: bool = False
    appointment_required: bool = False
    limited_access: bool = False
    call_before_delivery: bool = False
    inside_delivery: bool = False
    dock_available: bool = False
    forklift_available: bool = False
    pallet_jack_required: bool = False

    is_hazmat: bool = False
    un_number: str | None = None
    hazmat_class: str | None = None
    packing_group: str | None = None
    container_type: str | None = None
    proper_shipping_name: str | None = None
    placard_required: bool = False


def make_prefix(text: str, length: int) -> str:
    cleaned = "".join(ch for ch in text.upper() if ch.isalnum())
    return cleaned[:length].ljust(length, "X")


async def supabase_get(table: str, params: dict | None = None):
    async with httpx.AsyncClient(timeout=20.0) as client:
        response = await client.get(
            f"{SUPABASE_REST_URL}/{table}",
            headers=HEADERS,
            params=params
        )

    if response.status_code >= 400:
        raise HTTPException(status_code=response.status_code, detail=response.text)

    return response.json()


async def supabase_insert(table: str, payload: dict):
    async with httpx.AsyncClient(timeout=20.0) as client:
        response = await client.post(
            f"{SUPABASE_REST_URL}/{table}",
            headers=HEADERS,
            json=payload
        )

    if response.status_code >= 400:
        raise HTTPException(status_code=response.status_code, detail=response.text)

    return response.json()


async def generate_account_code(city: str, business_name: str) -> str:
    city_prefix = make_prefix(city, 3)
    business_prefix = make_prefix(business_name, 3)
    prefix = f"{city_prefix}-{business_prefix}"

    existing = await supabase_get(
        "customer_accounts",
        {
            "select": "account_code",
            "account_code": f"like.{prefix}-%"
        }
    )

    next_number = len(existing) + 1
    return f"{prefix}-{next_number:04d}"


@app.get("/")
def health_check():
    return {
        "service": "Pyle Century Customer Account API",
        "status": "online"
    }


@app.get("/health")
def health():
    return {
        "status": "ok"
    }


@app.post("/customers")
async def create_customer(customer: CustomerCreate):
    account_code = await generate_account_code(customer.city, customer.business_name)

    now = datetime.now(timezone.utc).isoformat()

    account_payload = {
        "account_code": account_code,
        "business_name": customer.business_name,
        "address1": customer.address1,
        "address2": customer.address2,
        "city": customer.city,
        "state": customer.state,
        "zip_code": customer.zip_code,
        "phone": customer.phone,
        "contact_name": customer.contact_name,
        "contact_email": customer.contact_email,

        "requires_liftgate": customer.requires_liftgate,
        "requires_straight_truck": customer.requires_straight_truck,
        "appointment_required": customer.appointment_required,
        "limited_access": customer.limited_access,
        "call_before_delivery": customer.call_before_delivery,
        "inside_delivery": customer.inside_delivery,
        "dock_available": customer.dock_available,
        "forklift_available": customer.forklift_available,
        "pallet_jack_required": customer.pallet_jack_required,

        "is_hazmat": customer.is_hazmat,
        "created_at": now,
        "updated_at": now,
    }

    inserted_customer = await supabase_insert("customer_accounts", account_payload)

    if not inserted_customer:
        raise HTTPException(status_code=500, detail="Failed to create customer account.")

    customer_id = inserted_customer[0]["id"]

    if customer.is_hazmat:
        hazmat_payload = {
            "customer_account_id": customer_id,
            "un_number": customer.un_number,
            "hazmat_class": customer.hazmat_class,
            "packing_group": customer.packing_group,
            "container_type": customer.container_type,
            "proper_shipping_name": customer.proper_shipping_name,
            "placard_required": customer.placard_required,
            "created_at": now,
        }

        await supabase_insert("customer_hazmat_profiles", hazmat_payload)

    return {
        "id": customer_id,
        "account_code": account_code,
        "business_name": customer.business_name,
        "message": "Customer account created successfully."
    }


@app.get("/customers/{account_code}")
async def get_customer(account_code: str):
    customers = await supabase_get(
        "customer_accounts",
        {
            "select": "*",
            "account_code": f"eq.{account_code}"
        }
    )

    if not customers:
        raise HTTPException(status_code=404, detail="Customer not found.")

    customer = customers[0]

    hazmat_profiles = await supabase_get(
        "customer_hazmat_profiles",
        {
            "select": "*",
            "customer_account_id": f"eq.{customer['id']}"
        }
    )

    return {
        "customer": customer,
        "hazmat_profiles": hazmat_profiles
    }
"@ | Set-Content "backend/customer-account-api/main.py"

@"
# Pyle Century Customer Account API

FastAPI backend for customer account creation, lookup, restrictions, and hazmat profiles.

## Render setup

Render settings:

Root Directory:
backend/customer-account-api

Build Command:
pip install -r requirements.txt

Start Command:
uvicorn main:app --host 0.0.0.0 --port `$PORT

Environment Variables:
SUPABASE_URL
SUPABASE_SERVICE_ROLE_KEY

## Test URLs

After deployment:

/
returns service status.

/docs
opens FastAPI Swagger API testing page.

/health
returns a basic health check.
"@ | Set-Content "backend/customer-account-api/README.md"

@"
-- Pyle Century Supabase schema starter

create table if not exists customer_accounts (
    id uuid primary key default gen_random_uuid(),
    account_code text unique not null,
    business_name text not null,
    address1 text not null,
    address2 text,
    city text not null,
    state text not null,
    zip_code text not null,
    phone text,
    contact_name text,
    contact_email text,

    requires_liftgate boolean default false,
    requires_straight_truck boolean default false,
    appointment_required boolean default false,
    limited_access boolean default false,
    call_before_delivery boolean default false,
    inside_delivery boolean default false,
    dock_available boolean default false,
    forklift_available boolean default false,
    pallet_jack_required boolean default false,

    is_hazmat boolean default false,

    created_at timestamptz default now(),
    updated_at timestamptz default now()
);

create table if not exists customer_hazmat_profiles (
    id uuid primary key default gen_random_uuid(),
    customer_account_id uuid references customer_accounts(id) on delete cascade,
    un_number text,
    hazmat_class text,
    packing_group text,
    container_type text,
    proper_shipping_name text,
    placard_required boolean default false,
    created_at timestamptz default now()
);
"@ | Set-Content "database/supabase/schema.sql"

@"
# Render Setup

## 1. Push this repo to GitHub

git add .
git commit -m "Clean Render-first Customer Account API"
git push

## 2. Create Supabase tables

Open:
database/supabase/schema.sql

Paste into:
Supabase > SQL Editor > New Query > Run

## 3. Create Render Web Service

Render > New > Web Service

Connect this GitHub repo.

Use:

Root Directory:
backend/customer-account-api

Build Command:
pip install -r requirements.txt

Start Command:
uvicorn main:app --host 0.0.0.0 --port `$PORT

## 4. Add Environment Variables in Render

SUPABASE_URL
SUPABASE_SERVICE_ROLE_KEY

Do not put those in GitHub.

## 5. Test

Open:

https://YOUR-RENDER-URL.onrender.com/

Then:

https://YOUR-RENDER-URL.onrender.com/docs
"@ | Set-Content "docs/render-setup.md"

Write-Host "Clean Pyle Century Render-first backend setup complete." -ForegroundColor Green
Write-Host ""
Write-Host "Next commands:" -ForegroundColor Yellow
Write-Host "git status"
Write-Host "git add ."
Write-Host "git commit -m `"Clean Render-first Customer Account API`""
Write-Host "git push"