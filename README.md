# reddisRabbitmq

| Area | Technology | Notes |
|------|------------|-------|
| Message-broker | RabbitMQ | Event-driven communication, queues, pub/sub |
| Cache database | Redis | in-memory key-value, caching database |
| WebSocket | SignalR | WebSockets for pushing updates to clients, in real-time |
| Background processing | IHostedService / BackgroundService | Internal long-running workers or scheduled tasks |

| General notes | Area? |
|---------------|-------|
| Maybe use/look at Hangfire for external job scheduler | Background processing |

```Mermaid
flowchart TD

    subgraph Clients["Clients (Browser / Game UI)"]
        A1["• Join matchmaking\n• Send actions\n• Receive updates via SignalR"]
    end

    subgraph Gateway["GameGateway (Web API + SignalR)"]
        B1["MatchController\n /match/{id}/action\n /matchmaking/join"]
        B2["SignalR Hub"]
        B3["MatchService"]
    end

    subgraph Redis["Redis"]
        RQ["matchmaking:queue (LIST)"]
        RP["match:{id}:players (LIST)"]
        RT["match:{id}:turn (STRING)"]
        RA["match:{id}:actions (LIST)"]
        RS["player:{id}:state (HASH)"]
        PUB["pubsub:match:{id} (CHANNEL)"]
    end

    subgraph MatchmakingWorker["Matchmaking Worker"]
        M1["Pop 2 players\nfrom matchmaking:queue"]
        M2["Create match in Redis"]
        M3["Publish MatchCreated event"]
    end

    subgraph GameLogicWorker["Game Logic Worker"]
        G1["Read action from\nmatch:{id}:actions"]
        G2["Validate turn"]
        G3["Apply action\n(move / attack / end)"]
        G4["Update player state in Redis"]
        G5["Increment turn counter"]
        G6["Publish turn update via pubsub"]
    end

    subgraph MQ["RabbitMQ (optional)"]
        Q1["MatchCreated\nTurnProcessed\nMatchEnded"]
    end

    A1 -- SignalR --> B2
    A1 -- HTTP --> B1

    B1 -- "Redis read/write" --> Redis
    B2 -- "Subscribe to pubsub" --> PUB

    M1 -- pops --> RQ
    M2 -- writes --> RP
    M2 -- writes --> RT
    M2 -- writes --> RS
    M2 -- writes --> RA
    M3 -- publish --> MQ

    G1 -- pops --> RA
    G2 --> G3
    G3 --> G4
    G4 -- write --> RS
    G5 -- write --> RT
    G6 -- publish --> PUB

    PUB -- "SignalR broadcast" --> B2
    MQ --> Q1
```
