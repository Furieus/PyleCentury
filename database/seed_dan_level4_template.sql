-- ============================================================
-- Pyle Century Seed Dan Level 4
-- ============================================================
-- Run this AFTER clean_rebuild_schema.sql.
--
-- First run this on your Windows PC:
--     whoami
--
-- Example:
--     DESKTOP-12345\dan
--
-- Replace the three values below before running.
-- ============================================================

insert into pc_employees (
    windows_identity,
    windows_username,
    windows_domain,
    display_name,
    employee_number,
    home_terminal_code,
    access_level,
    is_active
)
values (
    null,
    'dan',
    null,
    'Dan Rosengrant',
    '17713',
    'CON',
    4,
    true
)
on conflict (employee_number) do update set
    windows_identity = excluded.windows_identity,
    windows_username = excluded.windows_username,
    windows_domain = excluded.windows_domain,
    display_name = excluded.display_name,
    home_terminal_code = excluded.home_terminal_code,
    access_level = excluded.access_level,
    is_active = true,
    updated_at = now();

insert into pc_employee_terminal_access (employee_id, terminal_code)
select id, 'CON'
from pc_employees
where employee_number = '17713'
on conflict do nothing;

delete from pc_employee_module_access
where employee_id = (
    select id from pc_employees where employee_number = '17713'
);

insert into pc_employee_module_access (
    employee_id,
    module_code,
    permission_level
)
select
    e.id,
    m.module_code,
    'Admin'
from pc_employees e
cross join pc_modules m
where e.employee_number = '17713'
  and m.module_code in ('DockCommander', 'Billing', 'RPS', 'Dispatch', 'RouteGrid', 'Admin')
on conflict do nothing;

select *
from pc_employee_access_view
where employee_number = '17713';


-- Username-only login note: from whoami gaming\dan, use windows_username = 'dan'.
