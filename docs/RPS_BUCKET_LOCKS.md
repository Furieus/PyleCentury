# RPS Bucket Locks

## User behavior

When a user opens a route bucket for routing, the app attempts to acquire a bucket lock.

Example:

```text
CON → WALLINGFORD
```

If nobody else is routing the bucket:

```text
- Bucket opens in editable mode
- Dropdown shows this user as owner/routing
- App heartbeats the lock every ~30 seconds
```

If someone else is already routing it:

```text
- Dropdown shows 👁 or lock icon next to the bucket
- If another user opens it, they enter read-only mode
- They can view stops/runs but cannot move/remove/route stops
```

## Lock timeout

Locks expire after 2 minutes unless the app heartbeats them.

This protects against:

```text
- App crash
- User closing laptop
- Network drop
- User leaving bucket open and walking away
```

## Backend endpoints

```text
GET    /rps/buckets/{terminal_code}/locks
GET    /rps/buckets/{terminal_code}/{route_area_id}/lock
POST   /rps/buckets/{terminal_code}/{route_area_id}/lock
POST   /rps/buckets/{terminal_code}/{route_area_id}/lock/heartbeat
DELETE /rps/buckets/{terminal_code}/{route_area_id}/lock?employee_number=17713
```

## Database

```text
rps_bucket_locks
rps_active_bucket_locks
rps_cleanup_expired_bucket_locks()
```

## Desktop app logic

Route bucket dropdown should show:

```text
WALLINGFORD              normal
WATERBURY   👁 Dan       locked/view-only if opened
NEW HAVEN                normal
```

When read-only:

```text
- Disable drag/drop
- Disable move/remove buttons
- Disable save/clip/unclip buttons
- Show banner: Viewing only — currently being routed by Dan
```
