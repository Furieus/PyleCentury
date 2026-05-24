-- ============================================================
-- Pyle Century Clean Rebuild Schema
-- Supabase / Postgres
-- ============================================================
-- Use this after removing old Pyle Century tables.
--
-- This schema creates:
-- - Terminals
-- - Employees / Windows login profiles
-- - Access levels 1-4
-- - Module access
-- - Menu launch sessions
-- - Dock Commander terminal layouts
-- - RPS buckets / route areas
-- - RPS freight stops
-- - RPS clipped/planned runs
-- - RPS realtime event tracking
--
-- No local data JSON is required for production data.
-- Apps should read/write through Render backend -> Supabase Postgres.
-- ============================================================

create extension if not exists pgcrypto;

-- ============================================================
-- 1. CORE TERMINALS
-- ============================================================

create table pc_terminals (
    terminal_code text primary key,
    terminal_name text not null,
    state text not null,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

insert into pc_terminals (terminal_code, terminal_name, state, is_active)
values
    ('CON', 'Southington', 'CT', true),
    ('NYN', 'Newburgh', 'NY', true),
    ('NYX', 'Bronx', 'NY', true),
    ('NYA', 'Albany', 'NY', true),
    ('NYB', 'Buffalo', 'NY', true),
    ('NYM', 'Maspeth', 'NY', true),
    ('NYR', 'Rochester', 'NY', true),
    ('NYS', 'Syracuse', 'NY', true),
    ('MAS', 'Northborough', 'MA', true),
    ('MAW', 'Westfield', 'MA', true),
    ('NJC', 'Carteret', 'NJ', true),
    ('NJE', 'East Brunswick', 'NJ', true),
    ('NJW', 'Westampton', 'NJ', true),
    ('BAL', 'Baltimore', 'MD', true),
    ('ALE', 'Allentown', 'PA', true),
    ('ALT', 'Altoona', 'PA', true),
    ('PAC', 'Camp Hill', 'PA', true),
    ('PAE', 'Erie', 'PA', true),
    ('PGH', 'Pittsburgh', 'PA', true),
    ('YRK', 'York', 'PA', true)
on conflict (terminal_code) do update set
    terminal_name = excluded.terminal_name,
    state = excluded.state,
    is_active = excluded.is_active,
    updated_at = now();

-- ============================================================
-- 2. MODULES
-- ============================================================

create table pc_modules (
    module_code text primary key,
    module_name text not null,
    is_active boolean not null default true,
    created_at timestamptz not null default now()
);

insert into pc_modules (module_code, module_name, is_active)
values
    ('DockCommander', 'Dock Commander', true),
    ('Billing', 'Billing', true),
    ('RPS', 'Route Planning System', true),
    ('Dispatch', 'Dispatch', true),
    ('RouteGrid', 'Route Grid Maintenance', true),
    ('Admin', 'Administration', true)
on conflict (module_code) do update set
    module_name = excluded.module_name,
    is_active = excluded.is_active;

-- ============================================================
-- 3. ACCESS LEVELS
-- ============================================================

create table pc_access_level_definitions (
    access_level integer primary key,
    level_name text not null,
    description text not null,
    dock_home_permission text not null,
    dock_other_permission text not null,
    billing_permission text not null,
    admin_permission text not null,
    created_at timestamptz not null default now()
);

insert into pc_access_level_definitions (
    access_level,
    level_name,
    description,
    dock_home_permission,
    dock_other_permission,
    billing_permission,
    admin_permission
)
values
(1, 'Terminal Operations',
    'Dock Commander read/write at home terminal, Dock Commander read-only everywhere else, no Billing.',
    'read_write', 'read_only', 'none', 'none'),

(2, 'Multi-Terminal Operations',
    'Dock Commander read/write at home terminal and other terminals, no Billing.',
    'read_write', 'read_write', 'none', 'none'),

(3, 'Read Only + Billing',
    'Dock Commander read-only at every terminal, Billing access allowed.',
    'read_only', 'read_only', 'read_write', 'none'),

(4, 'Administrator',
    'Full admin access to all terminals and modules.',
    'admin', 'admin', 'admin', 'admin')
on conflict (access_level) do update set
    level_name = excluded.level_name,
    description = excluded.description,
    dock_home_permission = excluded.dock_home_permission,
    dock_other_permission = excluded.dock_other_permission,
    billing_permission = excluded.billing_permission,
    admin_permission = excluded.admin_permission;

-- ============================================================
-- 4. EMPLOYEES / WINDOWS LOGIN
-- ============================================================

create table pc_employees (
    id uuid primary key default gen_random_uuid(),

    -- Optional full whoami value, for example:
    -- DESKTOP-12345\dan
    -- DOMAIN\drosengrant
    windows_identity text,

    -- Username only, without the PC/domain prefix.
    windows_username text unique not null,

    -- PC name or domain name.
    windows_domain text,

    display_name text not null,
    employee_number text unique not null,

    home_terminal_code text not null references pc_terminals(terminal_code),
    access_level integer not null default 1 references pc_access_level_definitions(access_level),

    is_active boolean not null default true,

    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index ix_pc_employees_windows_username on pc_employees(windows_username);
create index ix_pc_employees_employee_number on pc_employees(employee_number);
create index ix_pc_employees_home_terminal on pc_employees(home_terminal_code);
create index ix_pc_employees_access_level on pc_employees(access_level);

create table pc_employee_terminal_access (
    id uuid primary key default gen_random_uuid(),
    employee_id uuid not null references pc_employees(id) on delete cascade,
    terminal_code text not null references pc_terminals(terminal_code),
    created_at timestamptz not null default now(),
    unique(employee_id, terminal_code)
);

create table pc_employee_module_access (
    id uuid primary key default gen_random_uuid(),
    employee_id uuid not null references pc_employees(id) on delete cascade,
    module_code text not null references pc_modules(module_code),
    permission_level text not null default 'User',
    created_at timestamptz not null default now(),
    unique(employee_id, module_code)
);

-- ============================================================
-- 5. MENU SESSIONS
-- ============================================================

create table pc_menu_sessions (
    session_token text primary key,
    employee_id uuid not null references pc_employees(id) on delete cascade,
    windows_identity text not null,
    module_code text not null references pc_modules(module_code),
    terminal_code text not null references pc_terminals(terminal_code),
    expires_at timestamptz not null,
    created_at timestamptz not null default now(),
    used_at timestamptz
);

create index ix_pc_menu_sessions_employee on pc_menu_sessions(employee_id);
create index ix_pc_menu_sessions_expires on pc_menu_sessions(expires_at);

-- ============================================================
-- 6. DOCK COMMANDER
-- ============================================================

create table pc_dock_layouts (
    id uuid primary key default gen_random_uuid(),
    terminal_code text not null references pc_terminals(terminal_code),
    layout_name text not null default 'default',
    layout_json jsonb not null default '{}'::jsonb,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    unique(terminal_code, layout_name)
);

create table pc_dock_door_state (
    id uuid primary key default gen_random_uuid(),
    terminal_code text not null references pc_terminals(terminal_code),
    door_number integer not null,
    door_status text not null default 'available',
    trailer_number text,
    route_name text,
    notes text,
    updated_by_employee_id uuid references pc_employees(id),
    updated_at timestamptz not null default now(),
    row_version bigint not null default 1,
    unique(terminal_code, door_number)
);

create index ix_pc_dock_door_state_terminal on pc_dock_door_state(terminal_code);

-- Known mapped layouts.
-- CON = default/original app layout.
-- NYN = equal-slot geometry:
-- Top slots: 12..1, four blank spacers, 56..43
-- Bottom slots: 13..42 continuous
insert into pc_dock_layouts (terminal_code, layout_name, layout_json, is_active)
values
(
    'CON',
    'default',
    '{
      "terminalCode": "CON",
      "layoutName": "default",
      "style": "DockCommanderDefault",
      "mapped": true,
      "notes": ["Original/default Dock Commander layout"]
    }'::jsonb,
    true
),
(
    'NYN',
    'default',
    '{
      "terminalCode": "NYN",
      "layoutName": "default",
      "style": "DockCommanderDefault",
      "mapped": true,
      "topSlots": [12,11,10,9,8,7,6,5,4,3,2,1,null,null,null,null,56,55,54,53,52,51,50,49,48,47,46,45,44,43],
      "bottomSlots": [13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42],
      "notes": [
        "All door slots equal size",
        "No blank gap between 24 and 25",
        "12 lines up with 13",
        "56 lines up with 29",
        "43 lines up with 42",
        "Freight/status data is not copied from reference screenshots"
      ]
    }'::jsonb,
    true
)
on conflict (terminal_code, layout_name) do update set
    layout_json = excluded.layout_json,
    is_active = excluded.is_active,
    updated_at = now();

