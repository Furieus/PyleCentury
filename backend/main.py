import os
import secrets
from datetime import datetime, timedelta, timezone
from typing import Optional, List, Any

from fastapi import FastAPI, HTTPException, WebSocket, WebSocketDisconnect
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field
from supabase import create_client, Client


SUPABASE_URL = os.getenv("SUPABASE_URL", "")
SUPABASE_SERVICE_ROLE_KEY = os.getenv("SUPABASE_SERVICE_ROLE_KEY", "")

if not SUPABASE_URL or not SUPABASE_SERVICE_ROLE_KEY:
    # Let app start so /health can explain the issue instead of crashing silently.
    supabase: Optional[Client] = None
else:
    supabase = create_client(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY)


app = FastAPI(title="Pyle Century Backend", version="0.1.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


def db() -> Client:
    if supabase is None:
        raise HTTPException(
            status_code=500,
            detail="Supabase is not configured. Set SUPABASE_URL and SUPABASE_SERVICE_ROLE_KEY in Render."
        )
    return supabase


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def access_level_permissions(access_level: int) -> dict:
    if access_level == 1:
        return {
            "allowedModules": ["DockCommander"],
            "dockHomePermission": "read_write",
            "dockOtherPermission": "read_only",
            "billingPermission": "none",
            "adminPermission": "none",
            "permissionLevel": "Level 1"
        }

    if access_level == 2:
        return {
            "allowedModules": ["DockCommander"],
            "dockHomePermission": "read_write",
            "dockOtherPermission": "read_write",
            "billingPermission": "none",
            "adminPermission": "none",
            "permissionLevel": "Level 2"
        }

    if access_level == 3:
        return {
            "allowedModules": ["DockCommander", "Billing"],
            "dockHomePermission": "read_only",
            "dockOtherPermission": "read_only",
            "billingPermission": "read_write",
            "adminPermission": "none",
            "permissionLevel": "Level 3"
        }

    return {
        "allowedModules": ["DockCommander", "Billing", "RPS", "Dispatch", "RouteGrid", "Admin"],
        "dockHomePermission": "admin",
        "dockOtherPermission": "admin",
        "billingPermission": "admin",
        "adminPermission": "admin",
        "permissionLevel": "Level 4"
    }


def can_open_module(access_level: int, module_code: str) -> bool:
    module_code = module_code.lower()

    if access_level == 1:
        return module_code in ["dockcommander"]

    if access_level == 2:
        return module_code in ["dockcommander"]

    if access_level == 3:
        return module_code in ["dockcommander", "billing"]

    if access_level == 4:
        return True

    return False


class EmployeeProfileResponse(BaseModel):
    windowsIdentity: Optional[str] = None
    windowsUserName: str
    windowsDomain: Optional[str] = None
    displayName: str
    employeeNumber: str
    homeTerminalCode: str
    allowedModules: List[str]
    allowedTerminals: List[str]
    permissionLevel: str
    accessLevel: int
    dockHomePermission: str
    dockOtherPermission: str
    billingPermission: str
    adminPermission: str


class CreateLaunchSessionRequest(BaseModel):
    windowsIdentity: Optional[str] = ""
    employeeNumber: str
    moduleCode: str
    terminalCode: str


class CreateLaunchSessionResponse(BaseModel):
    sessionToken: str


class ValidateLaunchRequest(BaseModel):
    sessionToken: str
    windowsIdentity: Optional[str] = ""
    moduleCode: str


class ValidateLaunchResponse(BaseModel):
    valid: bool
    employeeNumber: str
    displayName: str
    homeTerminalCode: str
    accessLevel: int
    permissionLevel: str


class AdminUserRow(BaseModel):
    id: str
    windowsIdentity: Optional[str] = None
    windowsUsername: str
    windowsDomain: Optional[str] = None
    displayName: str
    employeeNumber: str
    homeTerminalCode: str
    accessLevel: int
    isActive: bool


class AdminUpsertUserRequest(BaseModel):
    id: Optional[str] = None
    windowsIdentity: Optional[str] = None
    windowsUsername: str
    employeeNumber: str
    displayName: str
    homeTerminalCode: str
    accessLevel: int
    isActive: bool = True


def employee_profile(row: dict) -> EmployeeProfileResponse:
    access_level = int(row.get("access_level") or 1)
    perms = access_level_permissions(access_level)

    terminal_rows = (
        db().table("pc_employee_terminal_access")
        .select("terminal_code")
        .eq("employee_id", row["id"])
        .execute()
        .data
        or []
    )

    allowed_terminals = [r["terminal_code"] for r in terminal_rows]
    if row["home_terminal_code"] not in allowed_terminals:
        allowed_terminals.insert(0, row["home_terminal_code"])

    return EmployeeProfileResponse(
        windowsIdentity=row.get("windows_identity"),
        windowsUserName=row.get("windows_username") or "",
        windowsDomain=row.get("windows_domain"),
        displayName=row["display_name"],
        employeeNumber=row["employee_number"],
        homeTerminalCode=row["home_terminal_code"],
        allowedModules=perms["allowedModules"],
        allowedTerminals=allowed_terminals,
        permissionLevel=perms["permissionLevel"],
        accessLevel=access_level,
        dockHomePermission=perms["dockHomePermission"],
        dockOtherPermission=perms["dockOtherPermission"],
        billingPermission=perms["billingPermission"],
        adminPermission=perms["adminPermission"],
    )


def admin_user_row(row: dict) -> AdminUserRow:
    return AdminUserRow(
        id=row["id"],
        windowsIdentity=row.get("windows_identity"),
        windowsUsername=row["windows_username"],
        windowsDomain=row.get("windows_domain"),
        displayName=row["display_name"],
        employeeNumber=row["employee_number"],
        homeTerminalCode=row["home_terminal_code"],
        accessLevel=int(row.get("access_level") or 1),
        isActive=bool(row.get("is_active"))
    )


@app.get("/health")
def health():
    return {
        "ok": supabase is not None,
        "service": "Pyle Century Backend",
        "supabaseConfigured": supabase is not None,
        "time": utc_now_iso()
    }


@app.get("/auth/windows-profile", response_model=EmployeeProfileResponse)
def windows_profile(windows_username: str, windows_identity: str = ""):
    # Username-only login.
    # Example: whoami = gaming\dan -> windows_username = dan
    username = windows_username.strip().lower()

    rows = (
        db().table("pc_employees")
        .select("*")
        .ilike("windows_username", username)
        .eq("is_active", True)
        .limit(1)
        .execute()
        .data
        or []
    )

    if not rows:
        raise HTTPException(
            status_code=404,
            detail=f"No active employee profile found for Windows username: {username}"
        )

    return employee_profile(rows[0])


@app.post("/auth/menu-session", response_model=CreateLaunchSessionResponse)
def create_menu_session(req: CreateLaunchSessionRequest):
    employee_rows = (
        db().table("pc_employees")
        .select("*")
        .eq("employee_number", req.employeeNumber)
        .eq("is_active", True)
        .limit(1)
        .execute()
        .data
        or []
    )

    if not employee_rows:
        raise HTTPException(status_code=404, detail="Employee not found or inactive.")

    employee = employee_rows[0]
    access_level = int(employee.get("access_level") or 1)

    if not can_open_module(access_level, req.moduleCode):
        raise HTTPException(status_code=403, detail="Employee is not authorized for this module.")

    token = secrets.token_urlsafe(32)
    expires_at = datetime.now(timezone.utc) + timedelta(minutes=30)

    db().table("pc_menu_sessions").insert({
        "session_token": token,
        "employee_id": employee["id"],
        "windows_identity": req.windowsIdentity or employee.get("windows_username") or "",
        "module_code": req.moduleCode,
        "terminal_code": req.terminalCode,
        "expires_at": expires_at.isoformat()
    }).execute()

    return CreateLaunchSessionResponse(sessionToken=token)


@app.post("/auth/validate-launch", response_model=ValidateLaunchResponse)
def validate_launch(req: ValidateLaunchRequest):
    rows = (
        db().table("pc_menu_sessions")
        .select("*, pc_employees(*)")
        .eq("session_token", req.sessionToken)
        .eq("module_code", req.moduleCode)
        .limit(1)
        .execute()
        .data
        or []
    )

    if not rows:
        raise HTTPException(status_code=404, detail="Launch session not found.")

    session = rows[0]
    expires_at = datetime.fromisoformat(session["expires_at"].replace("Z", "+00:00"))

    if expires_at < datetime.now(timezone.utc):
        raise HTTPException(status_code=403, detail="Launch session expired.")

    employee = session.get("pc_employees") or {}
    access_level = int(employee.get("access_level") or 1)
    perms = access_level_permissions(access_level)

    db().table("pc_menu_sessions").update({
        "used_at": utc_now_iso()
    }).eq("session_token", req.sessionToken).execute()

    return ValidateLaunchResponse(
        valid=True,
        employeeNumber=employee["employee_number"],
        displayName=employee["display_name"],
        homeTerminalCode=employee["home_terminal_code"],
        accessLevel=access_level,
        permissionLevel=perms["permissionLevel"]
    )


@app.get("/admin/users", response_model=List[AdminUserRow])
def admin_list_users(query: str = "", access_level: Optional[int] = None):
    q = db().table("pc_employees").select("*").order("display_name")

    if access_level:
        q = q.eq("access_level", access_level)

    rows = q.execute().data or []

    if query:
        ql = query.lower()
        rows = [
            r for r in rows
            if ql in (r.get("display_name") or "").lower()
            or ql in (r.get("employee_number") or "").lower()
            or ql in (r.get("windows_username") or "").lower()
            or ql in (r.get("windows_identity") or "").lower()
        ]

    return [admin_user_row(r) for r in rows]


@app.post("/admin/users", response_model=AdminUserRow)
def admin_upsert_user(req: AdminUpsertUserRequest):
    if req.accessLevel not in [1, 2, 3, 4]:
        raise HTTPException(status_code=400, detail="Access level must be 1, 2, 3, or 4.")

    username = req.windowsUsername.strip().lower()
    if not username:
        raise HTTPException(status_code=400, detail="Windows username is required.")

    terminal = (
        db().table("pc_terminals")
        .select("*")
        .eq("terminal_code", req.homeTerminalCode)
        .limit(1)
        .execute()
        .data
        or []
    )

    if not terminal:
        raise HTTPException(status_code=400, detail=f"Unknown terminal: {req.homeTerminalCode}")

    windows_identity = req.windowsIdentity.strip() if req.windowsIdentity else None
    windows_domain = None

    if windows_identity and "\\" in windows_identity:
        windows_domain = windows_identity.split("\\", 1)[0]

    payload = {
        "windows_identity": windows_identity,
        "windows_username": username,
        "windows_domain": windows_domain,
        "display_name": req.displayName,
        "employee_number": req.employeeNumber,
        "home_terminal_code": req.homeTerminalCode,
        "access_level": req.accessLevel,
        "is_active": req.isActive,
        "updated_at": utc_now_iso()
    }

    if req.id:
        db().table("pc_employees").update(payload).eq("id", req.id).execute()
        employee_id = req.id
    else:
        existing = (
            db().table("pc_employees")
            .select("*")
            .eq("employee_number", req.employeeNumber)
            .limit(1)
            .execute()
            .data
            or []
        )

        if existing:
            employee_id = existing[0]["id"]
            db().table("pc_employees").update(payload).eq("id", employee_id).execute()
        else:
            created = db().table("pc_employees").insert(payload).execute().data or []
            employee_id = created[0]["id"]

    db().table("pc_employee_terminal_access").upsert({
        "employee_id": employee_id,
        "terminal_code": req.homeTerminalCode
    }).execute()

    db().table("pc_employee_module_access").delete().eq("employee_id", employee_id).execute()

    modules: list[tuple[str, str]] = []
    if req.accessLevel in [1, 2]:
        modules = [("DockCommander", "User")]
    elif req.accessLevel == 3:
        modules = [("DockCommander", "ReadOnly"), ("Billing", "User")]
    elif req.accessLevel == 4:
        modules = [
            ("DockCommander", "Admin"),
            ("Billing", "Admin"),
            ("RPS", "Admin"),
            ("Dispatch", "Admin"),
            ("RouteGrid", "Admin"),
            ("Admin", "Admin")
        ]

    for module_code, perm in modules:
        db().table("pc_employee_module_access").insert({
            "employee_id": employee_id,
            "module_code": module_code,
            "permission_level": perm
        }).execute()

    row = db().table("pc_employees").select("*").eq("id", employee_id).limit(1).execute().data[0]
    return admin_user_row(row)


@app.delete("/admin/users/{employee_id}")
def admin_delete_user(employee_id: str):
    existing = db().table("pc_employees").select("*").eq("id", employee_id).limit(1).execute().data or []
    if not existing:
        raise HTTPException(status_code=404, detail="Employee not found.")

    db().table("pc_employee_module_access").delete().eq("employee_id", employee_id).execute()
    db().table("pc_employee_terminal_access").delete().eq("employee_id", employee_id).execute()
    db().table("pc_menu_sessions").delete().eq("employee_id", employee_id).execute()
    db().table("pc_employees").delete().eq("id", employee_id).execute()

    return {"deleted": True, "employeeId": employee_id}


@app.get("/dock/layouts/{terminal_code}")
def get_dock_layout(terminal_code: str):
    rows = (
        db().table("pc_dock_layouts")
        .select("*")
        .eq("terminal_code", terminal_code.upper())
        .eq("layout_name", "default")
        .eq("is_active", True)
        .limit(1)
        .execute()
        .data
        or []
    )

    if not rows:
        raise HTTPException(status_code=404, detail="No dock layout map found for this terminal.")

    return rows[0]["layout_json"]


@app.get("/rps/terminals/{terminal_code}/buckets")
def get_rps_buckets(terminal_code: str):
    rows = (
        db().table("rps_route_areas")
        .select("*")
        .eq("terminal_code", terminal_code.upper())
        .eq("is_active", True)
        .order("display_order")
        .execute()
        .data
        or []
    )
    return rows


@app.get("/rps/buckets/{terminal_code}/{route_area_id}")
def get_rps_bucket_snapshot(terminal_code: str, route_area_id: str):
    stops = (
        db().table("rps_freight_stops")
        .select("*")
        .eq("terminal_code", terminal_code.upper())
        .eq("route_area_id", route_area_id)
        .order("sort_order")
        .execute()
        .data
        or []
    )

    runs = (
        db().table("rps_runs")
        .select("*, rps_run_stops(*)")
        .eq("terminal_code", terminal_code.upper())
        .eq("route_area_id", route_area_id)
        .neq("run_status", "deleted")
        .order("created_at")
        .execute()
        .data
        or []
    )

    events = (
        db().table("rps_bucket_events")
        .select("bucket_sequence")
        .eq("terminal_code", terminal_code.upper())
        .eq("route_area_id", route_area_id)
        .order("bucket_sequence", desc=True)
        .limit(1)
        .execute()
        .data
        or []
    )

    latest_sequence = events[0]["bucket_sequence"] if events else 0

    return {
        "terminalCode": terminal_code.upper(),
        "routeAreaId": route_area_id,
        "latestBucketSequence": latest_sequence,
        "stops": stops,
        "runs": runs
    }




# ============================================================
# RPS Bucket Locks
# ============================================================

class RpsAcquireBucketLockRequest(BaseModel):
    employeeNumber: str
    clientId: Optional[str] = None
    force: bool = False


class RpsBucketLockResponse(BaseModel):
    routeAreaId: str
    terminalCode: str
    locked: bool
    readOnly: bool
    lockOwnerName: Optional[str] = None
    lockOwnerEmployeeNumber: Optional[str] = None
    lockOwnerWindowsUsername: Optional[str] = None
    expiresAt: Optional[str] = None
    lockId: Optional[str] = None
    message: Optional[str] = None


def _get_employee_by_number(employee_number: str) -> dict:
    rows = (
        db().table("pc_employees")
        .select("*")
        .eq("employee_number", employee_number)
        .eq("is_active", True)
        .limit(1)
        .execute()
        .data
        or []
    )

    if not rows:
        raise HTTPException(status_code=404, detail="Employee not found or inactive.")

    return rows[0]


def _active_bucket_lock(terminal_code: str, route_area_id: str) -> Optional[dict]:
    rows = (
        db().table("rps_bucket_locks")
        .select("*, pc_employees(employee_number)")
        .eq("terminal_code", terminal_code.upper())
        .eq("route_area_id", route_area_id)
        .gt("expires_at", utc_now_iso())
        .limit(1)
        .execute()
        .data
        or []
    )
    return rows[0] if rows else None


@app.get("/rps/buckets/{terminal_code}/locks")
def get_rps_bucket_locks(terminal_code: str):
    # Returns active locks for a terminal so the bucket dropdown can show 👁/lock icons.
    rows = (
        db().table("rps_active_bucket_locks")
        .select("*")
        .eq("terminal_code", terminal_code.upper())
        .execute()
        .data
        or []
    )
    return rows


@app.get("/rps/buckets/{terminal_code}/{route_area_id}/lock", response_model=RpsBucketLockResponse)
def get_rps_bucket_lock(terminal_code: str, route_area_id: str, employee_number: Optional[str] = None):
    lock = _active_bucket_lock(terminal_code, route_area_id)

    if not lock:
        return RpsBucketLockResponse(
            routeAreaId=route_area_id,
            terminalCode=terminal_code.upper(),
            locked=False,
            readOnly=False,
            message="Bucket is available."
        )

    owner_emp_number = None
    if isinstance(lock.get("pc_employees"), dict):
        owner_emp_number = lock["pc_employees"].get("employee_number")

    is_owner = employee_number and owner_emp_number == employee_number

    return RpsBucketLockResponse(
        routeAreaId=route_area_id,
        terminalCode=terminal_code.upper(),
        locked=True,
        readOnly=not is_owner,
        lockOwnerName=lock.get("locked_by_display_name"),
        lockOwnerEmployeeNumber=owner_emp_number,
        lockOwnerWindowsUsername=lock.get("locked_by_windows_username"),
        expiresAt=lock.get("expires_at"),
        lockId=lock.get("id"),
        message="You own this bucket lock." if is_owner else f"Bucket is currently being routed by {lock.get('locked_by_display_name')}."
    )


@app.post("/rps/buckets/{terminal_code}/{route_area_id}/lock", response_model=RpsBucketLockResponse)
def acquire_rps_bucket_lock(terminal_code: str, route_area_id: str, req: RpsAcquireBucketLockRequest):
    employee = _get_employee_by_number(req.employeeNumber)
    now = datetime.now(timezone.utc)
    expires = now + timedelta(minutes=2)

    existing = _active_bucket_lock(terminal_code, route_area_id)

    if existing:
        owner_emp_number = None
        if isinstance(existing.get("pc_employees"), dict):
            owner_emp_number = existing["pc_employees"].get("employee_number")

        if owner_emp_number != req.employeeNumber and not req.force:
            return RpsBucketLockResponse(
                routeAreaId=route_area_id,
                terminalCode=terminal_code.upper(),
                locked=True,
                readOnly=True,
                lockOwnerName=existing.get("locked_by_display_name"),
                lockOwnerEmployeeNumber=owner_emp_number,
                lockOwnerWindowsUsername=existing.get("locked_by_windows_username"),
                expiresAt=existing.get("expires_at"),
                lockId=existing.get("id"),
                message=f"Bucket is currently being routed by {existing.get('locked_by_display_name')}."
            )

    # Upsert lock. Unique key is terminal_code + route_area_id.
    payload = {
        "terminal_code": terminal_code.upper(),
        "route_area_id": route_area_id,
        "locked_by_employee_id": employee["id"],
        "locked_by_display_name": employee["display_name"],
        "locked_by_windows_username": employee.get("windows_username"),
        "lock_mode": "routing",
        "heartbeat_at": now.isoformat(),
        "expires_at": expires.isoformat(),
        "client_id": req.clientId
    }

    db().table("rps_bucket_locks").upsert(
        payload,
        on_conflict="terminal_code,route_area_id"
    ).execute()

    # Event for realtime clients watching the bucket list.
    db().table("rps_bucket_events").insert({
        "terminal_code": terminal_code.upper(),
        "route_area_id": route_area_id,
        "event_type": "bucket_locked",
        "entity_type": "bucket_lock",
        "payload": {
            "lockedBy": employee["display_name"],
            "employeeNumber": employee["employee_number"],
            "expiresAt": expires.isoformat()
        },
        "created_by_employee_id": employee["id"]
    }).execute()

    lock = _active_bucket_lock(terminal_code, route_area_id)

    return RpsBucketLockResponse(
        routeAreaId=route_area_id,
        terminalCode=terminal_code.upper(),
        locked=True,
        readOnly=False,
        lockOwnerName=employee["display_name"],
        lockOwnerEmployeeNumber=employee["employee_number"],
        lockOwnerWindowsUsername=employee.get("windows_username"),
        expiresAt=expires.isoformat(),
        lockId=lock.get("id") if lock else None,
        message="Bucket lock acquired."
    )


@app.post("/rps/buckets/{terminal_code}/{route_area_id}/lock/heartbeat", response_model=RpsBucketLockResponse)
def heartbeat_rps_bucket_lock(terminal_code: str, route_area_id: str, req: RpsAcquireBucketLockRequest):
    employee = _get_employee_by_number(req.employeeNumber)
    lock = _active_bucket_lock(terminal_code, route_area_id)

    if not lock:
        return acquire_rps_bucket_lock(terminal_code, route_area_id, req)

    if lock.get("locked_by_employee_id") != employee["id"]:
        return RpsBucketLockResponse(
            routeAreaId=route_area_id,
            terminalCode=terminal_code.upper(),
            locked=True,
            readOnly=True,
            lockOwnerName=lock.get("locked_by_display_name"),
            lockOwnerWindowsUsername=lock.get("locked_by_windows_username"),
            expiresAt=lock.get("expires_at"),
            lockId=lock.get("id"),
            message=f"Bucket is currently being routed by {lock.get('locked_by_display_name')}."
        )

    expires = datetime.now(timezone.utc) + timedelta(minutes=2)

    db().table("rps_bucket_locks").update({
        "heartbeat_at": utc_now_iso(),
        "expires_at": expires.isoformat()
    }).eq("id", lock["id"]).execute()

    return RpsBucketLockResponse(
        routeAreaId=route_area_id,
        terminalCode=terminal_code.upper(),
        locked=True,
        readOnly=False,
        lockOwnerName=employee["display_name"],
        lockOwnerEmployeeNumber=employee["employee_number"],
        lockOwnerWindowsUsername=employee.get("windows_username"),
        expiresAt=expires.isoformat(),
        lockId=lock["id"],
        message="Bucket lock heartbeat updated."
    )


@app.delete("/rps/buckets/{terminal_code}/{route_area_id}/lock")
def release_rps_bucket_lock(terminal_code: str, route_area_id: str, employee_number: str):
    employee = _get_employee_by_number(employee_number)
    lock = _active_bucket_lock(terminal_code, route_area_id)

    if not lock:
        return {"released": False, "message": "No active lock."}

    if lock.get("locked_by_employee_id") != employee["id"] and int(employee.get("access_level") or 1) != 4:
        raise HTTPException(status_code=403, detail="Only the lock owner or an admin can release this bucket lock.")

    db().table("rps_bucket_locks").delete().eq("id", lock["id"]).execute()

    db().table("rps_bucket_events").insert({
        "terminal_code": terminal_code.upper(),
        "route_area_id": route_area_id,
        "event_type": "bucket_unlocked",
        "entity_type": "bucket_lock",
        "payload": {
            "releasedBy": employee["display_name"],
            "employeeNumber": employee["employee_number"]
        },
        "created_by_employee_id": employee["id"]
    }).execute()

    return {"released": True, "message": "Bucket lock released."}



# Basic websocket placeholder for RPS desktop app.
# Next step: wire this to rps_bucket_events broadcasts.
active_rps_connections: dict[str, list[WebSocket]] = {}


@app.websocket("/ws/rps/{terminal_code}/{route_area_id}")
async def rps_ws(websocket: WebSocket, terminal_code: str, route_area_id: str):
    await websocket.accept()
    key = f"{terminal_code.upper()}:{route_area_id}"
    active_rps_connections.setdefault(key, []).append(websocket)

    try:
        await websocket.send_json({
            "type": "connected",
            "terminalCode": terminal_code.upper(),
            "routeAreaId": route_area_id,
            "message": "RPS realtime socket connected"
        })

        while True:
            # Keep connection alive and allow client ping messages.
            msg = await websocket.receive_text()
            if msg.lower() == "ping":
                await websocket.send_json({"type": "pong", "time": utc_now_iso()})
    except WebSocketDisconnect:
        pass
    finally:
        if key in active_rps_connections and websocket in active_rps_connections[key]:
            active_rps_connections[key].remove(websocket)
