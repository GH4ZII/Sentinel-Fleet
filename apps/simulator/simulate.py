"""
Sentinel Fleet GPS telemetry simulator.

Bootstraps >=20 assets+devices via JWT (optional), then streams position
events to POST /api/v1/telemetry/events with X-Api-Key.

Week 4: SCENARIO=geofence_exit creates an Allowed geofence around the first
vehicle, links it, then drives that vehicle out of the fence to trigger a detection.
"""

from __future__ import annotations

import argparse
import json
import math
import os
import random
import time
import uuid
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import httpx

DEFAULT_API_BASE = os.environ.get("API_BASE", "http://localhost:8081")
DEFAULT_EMAIL = os.environ.get("SF_EMAIL", "simulator@sentinel.local")
DEFAULT_PASSWORD = os.environ.get("SF_PASSWORD", "Simulator123!")
DEFAULT_ORG = os.environ.get("SF_ORG_NAME", "Simulator Fleet AS")
DEVICES_FILE = Path(__file__).resolve().parent / "devices.json"
SCENARIO_FILE = Path(__file__).resolve().parent / "scenario.json"
VEHICLE_COUNT = int(os.environ.get("SF_VEHICLE_COUNT", "20"))
INTERVAL_SEC = float(os.environ.get("SF_INTERVAL_SEC", "3"))
DEFAULT_SCENARIO = os.environ.get("SCENARIO", "geofence_exit")

# Oslo area center
OSLO_LAT = 59.9139
OSLO_LON = 10.7522

# ~1.1 km square around depot (~0.01 deg lat / lon at Oslo)
FENCE_HALF_DEG = 0.008


@dataclass
class SimulatedVehicle:
    name: str
    asset_id: str
    device_id: str
    api_key: str
    lat: float
    lon: float
    heading: float
    speed_kph: float
    odometer_km: float
    fuel_percent: float
    scenario_role: str = "wander"  # wander | exit


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def move(vehicle: SimulatedVehicle, dt_sec: float) -> None:
    vehicle.heading = (vehicle.heading + random.uniform(-25, 25)) % 360
    vehicle.speed_kph = max(5.0, min(70.0, vehicle.speed_kph + random.uniform(-8, 8)))
    distance_km = vehicle.speed_kph * (dt_sec / 3600.0)
    heading_rad = math.radians(vehicle.heading)
    dlat = (distance_km / 111.0) * math.cos(heading_rad)
    dlon = (distance_km / (111.0 * math.cos(math.radians(vehicle.lat)))) * math.sin(heading_rad)
    vehicle.lat += dlat
    vehicle.lon += dlon
    vehicle.odometer_km += distance_km
    vehicle.fuel_percent = max(5.0, vehicle.fuel_percent - distance_km * 0.08)


def move_exit_scenario(vehicle: SimulatedVehicle, tick: int, dt_sec: float, depot_lat: float, depot_lon: float) -> None:
    """Stay near depot for a few ticks, then drive east out of the geofence."""
    if tick < 3:
        # Jitter inside the fence
        vehicle.lat = depot_lat + random.uniform(-0.001, 0.001)
        vehicle.lon = depot_lon + random.uniform(-0.001, 0.001)
        vehicle.heading = 90.0
        vehicle.speed_kph = 15.0
        return

    vehicle.heading = 90.0
    vehicle.speed_kph = 55.0
    distance_km = vehicle.speed_kph * (dt_sec / 3600.0)
    dlon = distance_km / (111.0 * math.cos(math.radians(vehicle.lat)))
    vehicle.lon += dlon
    vehicle.odometer_km += distance_km
    vehicle.fuel_percent = max(5.0, vehicle.fuel_percent - distance_km * 0.08)


def auth_register_or_login(client: httpx.Client, email: str, password: str, org: str) -> str:
    login = client.post(
        "/api/v1/auth/login",
        json={"email": email, "password": password},
    )
    if login.status_code == 200:
        return login.json()["accessToken"]

    register = client.post(
        "/api/v1/auth/register",
        json={
            "email": email,
            "password": password,
            "firstName": "Sim",
            "lastName": "Driver",
            "organizationName": org,
        },
    )
    if register.status_code in (200, 201):
        return register.json()["accessToken"]

    login = client.post(
        "/api/v1/auth/login",
        json={"email": email, "password": password},
    )
    login.raise_for_status()
    return login.json()["accessToken"]


def load_devices() -> list[dict[str, Any]]:
    if not DEVICES_FILE.exists():
        return []
    return json.loads(DEVICES_FILE.read_text(encoding="utf-8"))


def save_devices(devices: list[dict[str, Any]]) -> None:
    DEVICES_FILE.write_text(json.dumps(devices, indent=2), encoding="utf-8")


