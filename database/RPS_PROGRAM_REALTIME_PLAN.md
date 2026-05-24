# RPS Program-Based Real-Time Plan

## Goal

Move RPS from web mockup to a real Windows program while keeping all users synchronized.

Example:

```text
Two users are in CON → WALLINGFORD.
User A drags a stop from the left bucket into a run on the right.
User B sees that stop move immediately.
```

## Recommended architecture

```text
WPF RPS app
    ↓ REST mutations
Render FastAPI backend
    ↓ write transaction
Supabase Postgres
    ↓ event row / NOTIFY
Render WebSocket broadcast
    ↓ live update
Other WPF RPS apps watching the same bucket
```

## Why not local JSON

No production data should live in local JSON.

The app may have appsettings.json for config only:

```json
{
  "ApiBaseUrl": "https://PyleCentury.onrender.com"
}
```

But live RPS freight, runs, stops, route grids, and users live in Postgres.

## RPS tables in the schema

```text
rps_route_areas
rps_route_area_zipcodes
rps_freight_stops
rps_runs
rps_run_stops
rps_bucket_events
```

## Real-time event flow

### 1. User opens a bucket

```text
GET /rps/buckets/CON/WALLINGFORD
```

The app receives:

```text
- route area info
- unplanned freight stops
- active/clipped/saved runs
- latest bucket_sequence
```

### 2. App opens WebSocket

```text
/ws/rps/CON/<route_area_id>
```

### 3. User moves a stop

```text
POST /rps/stops/{stop_id}/move
```

Payload example:

```json
{
  "targetRunId": "uuid",
  "newSequence": 30.000,
  "expectedRowVersion": 4
}
```

### 4. Backend saves the move

Backend transaction:

```text
- update rps_freight_stops.planning_status
- insert/update rps_run_stops
- update row_version
- insert rps_bucket_events row
```

### 5. Backend broadcasts

Event example:

```json
{
  "eventType": "stop_moved",
  "terminalCode": "CON",
  "routeAreaId": "uuid",
  "entityType": "freight_stop",
  "entityId": "uuid",
  "bucketSequence": 1042
}
```

### 6. Other clients update

Other clients either:

```text
- apply the payload directly
```

or safer:

```text
- fetch the changed stop/run from REST
```

## Conflict handling

Every editable row has:

```text
row_version
updated_at
updated_by_employee_id
```

When a user saves a change, send the expected row_version.

If someone already changed it, backend returns:

```text
409 Conflict
```

Then the app reloads the bucket and shows a message like:

```text
This stop was changed by another user. The bucket has been refreshed.
```

## First program build scope

Start with:

```text
PyleCentury.RPS.Desktop
- terminal selector
- route/bucket selector
- left freight bucket
- right active run
- clipped run list
- drag stop to run
- drag stop back to bucket
- websocket live refresh
```

Then add:

```text
- save run to trailer/load
- route map
- smart warnings
- liftgate consolidation
- route sequencing
```


## Username-only login

For `whoami` value `gaming\dan`, use `windows_username = dan`. The full `gaming\dan` value is optional for now.
