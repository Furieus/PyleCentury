import os
import random
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

app = FastAPI(title="Pyle Century API")


class CustomerCreate(BaseModel):
    account_code: str | None = None
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


class ShipmentCreate(BaseModel):
    pro_number: str

    shipper_name: str
    shipper_address1: str | None = None
    shipper_city_state_zip: str | None = None
    shipper_phone: str | None = None

    consignee_account_code: str | None = None
    consignee_name: str
    consignee_address1: str | None = None
    consignee_address2: str | None = None
    consignee_city: str | None = None
    consignee_state: str | None = None
    consignee_zip_code: str | None = None
    consignee_phone: str | None = None
    consignee_contact_name: str | None = None
    consignee_contact_email: str | None = None

    product_description: str
    pieces: int = 0
    skids: int = 0
    weight_pounds: float = 0
    freight_class: str | None = None
    nmfc: str | None = None

    is_hazardous: bool = False
    un_number: str | None = None
    hazmat_class: str | None = None
    packing_group: str | None = None
    container_type: str | None = None
    proper_shipping_name: str | None = None

    is_foodstuffs: bool = False
    requires_liftgate: bool = False
    requires_straight_truck: bool = False
    appointment_required: bool = False
    limited_access: bool = False
    call_before_delivery: bool = False
    inside_delivery: bool = False

    special_instructions: str | None = None
    billing_status: str = "Created"


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


async def supabase_patch(table: str, match_column: str, match_value: str, payload: dict):
    async with httpx.AsyncClient(timeout=20.0) as client:
        response = await client.patch(
            f"{SUPABASE_REST_URL}/{table}",
            headers=HEADERS,
            params={match_column: f"eq.{match_value}"},
            json=payload
        )

    if response.status_code >= 400:
        raise HTTPException(status_code=response.status_code, detail=response.text)

    return response.json() if response.text else []


async def geocode_address(address1: str, city: str, state: str, zip_code: str) -> tuple[float | None, float | None]:
    query = f"{address1}, {city}, {state} {zip_code}, USA".strip()

    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            response = await client.get(
                "https://nominatim.openstreetmap.org/search",
                params={
                    "q": query,
                    "format": "json",
                    "limit": 1
                },
                headers={
                    "User-Agent": "PyleCenturyCustomerAccounts/0.1"
                }
            )

        if response.status_code >= 400:
            return None, None

        data = response.json()

        if not data:
            return None, None

        return float(data[0]["lat"]), float(data[0]["lon"])

    except Exception:
        return None, None


async def generate_account_code(city: str, business_name: str) -> str:
    city_prefix = make_prefix(city, 2)
    business_prefix = make_prefix(business_name, 2)
    prefix = f"{business_prefix}{city_prefix}"

    existing = await supabase_get(
        "customer_accounts",
        {
            "select": "account_code",
            "account_code": f"like.{prefix}%"
        }
    )

    next_number = len(existing) + 1
    return f"{prefix}{next_number:02d}"


@app.get("/")
def health_check():
    return {
        "service": "Pyle Century API",
        "status": "online"
    }


@app.get("/health")
def health():
    return {
        "status": "ok"
    }


@app.post("/customers")
async def create_or_update_customer(customer: CustomerCreate):
    account_code = customer.account_code.strip().upper() if customer.account_code else ""

    if not account_code:
        account_code = await generate_account_code(customer.city, customer.business_name)

    now = datetime.now(timezone.utc).isoformat()

    latitude, longitude = await geocode_address(
        customer.address1,
        customer.city,
        customer.state,
        customer.zip_code
    )

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
        "latitude": latitude,
        "longitude": longitude,
        "updated_at": now,
    }

    existing = await supabase_get(
        "customer_accounts",
        {
            "select": "id",
            "account_code": f"eq.{account_code}"
        }
    )

    if existing:
        await supabase_patch("customer_accounts", "account_code", account_code, account_payload)
        customer_id = existing[0]["id"]
    else:
        account_payload["created_at"] = now
        inserted_customer = await supabase_insert("customer_accounts", account_payload)

        if not inserted_customer:
            raise HTTPException(status_code=500, detail="Failed to create customer account.")

        customer_id = inserted_customer[0]["id"]

    # Replace hazmat profile for simplicity in this early build.
    # Existing rows are left alone if is_hazmat is false.
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
        "latitude": latitude,
        "longitude": longitude,
        "message": "Customer account saved successfully."
    }


@app.get("/customers/{account_code}")
async def get_customer(account_code: str):
    return await get_customer_by_code_or_name(account_code)


@app.get("/customers/lookup/{query}")
async def lookup_customer(query: str):
    return await get_customer_by_code_or_name(query)


async def get_customer_by_code_or_name(query: str):
    clean = query.strip()

    customers = await supabase_get(
        "customer_accounts",
        {
            "select": "*",
            "account_code": f"eq.{clean.upper()}"
        }
    )

    if not customers:
        customers = await supabase_get(
            "customer_accounts",
            {
                "select": "*",
                "business_name": f"ilike.*{clean}*",
                "limit": "1"
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


async def pro_exists(pro_number: str) -> bool:
    existing = await supabase_get(
        "billing_shipments",
        {
            "select": "pro_number",
            "pro_number": f"eq.{pro_number}"
        }
    )

    return len(existing) > 0


@app.get("/shipments/generate-pro")
async def generate_pro():
    for _ in range(100):
        pro_number = str(random.randint(100000000, 999999999))

        if not await pro_exists(pro_number):
            return {
                "pro_number": pro_number
            }

    raise HTTPException(status_code=500, detail="Unable to generate unique PRO number.")


@app.post("/shipments")
async def create_or_update_shipment(shipment: ShipmentCreate):
    if not shipment.pro_number or len(shipment.pro_number) != 9 or not shipment.pro_number.isdigit():
        raise HTTPException(status_code=400, detail="PRO number must be exactly 9 digits.")

    if shipment.is_hazardous and (not shipment.un_number or not shipment.hazmat_class):
        raise HTTPException(status_code=400, detail="Hazmat shipments require UN/NA number and class.")

    now = datetime.now(timezone.utc).isoformat()

    payload = shipment.model_dump()
    payload["updated_at"] = now

    existing = await supabase_get(
        "billing_shipments",
        {
            "select": "id",
            "pro_number": f"eq.{shipment.pro_number}"
        }
    )

    if existing:
        shipment_id = existing[0]["id"]

        async with httpx.AsyncClient(timeout=20.0) as client:
            response = await client.patch(
                f"{SUPABASE_REST_URL}/billing_shipments",
                headers=HEADERS,
                params={"id": f"eq.{shipment_id}"},
                json=payload
            )

        if response.status_code >= 400:
            raise HTTPException(status_code=response.status_code, detail=response.text)

        return {
            "pro_number": shipment.pro_number,
            "message": "Shipment updated successfully."
        }

    payload["created_at"] = now
    inserted = await supabase_insert("billing_shipments", payload)

    if not inserted:
        raise HTTPException(status_code=500, detail="Failed to save shipment.")

    return {
        "id": inserted[0]["id"],
        "pro_number": shipment.pro_number,
        "message": "Shipment saved successfully."
    }


@app.get("/shipments/{pro_number}")
async def get_shipment(pro_number: str):
    shipments = await supabase_get(
        "billing_shipments",
        {
            "select": "*",
            "pro_number": f"eq.{pro_number}"
        }
    )

    if not shipments:
        raise HTTPException(status_code=404, detail="Shipment not found.")

    return {
        "shipment": shipments[0]
    }
