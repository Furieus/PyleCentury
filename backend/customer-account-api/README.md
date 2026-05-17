# Pyle Century Customer Account API

Python FastAPI backend for customer account creation, lookup, restrictions, and hazmat profiles.

## Local setup

python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python -m uvicorn main:app --reload

## Environment variables

Create a .env file locally, but do not commit it.

SUPABASE_URL=your_supabase_url
SUPABASE_SERVICE_ROLE_KEY=your_service_role_key