def bootstrap_vehicles(client: httpx.Client, token: str, count: int) -> list[SimulatedVehicle]:
    headers = {"Authorization": f"Bearer {token}"}
    existing = load_devices()
    vehicles: list[SimulatedVehicle] = []

    for row in existing:
        vehicles.append(
            SimulatedVehicle(
                name=row["name"],
                asset_id=row["assetId"],
                device_id=row.get("deviceId", ""),
                api_key=row["apiKey"],
                lat=float(row.get("lat", OSLO_LAT + random.uniform(-0.04, 0.04))),
                lon=float(row.get("lon", OSLO_LON + random.uniform(-0.06, 0.06))),
                heading=float(row.get("heading", random.uniform(0, 360))),
                speed_kph=float(row.get("speedKph", random.uniform(20, 50))),
                odometer_km=float(row.get("odometerKm", random.uniform(1000, 90000))),
                fuel_percent=float(row.get("fuelPercent", random.uniform(40, 90))),
                scenario_role=row.get("scenarioRole", "wander"),
            )
        )

    need = max(0, count - len(vehicles))
    created_rows = list(existing)

    for i in range(need):
        idx = len(vehicles) + 1
        name = f"Sim Vehicle {idx:02d}"
        create = client.post(
            "/api/v1/assets",
            headers=headers,
            json={
                "name": name,
                "registrationNumber": f"SIM{idx:04d}",
                "manufacturer": "Simulator",
                "model": "V1",
                "createDevice": True,
            },
        )
        create.raise_for_status()
        body = create.json()
        asset = body["asset"]
        api_key = body.get("deviceApiKey")
        if not api_key:
            raise RuntimeError(f"Asset {name} created without deviceApiKey")

        lat = OSLO_LAT + random.uniform(-0.05, 0.05)
        lon = OSLO_LON + random.uniform(-0.08, 0.08)
        vehicle = SimulatedVehicle(
            name=name,
            asset_id=asset["id"],
            device_id="",
            api_key=api_key,
            lat=lat,
            lon=lon,
            heading=random.uniform(0, 360),
            speed_kph=random.uniform(20, 50),
            odometer_km=random.uniform(1000, 90000),
            fuel_percent=random.uniform(40, 90),
        )
        vehicles.append(vehicle)
        created_rows.append(
            {
                "name": name,
                "assetId": asset["id"],
                "apiKey": api_key,
                "lat": lat,
                "lon": lon,
                "heading": vehicle.heading,
                "speedKph": vehicle.speed_kph,
                "odometerKm": vehicle.odometer_km,
                "fuelPercent": vehicle.fuel_percent,
                "scenarioRole": "wander",
            }
        )
        print(f"Created {name} ({asset['id']})")

    if need:
        save_devices(created_rows)

    return vehicles[:count]


def ensure_default_rules(client: httpx.Client, token: str) -> None:
    headers = {"Authorization": f"Bearer {token}"}
    response = client.get("/api/v1/rules", headers=headers)
    response.raise_for_status()
    print(f"Detection rules ready ({len(response.json())})")


def setup_geofence_exit_scenario(
    client: httpx.Client,
    token: str,
    vehicles: list[SimulatedVehicle],
) -> tuple[SimulatedVehicle, float, float]:
    headers = {"Authorization": f"Bearer {token}"}
    ensure_default_rules(client, token)

    exit_vehicle = vehicles[0]
    depot_lat = OSLO_LAT
    depot_lon = OSLO_LON
    exit_vehicle.lat = depot_lat
    exit_vehicle.lon = depot_lon
    exit_vehicle.heading = 90.0
    exit_vehicle.speed_kph = 20.0
    exit_vehicle.scenario_role = "exit"

    scenario: dict[str, Any] = {}
    if SCENARIO_FILE.exists():
        scenario = json.loads(SCENARIO_FILE.read_text(encoding="utf-8"))

    geofence_id = scenario.get("geofenceId")
    if geofence_id:
        check = client.get(f"/api/v1/geofences/{geofence_id}", headers=headers)
        if check.status_code != 200:
            geofence_id = None

    if not geofence_id:
        half = FENCE_HALF_DEG
        coordinates = [
            {"longitude": depot_lon - half, "latitude": depot_lat - half},
            {"longitude": depot_lon + half, "latitude": depot_lat - half},
            {"longitude": depot_lon + half, "latitude": depot_lat + half},
            {"longitude": depot_lon - half, "latitude": depot_lat + half},
        ]
        create = client.post(
            "/api/v1/geofences",
            headers=headers,
            json={
                "name": "Lager Oslo",
                "description": "Simulator allowed depot for Week 4 geofence exit demo",
                "geofenceType": "Allowed",
                "coordinates": coordinates,
                "isActive": True,
            },
        )
        create.raise_for_status()
        geofence = create.json()
        geofence_id = geofence["id"]
        print(f"Created geofence Lager Oslo ({geofence_id})")

    link = client.post(
        f"/api/v1/geofences/{geofence_id}/assets",
        headers=headers,
        json={"assetId": exit_vehicle.asset_id, "ruleType": "Both"},
    )
    if link.status_code not in (200, 201):
        raise RuntimeError(f"Failed to link asset to geofence: {link.status_code} {link.text}")

    SCENARIO_FILE.write_text(
        json.dumps(
            {
                "scenario": "geofence_exit",
                "geofenceId": geofence_id,
                "assetId": exit_vehicle.asset_id,
                "depotLat": depot_lat,
                "depotLon": depot_lon,
            },
            indent=2,
        ),
        encoding="utf-8",
    )
    print(
        f"Geofence exit scenario: {exit_vehicle.name} starts inside Lager Oslo, "
        "then drives east out of the fence."
    )
    return exit_vehicle, depot_lat, depot_lon


