import csv
import hashlib
import re
import time
from dataclasses import dataclass
from typing import Optional

import requests


OVERPASS_URLS = [
    "https://overpass-api.de/api/interpreter",
    "https://overpass.kumi.systems/api/interpreter",
    "https://overpass.openstreetmap.ru/api/interpreter",
]

OUTPUT_CSV = "customer_candidates_meriden_wallingford_northhaven.csv"

# Search centers. Radius is in meters.
SEARCH_AREAS = [
    {"city": "Meriden", "state": "CT", "lat": 41.5382, "lon": -72.8070, "radius": 3500},
    {"city": "Wallingford", "state": "CT", "lat": 41.4570, "lon": -72.8232, "radius": 3500},
    {"city": "North Haven", "state": "CT", "lat": 41.3909, "lon": -72.8595, "radius": 3500},
]

# Tune these depending on what you want to pull.
# This intentionally focuses on likely shipper/receiver/business locations,
# not every restaurant, school, park, etc.
OSM_TAG_FILTERS = [
    ('shop', None),
    ('office', None),
    ('industrial', None),
    ('warehouse', None),
]

EXCLUDED_KEYWORDS = [
    "school",
    "church",
    "cemetery",
    "playground",
    "park",
    "police",
    "fire station",
]


@dataclass
class CustomerCandidate:
    account_code: str
    business_name: str
    address1: str
    city: str
    state: str
    zip_code: str
    phone: str
    website: str
    latitude: float
    longitude: float
    source: str
    osm_type: str
    osm_id: str


def clean_text(value: Optional[str]) -> str:
    if not value:
        return ""
    return " ".join(str(value).replace("\n", " ").split()).strip()


def normalize_business_prefix(name: str) -> str:
    cleaned = re.sub(r"[^A-Z0-9]", "", name.upper())

    # Remove common business suffixes to make codes cleaner.
    for suffix in ["LLC", "INC", "CORP", "CO", "COMPANY", "THE"]:
        cleaned = cleaned.replace(suffix, "")

    if len(cleaned) >= 2:
        return cleaned[:2]

    return cleaned.ljust(2, "X")


def city_prefix(city: str) -> str:
    city = city.upper().replace(" ", "")
    if city.startswith("MERIDEN"):
        return "ME"
    if city.startswith("WALLINGFORD"):
        return "WA"
    if city.startswith("NORTHHAVEN"):
        return "NH"
    return city[:2].ljust(2, "X")


def stable_two_digit_number(name: str, address: str) -> str:
    # Stable number from name/address so reruns produce the same code most of the time.
    raw = f"{name}|{address}".encode("utf-8")
    digest = hashlib.sha1(raw).hexdigest()
    number = int(digest[:6], 16) % 99 + 1
    return f"{number:02d}"


def generate_account_code(name: str, city: str, address: str) -> str:
    # Example: ABME17 = ABC Supply in Meriden
    return f"{normalize_business_prefix(name)}{city_prefix(city)}{stable_two_digit_number(name, address)}"


def should_exclude(name: str) -> bool:
    lowered = name.lower()
    return any(keyword in lowered for keyword in EXCLUDED_KEYWORDS)


def build_overpass_query(lat: float, lon: float, radius: int) -> str:
    parts = []

    for key, value in OSM_TAG_FILTERS:
        if value is None:
            parts.append(f'node(around:{radius},{lat},{lon})["{key}"]["name"];')
            parts.append(f'way(around:{radius},{lat},{lon})["{key}"]["name"];')
            parts.append(f'relation(around:{radius},{lat},{lon})["{key}"]["name"];')
        else:
            parts.append(f'node(around:{radius},{lat},{lon})["{key}"="{value}"]["name"];')
            parts.append(f'way(around:{radius},{lat},{lon})["{key}"="{value}"]["name"];')
            parts.append(f'relation(around:{radius},{lat},{lon})["{key}"="{value}"]["name"];')

    joined = "\n".join(parts)

    return f"""
[out:json][timeout:60];
(
{joined}
);
out center tags;
"""


def get_address(tags: dict) -> tuple[str, str, str, str]:
    house_number = clean_text(tags.get("addr:housenumber"))
    street = clean_text(tags.get("addr:street"))
    city = clean_text(tags.get("addr:city"))
    state = clean_text(tags.get("addr:state"))
    zip_code = clean_text(tags.get("addr:postcode"))

    address1 = " ".join(part for part in [house_number, street] if part).strip()

    return address1, city, state, zip_code


