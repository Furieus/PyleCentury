-- ============================================================
-- Pyle Century RPS Bucket Locks
-- ============================================================
-- Purpose:
-- - Show when someone else is routing a bucket.
-- - Let other users open the bucket in read-only/view mode.
-- - Automatically expire stale locks.
--
-- Example:
-- Dan opens CON / WALLINGFORD for routing.
-- Another user sees a lock/eyes icon next to WALLINGFORD.
-- If they open it, they get read-only mode.
-- ============================================================

create table if not exists rps_bucket_locks (
    id uuid primary key default gen_random_uuid(),

    terminal_code text not null references pc_terminals(terminal_code),
    route_area_id uuid not null references rps_route_areas(id) on delete cascade,

    locked_by_employee_id uuid not null references pc_employees(id) on delete cascade,
    locked_by_display_name text not null,
    locked_by_windows_username text,

    -- routing = actively editing/routing this bucket.
    -- viewing = optional future state if we want to show watchers too.
    lock_mode text not null default 'routing',

    acquired_at timestamptz not null default now(),
    heartbeat_at timestamptz not null default now(),
    expires_at timestamptz not null default (now() + interval '2 minutes'),

    client_id text,
    notes text,

    unique(terminal_code, route_area_id)
);

create index if not exists ix_rps_bucket_locks_scope
on rps_bucket_locks(terminal_code, route_area_id);

create index if not exists ix_rps_bucket_locks_expires
on rps_bucket_locks(expires_at);

-- View showing active/non-expired bucket locks.
create or replace view rps_active_bucket_locks as
select
    l.id,
    l.terminal_code,
    l.route_area_id,
    ra.route_area_name,
    l.locked_by_employee_id,
    l.locked_by_display_name,
    l.locked_by_windows_username,
    l.lock_mode,
    l.acquired_at,
    l.heartbeat_at,
    l.expires_at,
    l.client_id
from rps_bucket_locks l
join rps_route_areas ra on ra.id = l.route_area_id
where l.expires_at > now();

-- Function to clean stale locks.
create or replace function rps_cleanup_expired_bucket_locks()
returns integer
language plpgsql
as $$
declare
    v_count integer;
begin
    delete from rps_bucket_locks
    where expires_at <= now();

    get diagnostics v_count = row_count;
    return v_count;
end;
$$;
