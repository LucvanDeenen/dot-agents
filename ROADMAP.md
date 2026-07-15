# Improvements
- Add this SSE endpoint to the OpenAPI spec so it is documented centrally.
- If you need multi-instance deployment, move broadcaster fan-out to Redis/pub-sub so events are shared across nodes.
- Optionally add a per-job stream endpoint (for example jobs/{id}/events) to reduce client-side filtering.