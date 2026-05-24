-- Seed Dan as Level 4 using username-only login.
-- whoami shown: gaming\dan
-- Login key used by app: dan

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
    windows_identity = null,
    windows_username = 'dan',
    windows_domain = null,
    display_name = excluded.display_name,
    home_terminal_code = excluded.home_terminal_code,
    access_level = 4,
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
