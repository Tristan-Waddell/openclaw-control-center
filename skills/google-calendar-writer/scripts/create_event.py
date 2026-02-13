#!/usr/bin/env python3
import argparse
import json
import os
import sys
import urllib.parse
import urllib.request

SECRETS_PATH = "/root/.openclaw/secrets/google_calendar_oauth.env"
TOKEN_URL = "https://oauth2.googleapis.com/token"


def load_env(path):
    data = {}
    with open(path, "r", encoding="utf-8") as f:
        for raw in f:
            line = raw.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            k, v = line.split("=", 1)
            data[k.strip()] = v.strip().strip('"').strip("'")
    return data


def post_json(url, payload, headers=None):
    body = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(url, data=body, method="POST")
    req.add_header("Content-Type", "application/json")
    if headers:
        for k, v in headers.items():
            req.add_header(k, v)
    with urllib.request.urlopen(req, timeout=30) as resp:
        return json.loads(resp.read().decode("utf-8"))


def get_access_token(cfg):
    form = urllib.parse.urlencode(
        {
            "client_id": cfg["GOOGLE_CLIENT_ID"],
            "client_secret": cfg["GOOGLE_CLIENT_SECRET"],
            "refresh_token": cfg["GOOGLE_REFRESH_TOKEN"],
            "grant_type": "refresh_token",
        }
    ).encode("utf-8")
    req = urllib.request.Request(TOKEN_URL, data=form, method="POST")
    req.add_header("Content-Type", "application/x-www-form-urlencoded")
    with urllib.request.urlopen(req, timeout=30) as resp:
        j = json.loads(resp.read().decode("utf-8"))
    tok = j.get("access_token")
    if not tok:
        raise RuntimeError(f"No access_token in token response: {j}")
    return tok


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--title", required=True)
    ap.add_argument("--start", required=True, help="ISO datetime with offset")
    ap.add_argument("--end", required=True, help="ISO datetime with offset")
    ap.add_argument("--tz", default="America/New_York")
    ap.add_argument("--location", default="")
    ap.add_argument("--description", default="")
    ap.add_argument("--calendar-id", default="")
    ap.add_argument("--reminder-minutes", type=int, default=30)
    args = ap.parse_args()

    if not os.path.exists(SECRETS_PATH):
        raise SystemExit(f"Missing secrets file: {SECRETS_PATH}")

    cfg = load_env(SECRETS_PATH)
    for k in ("GOOGLE_CLIENT_ID", "GOOGLE_CLIENT_SECRET", "GOOGLE_REFRESH_TOKEN"):
        if not cfg.get(k):
            raise SystemExit(f"Missing required key in secrets: {k}")

    calendar_id = args.calendar_id or cfg.get("GOOGLE_CALENDAR_ID", "primary")
    token = get_access_token(cfg)

    event = {
        "summary": args.title,
        "location": args.location,
        "description": args.description,
        "start": {"dateTime": args.start, "timeZone": args.tz},
        "end": {"dateTime": args.end, "timeZone": args.tz},
        "reminders": {
            "useDefault": False,
            "overrides": [{"method": "popup", "minutes": args.reminder_minutes}],
        },
    }

    url = f"https://www.googleapis.com/calendar/v3/calendars/{urllib.parse.quote(calendar_id, safe='')}/events"
    res = post_json(url, event, headers={"Authorization": f"Bearer {token}"})
    print(json.dumps({"id": res.get("id"), "htmlLink": res.get("htmlLink")}, ensure_ascii=False))


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(f"ERROR: {e}", file=sys.stderr)
        sys.exit(1)
