import os
from datetime import datetime
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from dotenv import load_dotenv
from supabase import create_client, Client

load_dotenv()

SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_SERVICE_ROLE_KEY = os.getenv("SUPABASE_SERVICE_ROLE_KEY")

if not SUPABASE_URL or not SUPABASE_SERVICE_ROLE_KEY:
    raise RuntimeError("Missing Supabase environment variables.")

supabase: Client = create_client(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY)

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


def generate_account_code(city: str, business_name: str) -> str:
    city_prefix = make_prefix(city, 3)
    business_prefix = make_prefix(business_name, 3)

    prefix = f"{city_prefix}-{business_prefix}"

    existing = (
        supabase.table("customer_accounts")
        .select("account_code")
        .like("account_code", f"{prefix}-%")
        .execute()
    )

    next_number = len(existing.data) + 1
    return f"{prefix}-{next_number:04d}"


@app.get("/")
def health_check():
    return {
        "service": "Pyle Century Customer Account API",
        "status": "online"
    }


@app.post("/customers")
def create_customer(customer: CustomerCreate):
    account_code = generate_account_code(customer.city, customer.business_name)

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
        "created_at": datetime.utcnow().isoformat(),
        "updated_at": datetime.utcnow().isoformat(),
    }

    result = supabase.table("customer_accounts").insert(account_payload).execute()

    if not result.data:
        raise HTTPException(status_code=500, detail="Failed to create customer account.")

    customer_id = result.data[0]["id"]

    if customer.is_hazmat:
        hazmat_payload = {
            "customer_account_id": customer_id,
            "un_number": customer.un_number,
            "hazmat_class": customer.hazmat_class,
            "packing_group": customer.packing_group,
            "container_type": customer.container_type,
            "proper_shipping_name": customer.proper_shipping_name,
            "placard_required": customer.placard_required,
            "created_at": datetime.utcnow().isoformat(),
        }

        supabase.table("customer_hazmat_profiles").insert(hazmat_payload).execute()

    return {
        "id": customer_id,
        "account_code": account_code,
        "business_name": customer.business_name,
        "message": "Customer account created successfully."
    }


@app.get("/customers/{account_code}")
def get_customer(account_code: str):
    result = (
        supabase.table("customer_accounts")
        .select("*")
        .eq("account_code", account_code)
        .single()
        .execute()
    )

    if not result.data:
        raise HTTPException(status_code=404, detail="Customer not found.")

    customer_id = result.data["id"]

    hazmat = (
        supabase.table("customer_hazmat_profiles")
        .select("*")
        .eq("customer_account_id", customer_id)
        .execute()
    )

    return {
        "customer": result.data,
        "hazmat_profiles": hazmat.data
    }
