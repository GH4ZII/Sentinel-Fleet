-- Enable PostGIS for geospatial queries (geofences, proximity, route analysis)
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS postgis_topology;
