# Pyle Century Customer Account API

FastAPI backend for customer account creation, lookup, restrictions, and hazmat profiles.

## Render setup

Render settings:

Root Directory:
backend/customer-account-api

Build Command:
pip install -r requirements.txt

Start Command:
uvicorn main:app --host 0.0.0.0 --port $PORT

Environment Variables:
SUPABASE_URL
SUPABASE_SERVICE_ROLE_KEY

## Test URLs

After deployment:

/
returns service status.

/docs
opens FastAPI Swagger API testing page.

/health
returns a basic health check.