-- ============================================================
-- 7. RPS ROUTE GRID / BUCKET SETUP
-- ============================================================

create table rps_route_areas (
    id uuid primary key default gen_random_uuid(),
    terminal_code text not null references pc_terminals(terminal_code),
    route_area_name text not null,
    display_order integer not null default 0,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    unique(terminal_code, route_area_name)
);

create table rps_route_area_zipcodes (
    id uuid primary key default gen_random_uuid(),
    route_area_id uuid not null references rps_route_areas(id) on delete cascade,
    zip_code text not null,
    created_at timestamptz not null default now(),
    unique(route_area_id, zip_code)
);

create index ix_rps_route_areas_terminal on rps_route_areas(terminal_code);
create index ix_rps_route_zipcodes_zip on rps_route_area_zipcodes(zip_code);

-- Seed CON route buckets from current planning direction.
insert into rps_route_areas (terminal_code, route_area_name, display_order, is_active)
values
('CON', 'WINDSOR', 10, true),
('CON', 'ENFIELD', 20, true),
('CON', 'SOUTH WINDSOR', 30, true),
('CON', 'HARTFORD', 40, true),
('CON', '21P1', 50, true),
('CON', 'ELLINGTON', 60, true),
('CON', 'MANCHESTER', 70, true),
('CON', 'NEW LONDON', 80, true),
('CON', 'CASINOS', 90, true),
('CON', 'ROUTE 9', 100, true),
('CON', 'MIDDLETOWN', 110, true),
('CON', 'NEW BRITAIN', 120, true),
('CON', 'BRANFORD', 130, true),
('CON', 'NEW HAVEN', 140, true),
('CON', 'WALLINGFORD', 150, true),
('CON', 'FAIRFIELD', 160, true),
('CON', 'BRIDGEPORT', 170, true),
('CON', 'WEST HAVEN', 180, true),
('CON', 'REDDING', 190, true),
('CON', 'NEWTOWN', 200, true),
('CON', 'VALLEY', 210, true),
('CON', 'TORRINGTON', 220, true),
('CON', 'NEW MILFORD', 230, true),
('CON', 'WATERBURY', 240, true),
('CON', 'AVON', 250, true),
('CON', 'LOCAL/BRISTOL', 260, true)
on conflict (terminal_code, route_area_name) do update set
    display_order = excluded.display_order,
    is_active = excluded.is_active,
    updated_at = now();

