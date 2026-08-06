FROM python:3.12-slim

WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY simulate.py .

ENV API_BASE=http://api:8080
ENV SF_VEHICLE_COUNT=20
ENV SF_INTERVAL_SEC=3

CMD ["python", "-u", "simulate.py"]
