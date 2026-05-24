# Pyle Century Clean Rebuild Schema

Run order:

1. Optional, only if you have not already removed old tables:

```text
optional_drop_pyle_century_tables.sql
```

2. Main rebuild:

```text
clean_rebuild_schema.sql
```

3. Seed Dan as Level 4 after editing the Windows login values:

```text
seed_dan_level4_template.sql
```

To get your Windows login:

```bat
whoami
```

The schema includes Dock Commander, employee access, and the foundation for program-based real-time RPS.

See:

```text
RPS_PROGRAM_REALTIME_PLAN.md
```


## Username-only login

For `whoami` value `gaming\dan`, use `windows_username = dan`. The full `gaming\dan` value is optional for now.
