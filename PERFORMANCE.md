# Performance diagnostics

Open `http://127.0.0.1:3000/?perf=1`, then create or join a battle. The diagnostics panel appears below the board. The opt-in lasts for that browser tab's session; `?perf=0` turns it off. Measurements remain local and are never uploaded.

Use **Reset measurements** after loading, then drive, aim and fire normally, or select **Run 20-second drive + aim + fire**. The automatic run sends real board input events; it moves and fires your tank. It stops when the tab is hidden. Its report freezes at completion. **Download measurements** saves the report as JSON. Reset resumes live measurement.

Keep Chrome uncovered and foregrounded. Chrome can throttle animation frames in covered windows even when `document.visibilityState` says `visible`. Do not interpret one-second RAF gaps as game CPU time without checking long tasks and receive-to-render time. `possibleFrameThrottling` is a heuristic, not proof.

## Reading the measurements

| Metric | Measures | Helps isolate |
| --- | --- | --- |
| `frameGapMs` | Time between foreground animation callbacks | Visible smoothness or browser throttling |
| `longTaskMs` | Browser main-thread tasks lasting at least 50 ms | JavaScript/WASM stalls |
| `serverWorkMs` | Tick entry through simulation, including lock wait | Server simulation or contention |
| `serverIntervalMs` | Time between server tick starts | Tick scheduling/backpressure |
| `serverBroadcastMs` | Previous broadcast duration | Serialization/output backpressure |
| `roundTripMs` | SignalR request/response, every two seconds | Transport plus scheduling, without clock synchronization |
| `updateGapMs` | Time between client snapshot callbacks | Delivery cadence and backlog |
| `receiveToRenderMs` | Snapshot callback to Blazor after-render | Client render work/queue delay; excludes deserialization and physical paint |
| `eventToSendMs` | Latest observed board event to sending input | Client input scheduling |
| `inputToStateMs` | Each sent input sequence to cumulative acknowledgement in rendered state | Input/network/simulation/update delay |
| `inputToNextFrameMs` | Acknowledgement followed by the next RAF | Approximation of visible response; not hardware display latency |
| `wireBytes` | Received UTF-8 SignalR payload bytes | Update size; excludes TCP/WebSocket framing |

Each metric retains at most 1,200 samples. Reports show total count, retained count, median, p95 and maximum. Percentiles describe the retained window; rate counters describe the interval since reset. Hidden-tab frame/update/input samples are excluded. Input acknowledgements are cumulative: several inputs may be acknowledged by one snapshot. Acknowledgement does not imply each intermediate input produced a separate simulation tick. Broadcast timing is carried by the next snapshot.

`boardRenders` should track updates rather than mouse events. `mapRenders` should stay zero after resetting a warmed-up game. No samples is different from a measured zero. Startup/JIT work should be measured separately from steady gameplay. The report records recent browser errors and focus/visibility state.

## Foggish investigation, 2026-09-30

The initial Highland Ruins implementation resent the map every tick and recomputed/formatted curved SVG outlines in WebAssembly. Rendering took longer than the 100 ms update interval, causing an accumulating queue. Mouse events triggered additional board rendering. CSS moved the camera instantly while tank positions animated.

Changes:

- Send map geometry once on subscription and again after reconnect; routine snapshots contain dynamic state only.
- Retain the map object and SVG subtree, cache shape outlines and SVG point strings, and invalidate caches on shape changes.
- Avoid Blazor board rendering for input-only events. Send keyboard/fire changes promptly and sample aiming at 50 Hz, awaiting sends instead of overlapping them.
- Animate camera transforms over the same interval as tank movement.
- Reuse one SignalR connection, dispose page subscriptions, and unsubscribe when leaving a game.
- Use asynchronous periodic server timing rather than adding a blocking sleep after every tick; remove per-tick/input console output.
- Handle missing battles with a lobby recovery message instead of a renderer exception.

Observed in local Chrome (Debug build):

| Measurement | Before | After |
| --- | --- | --- |
| Receive-to-render median | 185.7 ms | 1.1 ms |
| Receive-to-render p95 | 205.1 ms | 1.3 ms |
| Typical update payload | 12,217 bytes, one tank | 1,164 bytes, two tanks |
| Foreground frame rate | Baseline stalled; covered-window results unsuitable for an exact FPS ratio | 60 FPS |
| Foreground frame-gap p95 | Not comparable across window visibility | 16.8 ms |
| Input-to-state p95 | 23,074 ms during accumulated backlog, only six acknowledged samples | 106.6 ms, 661 acknowledged inputs |

The final foreground sample covered 32 seconds of two-player Foggish gameplay, including mouse/keyboard input. Baseline and final input sampling/window conditions differ, so input values show elimination of backlog rather than a controlled speedup ratio. The simulation still runs at 10 Hz; this imposes up to roughly one tick of input waiting even with a fast renderer. Sixty display frames per second does not mean 60 authoritative simulation updates.

`PerformanceRegressionTests` exercises 40 moving tanks and projectiles on every Foggish map, including snapshot serialization. Initial measured p95 tick costs were 2.60 ms (Frontier Town), 5.43 ms (Highland Ruins), and 3.00 ms (Farmland Patrol), against a 100 ms budget. This is a server workload test, not a claim of testing 40 separate browsers or a remote network.

Run `dotnet test GameTest --logger "trx;LogFileName=performance.trx" --results-directory .runlogs`. Tests protect compact snapshots, cache invalidation, missing-battle recovery and the 40-tank server budget. Local reports/build logs are under `.runlogs/` and excluded from git.

If profiling exposes new rendering limits, next candidates are viewport culling and Canvas rendering with cached terrain. If input delay is the remaining issue, evaluate client prediction and reconciliation separately; changing the tick rate alone would also change existing per-tick gameplay constants. Measure first and retain the same map, player count, window visibility and workload for comparisons.
