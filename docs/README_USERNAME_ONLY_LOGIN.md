# Username-Only Login

For now, Pyle Century uses only the username part of `whoami`.

Example from your PC:

```text
gaming\dan
```

The app looks up:

```text
dan
```

not:

```text
gaming\dan
```

## Database

Use:

```text
pc_employees.windows_username = dan
```

`windows_identity` is optional/background info and does not need to be the login key.

## Migration for existing DB

Run:

```text
backend-render-update/supabase_username_only_login_migration.sql
```

## Seed Dan as Level 4

Run:

```text
backend-render-update/seed_dan_username_only_level4.sql
```

## Backend

Add/update the Render endpoint using:

```text
backend-render-update/main_py_username_only_login_patch.txt
```

The endpoint should resolve:

```text
GET /auth/windows-profile?windows_username=dan
```

## Employee Manager

When creating users, the important field is:

```text
Windows Username
```

For `gaming\dan`, enter:

```text
dan
```

The full Windows Identity field can be left blank for now.