def send_position(client: httpx.Client, vehicle: SimulatedVehicle) -> None:
    payload = {
        "eventId": str(uuid.uuid4()),
        "recordedAt": utc_now_iso(),
        "eventType": "position",
        "schemaVersion": 1,
        "position": {
            "latitude": round(vehicle.lat, 6),
            "longitude": round(vehicle.lon, 6),
            "speedKph": round(vehicle.speed_kph, 1),
            "heading": round(vehicle.heading, 1),
        },
        "vehicle": {
            "ignitionOn": True,
            "odometerKm": round(vehicle.odometer_km, 1),
            "fuelLevelPercent": round(vehicle.fuel_percent, 1),
        },
        "driver": {"userId": None},
    }
    response = client.post(
        "/api/v1/telemetry/events",
        headers={"X-Api-Key": vehicle.api_key},
        json=payload,
    )
    if response.status_code not in (200, 202):
        raise RuntimeError(
            f"Ingest failed for {vehicle.name}: {response.status_code} {response.text}"
        )


def persist_state(vehicles: list[SimulatedVehicle]) -> None:
    rows = [
        {
            "name": v.name,
            "assetId": v.asset_id,
            "deviceId": v.device_id,
            "apiKey": v.api_key,
            "lat": v.lat,
            "lon": v.lon,
            "heading": v.heading,
            "speedKph": v.speed_kph,
            "odometerKm": v.odometer_km,
            "fuelPercent": v.fuel_percent,
            "scenarioRole": v.scenario_role,
        }
        for v in vehicles
    ]
    save_devices(rows)


def main() -> int:
    parser = argparse.ArgumentParser(description="Sentinel Fleet telemetry simulator")
    parser.add_argument("--api-base", default=DEFAULT_API_BASE)
    parser.add_argument("--email", default=DEFAULT_EMAIL)
    parser.add_argument("--password", default=DEFAULT_PASSWORD)
    parser.add_argument("--org", default=DEFAULT_ORG)
    parser.add_argument("--count", type=int, default=VEHICLE_COUNT)
    parser.add_argument("--interval", type=float, default=INTERVAL_SEC)
    parser.add_argument("--bootstrap-only", action="store_true")
    parser.add_argument(
        "--scenario",
        default=DEFAULT_SCENARIO,
        choices=["none", "geofence_exit"],
        help="Demo scenario (default: geofence_exit)",
    )
    args = parser.parse_args()

    print(f"API: {args.api_base}")
    print(f"Vehicles: {args.count}, interval: {args.interval}s, scenario: {args.scenario}")

    depot_lat = OSLO_LAT
    depot_lon = OSLO_LON

    with httpx.Client(base_url=args.api_base, timeout=30.0) as client:
        token = auth_register_or_login(client, args.email, args.password, args.org)
        vehicles = bootstrap_vehicles(client, token, args.count)
        print(f"Ready with {len(vehicles)} vehicles")

        if args.scenario == "geofence_exit":
            _, depot_lat, depot_lon = setup_geofence_exit_scenario(client, token, vehicles)

        if args.bootstrap_only:
            return 0

        tick = 0
        while True:
            started = time.time()
            for vehicle in vehicles:
                if vehicle.scenario_role == "exit":
                    move_exit_scenario(vehicle, tick, args.interval, depot_lat, depot_lon)
                else:
                    move(vehicle, args.interval)
                send_position(client, vehicle)
            tick += 1
            if tick % 10 == 0:
                persist_state(vehicles)
                exit_v = next((v for v in vehicles if v.scenario_role == "exit"), None)
                extra = ""
                if exit_v:
                    extra = f" exit={exit_v.name}@{exit_v.lat:.5f},{exit_v.lon:.5f}"
                print(f"tick={tick} sent={len(vehicles)} positions{extra}")
            elapsed = time.time() - started
            sleep_for = max(0.0, args.interval - elapsed)
            time.sleep(sleep_for)


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except KeyboardInterrupt:
        print("\nStopped.")
        raise SystemExit(0)
