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