def fetch_area(area: dict) -> list[CustomerCandidate]:
    query = build_overpass_query(area["lat"], area["lon"], area["radius"])

    last_error = None

    for url in OVERPASS_URLS:
        try:
            print(f"  Trying Overpass server: {url}")

            response = requests.post(
                url,
                data={"data": query},
                timeout=120,
                headers={
                    "User-Agent": "PyleCenturyCustomerCandidateBuilder/0.1"
                },
            )

            response.raise_for_status()
            data = response.json()

            candidates: list[CustomerCandidate] = []

            for element in data.get("elements", []):
                tags = element.get("tags", {})

                name = clean_text(tags.get("name"))
                if not name:
                    continue

                if should_exclude(name):
                    continue

                address1, tag_city, tag_state, zip_code = get_address(tags)

                city = tag_city or area["city"]
                state = tag_state or area["state"]

                lat = element.get("lat") or element.get("center", {}).get("lat")
                lon = element.get("lon") or element.get("center", {}).get("lon")

                if lat is None or lon is None:
                    continue

                account_code = generate_account_code(name, city, address1)

                candidates.append(
                    CustomerCandidate(
                        account_code=account_code,
                        business_name=name,
                        address1=address1,
                        city=city,
                        state=state,
                        zip_code=zip_code,
                        phone=clean_text(tags.get("phone") or tags.get("contact:phone")),
                        website=clean_text(tags.get("website") or tags.get("contact:website")),
                        latitude=float(lat),
                        longitude=float(lon),
                        source="OpenStreetMap",
                        osm_type=element.get("type", ""),
                        osm_id=str(element.get("id", "")),
                    )
                )

            return candidates

        except requests.RequestException as ex:
            last_error = ex
            print(f"  Failed on {url}: {ex}")
            print("  Trying next server...")
            time.sleep(3)

    print(f"  All Overpass servers failed for {area['city']}. Skipping this area.")
    print(f"  Last error: {last_error}")
    return []


def dedupe(candidates: list[CustomerCandidate]) -> list[CustomerCandidate]:
    seen = set()
    output = []

    for c in candidates:
        key = (
            c.business_name.lower(),
            c.address1.lower(),
            c.city.lower(),
            round(c.latitude, 5),
            round(c.longitude, 5),
        )

        if key in seen:
            continue

        seen.add(key)
        output.append(c)

    # If duplicate account codes occur, append a letter.
    code_counts = {}

    final = []
    for c in output:
        base_code = c.account_code
        count = code_counts.get(base_code, 0)
        code_counts[base_code] = count + 1

        if count == 0:
            final.append(c)
        else:
            suffix = chr(ord("A") + count - 1)
            final.append(
                CustomerCandidate(
                    account_code=f"{base_code}{suffix}",
                    business_name=c.business_name,
                    address1=c.address1,
                    city=c.city,
                    state=c.state,
                    zip_code=c.zip_code,
                    phone=c.phone,
                    website=c.website,
                    latitude=c.latitude,
                    longitude=c.longitude,
                    source=c.source,
                    osm_type=c.osm_type,
                    osm_id=c.osm_id,
                )
            )

    return final


def write_csv(candidates: list[CustomerCandidate]) -> None:
    fieldnames = [
        "account_code",
        "business_name",
        "address1",
        "city",
        "state",
        "zip_code",
        "phone",
        "website",
        "latitude",
        "longitude",
        "source",
        "osm_type",
        "osm_id",
        "requires_liftgate",
        "requires_straight_truck",
        "appointment_required",
        "limited_access",
        "call_before_delivery",
        "inside_delivery",
        "dock_available",
        "forklift_available",
        "pallet_jack_required",
        "is_hazmat",
        "un_number",
        "hazmat_class",
        "packing_group",
        "container_type",
        "proper_shipping_name",
        "placard_required",
        "review_status",
    ]

    with open(OUTPUT_CSV, "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()

        for c in candidates:
            writer.writerow(
                {
                    "account_code": c.account_code,
                    "business_name": c.business_name,
                    "address1": c.address1,
                    "city": c.city,
                    "state": c.state,
                    "zip_code": c.zip_code,
                    "phone": c.phone,
                    "website": c.website,
                    "latitude": c.latitude,
                    "longitude": c.longitude,
                    "source": c.source,
                    "osm_type": c.osm_type,
                    "osm_id": c.osm_id,

                    # Default blank/false fields for review before import.
                    "requires_liftgate": "false",
                    "requires_straight_truck": "false",
                    "appointment_required": "false",
                    "limited_access": "false",
                    "call_before_delivery": "false",
                    "inside_delivery": "false",
                    "dock_available": "false",
                    "forklift_available": "false",
                    "pallet_jack_required": "false",
                    "is_hazmat": "false",
                    "un_number": "",
                    "hazmat_class": "",
                    "packing_group": "",
                    "container_type": "",
                    "proper_shipping_name": "",
                    "placard_required": "false",
                    "review_status": "needs_review",
                }
            )


def main():
    all_candidates = []

    for area in SEARCH_AREAS:
        print(f"Fetching businesses near {area['city']}, {area['state']}...")
        candidates = fetch_area(area)
        print(f"  Found {len(candidates)} candidates.")
        all_candidates.extend(candidates)

        # Be nice to public APIs.
        time.sleep(2)

    final = dedupe(all_candidates)
    write_csv(final)

    print()
    print(f"Done. Wrote {len(final)} unique customer candidates to:")
    print(OUTPUT_CSV)
    print()
    print("Review the CSV before importing anything into Supabase.")


if __name__ == "__main__":
    main()