-- ============================================================
-- Optional: Drop Pyle Century Tables
-- ============================================================
-- Only run this if you want to wipe the Pyle Century schema clean.
-- This destroys Pyle Century data.
-- ============================================================

drop trigger if exists trg_rps_notify_bucket_event on rps_bucket_events;
drop function if exists rps_notify_bucket_event();
drop function if exists pc_touch_row();
drop function if exists pc_dock_permission(text, text);
drop function if exists pc_can_open_module(text, text);
drop view if exists pc_employee_access_view;

drop table if exists rps_bucket_events cascade;
drop table if exists rps_run_stops cascade;
drop table if exists rps_runs cascade;
drop table if exists rps_freight_stops cascade;
drop table if exists rps_route_area_zipcodes cascade;
drop table if exists rps_route_areas cascade;

drop table if exists pc_dock_door_state cascade;
drop table if exists pc_dock_layouts cascade;

drop table if exists pc_menu_sessions cascade;
drop table if exists pc_employee_module_access cascade;
drop table if exists pc_employee_terminal_access cascade;
drop table if exists pc_employees cascade;
drop table if exists pc_access_level_definitions cascade;
drop table if exists pc_modules cascade;
drop table if exists pc_terminals cascade;


-- Username-only login note: from whoami gaming\dan, use windows_username = 'dan'.
