# Render Setup

## 1. Push this repo to GitHub

git add .
git commit -m "Clean Render-first Customer Account API"
git push

## 2. Create Supabase tables

Open:
database/supabase/schema.sql

Paste into:
Supabase > SQL Editor > New Query > Run

## 3. Create Render Web Service

Render > New > Web Service

Connect this GitHub repo.

Use:

Root Directory:
backend/customer-account-api

Build Command:
pip install -r requirements.txt

Start Command:
uvicorn main:app --host 0.0.0.0 --port $PORT

## 4. Add Environment Variables in Render

SUPABASE_URL
SUPABASE_SERVICE_ROLE_KEY

Do not put those in GitHub.

## 5. Test

Open:

https://YOUR-RENDER-URL.onrender.com/

Then:

https://YOUR-RENDER-URL.onrender.com/docs
