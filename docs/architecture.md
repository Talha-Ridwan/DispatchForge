# DispatchForge architecture: who touches which table

Tables and columns live in [`schema.dbml`](schema.dbml). This file shows the code around them.
Keep both in sync with the code, in the same commit as the change.

**Legend**

- Rectangle = service (runs per HTTP request). Rounded = background worker. Cylinder = table. Double-edged box = in-memory queue.
- Solid arrow = writes (and may read). Dashed arrow = reads only.
- Dashed border or "planned" in a label = not built yet. The phase number refers to the plan.

## 1. Admin API (control plane)

```mermaid
flowchart LR
    subgraph API["Admin API · JWT (register and login are open)"]
        direction TB
        US["<b>UserService</b><br/>register · login · delete"]
        TS["<b>TenantService</b><br/>create · get · list · update<br/>delete = mark for death"]
        ETS["<b>EventTypeService</b><br/>create: assigns a free bit 0–63 · list<br/>delete = mark for death + clear the bit"]
        DS["<b>DestinationService</b><br/>create: makes HMAC key, builds bitmask<br/>get · list · update · delete"]
    end

    Users[("Users")]
    Tenants[("Tenants")]
    EventTypes[("EventTypes")]
    Destinations[("Destinations")]
    Outbox[("DeliveryOutboxes")]

    US -->|"insert · delete · find by username"| Users
    TS -->|"insert · update · read"| Tenants
    ETS -->|"insert · update status · read"| EventTypes
    ETS -.->|"tenant exists?"| Tenants
    ETS -->|"clear the bit, same transaction"| Destinations
    DS -->|"insert · update · delete · read"| Destinations
    DS -.->|"name ↔ bit"| EventTypes
    Destinations -->|"FK cascade: delete drops pending deliveries"| Outbox
```

## 2. Ingest path (one request)

```mermaid
flowchart LR
    Sender(["Internal service<br/>POST /api/tenants/{id}/events"])
    ES["<b>EventService</b> · the waiter, per request<br/>route · hash key · build ticket<br/>wait for the buzz"]
    Q[["<b>IngestQueue</b> · the rail<br/>bounded channel, 10,000 tickets"]]
    IW("<b>IngestWriterService</b> · 2 cooks, background<br/>up to 500 tickets, one transaction<br/>buzz after commit → 202")

    subgraph Control["Control-plane tables"]
        Tenants[("Tenants")]
        EventTypes[("EventTypes")]
        Destinations[("Destinations")]
    end

    Idem[("IdempotencyRecords")]
    Events[("Events<br/>weekly partitions")]
    Outbox[("DeliveryOutboxes")]

    Sender --> ES
    ES -.->|"active?"| Tenants
    ES -.->|"find by name"| EventTypes
    ES -.->|"match bitmask"| Destinations
    ES -->|"enqueue ticket"| Q
    Q --> IW
    IW -->|"insert ON CONFLICT DO NOTHING"| Idem
    IW -->|"insert winners"| Events
    IW -->|"insert 1 row per destination"| Outbox
```

## 3. Background workers

```mermaid
flowchart LR
    PM("<b>PartitionMaintenanceService</b> · every 12 h<br/>create this week + 3 ahead<br/>detach + drop expired weeks")
    DW("<b>DispatchEngine</b> · 2 loops, background<br/>claim up to 200: FOR UPDATE SKIP LOCKED, 2-min lease<br/>planned, Phase 1: sign · send · retry · dead-letter")
    CL("<b>CleanupService</b> · every 30 min<br/>delete marked event types")

    Events[("Events<br/>weekly partitions")]
    Outbox[("DeliveryOutboxes")]
    Destinations[("Destinations")]
    Sub(["Subscriber endpoint"])
    DL[("DeadLetters<br/>planned")]
    Idem[("IdempotencyRecords")]
    EventTypes[("EventTypes")]

    PM -->|"create · detach · drop partitions"| Events
    DW -.->|"payload, joined at claim"| Events
    DW -->|"claim: set LeasedBy · LeaseExpiresAt<br/>planned: retry · delete on 2xx"| Outbox
    DW -->|"read URL · key · timeout at claim<br/>Phase 2: write circuit state"| Destinations
    DW -->|"planned: signed POST"| Sub
    DW -->|"planned: poison · retry limit · age cap"| DL
    CL -->|"planned: purge old"| DL
    CL -->|"planned: delete older than 7 days"| Idem
    CL -->|"delete marked"| EventTypes

    classDef planned stroke-dasharray: 6 4
    class DL planned
```

## Notes

- **Ownership rule:** HTTP services own the control-plane tables; background workers own the data-plane tables. Only two things cross: deleting a destination cascades into the outbox, and the DispatchEngine writes `CircuitState` (Phase 2).
- **Partition drop safety net:** the outbox → Events FK blocks dropping a week that still has pending deliveries. Retention (7 days) is longer than the outbox age cap (3 days), so it shouldn't happen.
- **Planned, not drawn yet:** the routing cache (Phase 4) replaces the 3 routing reads; the admin services will invalidate it when they change a tenant, event type or destination.