-- Example ZIPs for SOUTH WINDSOR from current notes.
insert into rps_route_area_zipcodes (route_area_id, zip_code)
select ra.id, z.zip_code
from rps_route_areas ra
cross join (
    values
        ('06028'),
        ('06074'),
        ('06108'),
        ('06115'),
        ('06118'),
        ('06132')
) as z(zip_code)
where ra.terminal_code = 'CON'
  and ra.route_area_name = 'SOUTH WINDSOR'
on conflict do nothing;

-- ============================================================
-- 8. RPS FREIGHT / STOPS
-- ============================================================

create table rps_freight_stops (
    id uuid primary key default gen_random_uuid(),

    terminal_code text not null references pc_terminals(terminal_code),
    route_area_id uuid references rps_route_areas(id),

    pro_number text,
    bill_number text,
    customer_name text,
    consignee_name text,

    address1 text,
    address2 text,
    city text,
    state text,
    zip_code text,

    pieces integer not null default 0,
    pallets integer not null default 0,
    weight_lbs integer not null default 0,

    has_liftgate boolean not null default false,
    has_hazmat boolean not null default false,
    has_freezable boolean not null default false,
    appointment_required boolean not null default false,
    appointment_start timestamptz,
    appointment_end timestamptz,

    -- bucket = unplanned/left side bucket
    -- run = assigned to an active/clipped/planned run
    -- removed = temporarily removed/hidden from planning
    planning_status text not null default 'bucket',

    sort_order numeric(12,3) not null default 0,

    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    updated_by_employee_id uuid references pc_employees(id),

    row_version bigint not null default 1
);

create index ix_rps_freight_terminal on rps_freight_stops(terminal_code);
create index ix_rps_freight_route_area on rps_freight_stops(route_area_id);
create index ix_rps_freight_zip on rps_freight_stops(zip_code);
create index ix_rps_freight_status on rps_freight_stops(planning_status);

-- ============================================================
-- 9. RPS RUNS / CLIPPED RUNS / SAVED RUNS
-- ============================================================

