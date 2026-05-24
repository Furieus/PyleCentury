# Rebuild GitHub Root Steps

From your existing local GitHub folder:

```powershell
cd C:\Users\Dan\Desktop\PyleCentury

# Optional safety backup first:
Copy-Item . C:\Users\Dan\Desktop\PyleCentury_BACKUP -Recurse

# Delete everything except .git:
Get-ChildItem -Force | Where-Object { $_.Name -ne ".git" } | Remove-Item -Recurse -Force
```

Then extract the clean package contents into that folder.

Commit:

```powershell
git status
git add .
git commit -m "Rebuild Pyle Century clean root"
git push
```

Render settings:

```text
Root Directory: backend
Build Command: pip install -r requirements.txt
Start Command: uvicorn main:app --host 0.0.0.0 --port $PORT
```
