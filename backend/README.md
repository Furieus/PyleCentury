# Pyle Century Backend

Render settings:

```text
Root Directory: backend
Build Command:  pip install -r requirements.txt
Start Command:  uvicorn main:app --host 0.0.0.0 --port $PORT
```

Environment variables:

```text
SUPABASE_URL
SUPABASE_SERVICE_ROLE_KEY
```

Test after deploy:

```text
https://PyleCentury.onrender.com/health
https://PyleCentury.onrender.com/auth/windows-profile?windows_username=dan
```