create table rps_runs (
    id uuid primary key default gen_random_uuid(),

    terminal_code text not null references pc_terminals(terminal_code),
    route_area_id uuid references rps_route_areas(id),

    run_name text not null,

    -- active = currently being planned
    -- clipped = planned/clipped run not committed to trailer/load
    -- saved = committed/saved to trailer/load
    -- locked = saved and locked from edits
    -- deleted = soft-deleted
    run_status text not null default 'active',

    trailer_number text,
    driver_name text,
    notes text,

    created_by_employee_id uuid references pc_employees(id),
    updated_by_employee_id uuid references pc_employees(id),

    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),

    row_version bigint not null default 1,

    unique(terminal_code, route_area_id, run_name)
);

create index ix_rps_runs_terminal_area on rps_runs(terminal_code, route_area_id);
create index ix_rps_runs_status on rps_runs(run_status);

create table rps_run_stops (
    id uuid primary key default gen_random_uuid(),

    run_id uuid not null references rps_runs(id) on delete cascade,
    freight_stop_id uuid not null references rps_freight_stops(id) on delete cascade,

    stop_sequence numeric(12,3) not null default 0,

    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    updated_by_employee_id uuid references pc_employees(id),

    unique(run_id, freight_stop_id)
);

create index ix_rps_run_stops_run on rps_run_stops(run_id);
create index ix_rps_run_stops_freight on rps_run_stops(freight_stop_id);

-- ============================================================
-- 10. RPS REALTIME EVENTS
-- ============================================================
-- Backend should insert one row per meaningful change.
--
-- WPF RPS app should:
-- 1. Load initial bucket snapshot by terminal_code + route_area_id.
-- 2. Open websocket to Render:
--    /ws/rps/{terminal_code}/{route_area_id}
-- 3. When a user moves/removes/sequences a stop:
--    POST mutation to backend.
-- 4. Backend writes transaction to Postgres and inserts rps_bucket_events row.
-- 5. Backend broadcasts the event to all clients watching that bucket.
-- 6. Other clients reload the affected stop/run or refresh the bucket snapshot.
--
-- This gives immediate multi-user sync.

create table rps_bucket_events (
    id uuid primary key default gen_random_uuid(),

    terminal_code text not null references pc_terminals(terminal_code),
    route_area_id uuid references rps_route_areas(id),

    event_type text not null,
    entity_type text not null,
    entity_id uuid,

    payload jsonb not null default '{}'::jsonb,

    created_by_employee_id uuid references pc_employees(id),
    created_at timestamptz not null default now(),

    -- Useful for client-side incremental sync.
    bucket_sequence bigserial
);

create index ix_rps_bucket_events_scope on rps_bucket_events(terminal_code, route_area_id, bucket_sequence);
create index ix_rps_bucket_events_created_at on rps_bucket_events(created_at);

-- Optional Postgres NOTIFY trigger for backend listeners.
-- Render can also poll rps_bucket_events if websocket process restarts.
create or replace function rps_notify_bucket_event()
returns trigger
language plpgsql
as $$
begin
    perform pg_notify(
        'rps_bucket_events',
        json_build_object(
            'id', new.id,
            'terminalCode', new.terminal_code,
            'routeAreaId', new.route_area_id,
            'eventType', new.event_type,
            'entityType', new.entity_type,
            'entityId', new.entity_id,
            'bucketSequence', new.bucket_sequence,
            'createdAt', new.created_at
        )::text
    );

    return new;
end;
$$;

drop trigger if exists trg_rps_notify_bucket_event on rps_bucket_events;

create trigger trg_rps_notify_bucket_event
after insert on rps_bucket_events
for each row
execute function rps_notify_bucket_event();

-- ============================================================
-- 11. UPDATED_AT / ROW_VERSION HELPERS
-- ============================================================

create or replace function pc_touch_row()
returns trigger
language plpgsql
as $$
begin
    new.updated_at = now();

    if to_jsonb(new) ? 'row_version' then
        new.row_version = coalesce(old.row_version, 0) + 1;
    end if;

    return new;
end;
$$;

drop trigger if exists trg_pc_employees_touch on pc_employees;
create trigger trg_pc_employees_touch
before update on pc_employees
for each row execute function pc_touch_row();

drop trigger if exists trg_pc_dock_door_state_touch on pc_dock_door_state;
create trigger trg_pc_dock_door_state_touch
before update on pc_dock_door_state
for each row execute function pc_touch_row();

drop trigger if exists trg_rps_freight_stops_touch on rps_freight_stops;
create trigger trg_rps_freight_stops_touch
before update on rps_freight_stops
for each row execute function pc_touch_row();

drop trigger if exists trg_rps_runs_touch on rps_runs;
create trigger trg_rps_runs_touch
before update on rps_runs
for each row execute function pc_touch_row();

drop trigger if exists trg_rps_run_stops_touch on rps_run_stops;
create trigger trg_rps_run_stops_touch
before update on rps_run_stops
for each row execute function pc_touch_row();

-- ============================================================
-- 12. VIEWS / PERMISSION FUNCTIONS
-- ============================================================

create or replace view pc_employee_access_view as
select
    e.id,
    e.windows_identity,
    e.windows_username,
    e.windows_domain,
    e.display_name,
    e.employee_number,
    e.home_terminal_code,
    t.terminal_name as home_terminal_name,
    e.access_level,
    ald.level_name,
    ald.description as access_description,
    ald.dock_home_permission,
    ald.dock_other_permission,
    ald.billing_permission,
    ald.admin_permission,
    e.is_active,
    e.created_at,
    e.updated_at
from pc_employees e
join pc_terminals t on t.terminal_code = e.home_terminal_code
join pc_access_level_definitions ald on ald.access_level = e.access_level;

create or replace function pc_dock_permission(
    p_employee_number text,
    p_terminal_code text
)
returns text
language plpgsql
stable
as $$
declare
    v_home_terminal text;
    v_access_level integer;
begin
    select home_terminal_code, access_level
    into v_home_terminal, v_access_level
    from pc_employees
    where employee_number = p_employee_number
      and is_active = true;

    if v_access_level is null then
        return 'none';
    end if;

    if v_access_level = 1 then
        if upper(v_home_terminal) = upper(p_terminal_code) then
            return 'read_write';
        else
            return 'read_only';
        end if;
    end if;

    if v_access_level = 2 then
        return 'read_write';
    end if;

    if v_access_level = 3 then
        return 'read_only';
    end if;

    if v_access_level = 4 then
        return 'admin';
    end if;

    return 'none';
end;
$$;

create or replace function pc_can_open_module(
    p_employee_number text,
    p_module_code text
)
returns boolean
language plpgsql
stable
as $$
declare
    v_access_level integer;
    v_module text := lower(p_module_code);
begin
    select access_level
    into v_access_level
    from pc_employees
    where employee_number = p_employee_number
      and is_active = true;

    if v_access_level is null then
        return false;
    end if;

    if v_access_level = 1 then
        return v_module in ('dockcommander');
    end if;

    if v_access_level = 2 then
        return v_module in ('dockcommander');
    end if;

    if v_access_level = 3 then
        return v_module in ('dockcommander', 'billing');
    end if;

    if v_access_level = 4 then
        return true;
    end if;

    return false;
end;
$$;

-- ============================================================
-- 13. SEED DAN AS LEVEL 4
-- ============================================================
-- IMPORTANT:
-- Replace these before running if you know the real whoami value.
--
-- Run on Windows:
--     whoami
--
-- Example:
--     DESKTOP-12345\dan
--
-- Replace:
--     dan
--     dan
--     
-- ============================================================

-- Uncomment and edit this block after you get whoami.
--
-- insert into pc_employees (
--     windows_identity,
--     windows_username,
--     windows_domain,
--     display_name,
--     employee_number,
--     home_terminal_code,
--     access_level,
--     is_active
-- )
-- values (
--     'dan',
--     'dan',
--     '',
--     'Dan Rosengrant',
--     '17713',
--     'CON',
--     4,
--     true
-- )
-- on conflict (employee_number) do update set
--     windows_identity = excluded.windows_identity,
--     windows_username = excluded.windows_username,
--     windows_domain = excluded.windows_domain,
--     display_name = excluded.display_name,
--     home_terminal_code = excluded.home_terminal_code,
--     access_level = excluded.access_level,
--     is_active = true,
--     updated_at = now();
--
-- insert into pc_employee_terminal_access (employee_id, terminal_code)
-- select id, 'CON'
-- from pc_employees
-- where employee_number = '17713'
-- on conflict do nothing;
--
-- delete from pc_employee_module_access
-- where employee_id = (
--     select id from pc_employees where employee_number = '17713'
-- );
--
-- insert into pc_employee_module_access (
--     employee_id,
--     module_code,
--     permission_level
-- )
-- select
--     e.id,
--     m.module_code,
--     'Admin'
-- from pc_employees e
-- cross join pc_modules m
-- where e.employee_number = '17713'
--   and m.module_code in ('DockCommander', 'Billing', 'RPS', 'Dispatch', 'RouteGrid', 'Admin')
-- on conflict do nothing;

-- ============================================================
-- Done.
-- ============================================================


-- Username-only login note: from whoami gaming\dan, use windows_username = 'dan'.
