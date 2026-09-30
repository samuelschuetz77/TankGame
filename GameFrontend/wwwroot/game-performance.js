// Opt in with ?perf=1; ?perf=0 disables it. Bounded, local-only samples, no telemetry upload.
(() => {
    const setting = new URLSearchParams(location.search).get('perf');
    if (setting !== null) sessionStorage.setItem('tankPerf', setting === '1' ? '1' : '0');
    const enabled = sessionStorage.getItem('tankPerf') === '1';
    const noop = () => {};
    if (!enabled) {
        window.tankPerf = { attach: noop, count: noop, sent: noop, received: noop, rendered: noop, sample: noop, isEnabled: () => false };
        return;
    }
    const limit = 1200;
    let samples, counts, sent, received, start, lastFrame, lastUpdate, lastTick, panel, board;
    let driveTimer, driving = false, hiddenSince = null, hiddenMs = 0, eventAt = 0;
    let lastRun = null, inputListeners;
    const errors = [];
    function reset() {
        lastRun = null;
        samples = {}; counts = {}; sent = new Map(); received = new Map();
        start = performance.now(); lastFrame = lastUpdate = lastTick = 0; hiddenMs = 0;
        hiddenSince = document.hidden ? start : null;
    }
    reset();
    function record(name, value) {
        if (!Number.isFinite(value) || value < 0) return;
        const ring = samples[name] ??= { values: new Float64Array(limit), count: 0 };
        ring.values[ring.count++ % limit] = value;
    }
    function count(name, n = 1) { counts[name] = (counts[name] || 0) + n; }
    function stats(ring) {
        const values = Array.from(ring.values.subarray(0, Math.min(ring.count, limit))).sort((a, b) => a - b);
        const at = q => +(values[Math.min(values.length - 1, Math.floor(values.length * q))] || 0).toFixed(2);
        return { count: ring.count, retained: values.length, p50: at(.5), p95: at(.95), max: at(1) };
    }
    function report() {
        const now = performance.now();
        const visibleSeconds = Math.max(.001, (now - start - hiddenMs - (hiddenSince === null ? 0 : now - hiddenSince)) / 1000);
        return {
            version: 2, hasFocus: document.hasFocus(),
            possibleFrameThrottling: !!samples.frameGapMs && stats(samples.frameGapMs).p50 > 250 && (!samples.longTaskMs || stats(samples.longTaskMs).p95 < 50),
            errors: [...errors], status: driving ? 'driving' : 'ready', visibleSeconds: +visibleSeconds.toFixed(1),
            visibility: document.visibilityState, counts: { ...counts },
            rates: { framesPerSecond: +((counts.frames || 0) / visibleSeconds).toFixed(1),
                updatesPerSecond: +((counts.updates || 0) / visibleSeconds).toFixed(1),
                wireKiBPerSecond: +((counts.wireBytes || 0) / visibleSeconds / 1024).toFixed(1) },
            millisecondsExceptWireBytes: Object.fromEntries(Object.entries(samples).map(([k, v]) => [k, stats(v)])),
            note: 'A completed drive freezes this report; Reset resumes live capture. possibleFrameThrottling flags slow RAF without matching long tasks (covered/background windows can throttle even when visibility is visible). Samples bounded to 1200 per metric. Hidden frames excluded. Input-to-frame is acknowledgement plus next RAF, not physical display latency. Wire bytes are UTF-8 SignalR payloads, not TCP overhead.'
        };
    }
    function download() {
        const url = URL.createObjectURL(new Blob([JSON.stringify(lastRun ?? report(), null, 2)], { type: 'application/json' }));
        const a = document.createElement('a'); a.href = url; a.download = `tank-performance-${Date.now()}.json`; a.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
    function key(key, down) { board?.dispatchEvent(new KeyboardEvent(down ? 'keydown' : 'keyup', { key, bubbles: true })); }
    function stopDrive() {
        clearInterval(driveTimer);
        const wasDriving = driving; driving = false;
        for (const k of ['w', 'a', 's', 'd', ' ']) key(k, false);
        if (wasDriving) lastRun = report();
    }
    function drive() {
        if (!board || driving) return;
        reset(); driving = true; board.focus();
        let previous = '', fire = false;
        const began = performance.now();
        driveTimer = setInterval(() => {
            const elapsed = performance.now() - began;
            if (elapsed >= 20000 || document.hidden || !board.isConnected) { stopDrive(); return; }
            const next = ['w', 'd', 's', 'a'][Math.floor(elapsed / 2500) % 4];
            if (next !== previous) { if (previous) key(previous, false); key(next, true); previous = next; }
            const shooting = elapsed % 1200 < 120;
            if (shooting !== fire) { key(' ', shooting); fire = shooting; }
            const rect = board.getBoundingClientRect();
            board.dispatchEvent(new MouseEvent('mousemove', { bubbles: true,
                clientX: rect.left + rect.width / 2 + 150 * Math.cos(elapsed / 700),
                clientY: rect.top + rect.height / 2 + 150 * Math.sin(elapsed / 700) }));
        }, 16);
    }
    function attach(element) {
        if (board === element) return;
        stopDrive();
        inputListeners?.abort();
        inputListeners = new AbortController();
        board = element;
        reset();
        const observeInput = () => { if (!document.hidden) { eventAt = performance.now(); count('inputEvents'); } };
        for (const event of ['keydown', 'keyup', 'mousemove', 'mousedown', 'mouseup'])
            board.addEventListener(event, observeInput, { capture: true, signal: inputListeners.signal });
        if (panel) return;
        panel = document.createElement('details'); panel.id = 'tank-performance'; panel.open = true;
        panel.style.cssText = 'margin:16px;padding:12px;border:1px solid #999;background:#f8fafc;color:#111;font:12px monospace;max-width:1100px';
        const title = document.createElement('summary'); title.textContent = 'Performance diagnostics (local only)'; panel.append(title);
        for (const [label, action] of [['Reset measurements', reset], ['Run 20-second drive + aim + fire', drive], ['Stop drive', stopDrive], ['Download measurements', download]]) {
            const button = document.createElement('button'); button.textContent = label; button.onclick = action; button.style.margin = '6px'; panel.append(button);
        }
        const output = document.createElement('pre'); output.id = 'tank-performance-report'; panel.append(output); document.body.append(panel);
        setInterval(() => { panel.hidden = !board?.isConnected; if (panel.open && !panel.hidden) output.textContent = JSON.stringify(lastRun ?? report(), null, 2); }, 1000);
    }
    document.addEventListener('visibilitychange', () => {
        if (document.hidden) { hiddenSince = performance.now(); stopDrive(); }
        else { if (hiddenSince !== null) hiddenMs += performance.now() - hiddenSince; hiddenSince = null; }
        lastFrame = lastUpdate = 0;
    });
    function frame(now) {
        if (board?.isConnected && !document.hidden) {
            if (lastFrame) record('frameGapMs', now - lastFrame);
            lastFrame = now; count('frames');
        }
        requestAnimationFrame(frame);
    }
    requestAnimationFrame(frame);
    if (PerformanceObserver.supportedEntryTypes?.includes('longtask')) {
        new PerformanceObserver(list => {
            if (board?.isConnected && !document.hidden)
                for (const task of list.getEntries()) { record('longTaskMs', task.duration); count('longTasks'); }
        }).observe({ type: 'longtask' });
    }
    // Observe actual received payloads, including serialization volume and duplicates.
    for (const type of ['error', 'unhandledrejection']) window.addEventListener(type, event => {
        errors.push(String(event.message || event.reason || type).slice(0, 300));
        if (errors.length > 5) errors.shift();
    });
    const NativeSocket = window.WebSocket;
    window.WebSocket = class extends NativeSocket {
        constructor(...args) {
            super(...args);
            this.addEventListener('message', event => {
                if (!board?.isConnected || document.hidden) return;
                const bytes = typeof event.data === 'string' ? new TextEncoder().encode(event.data).byteLength : event.data.byteLength || event.data.size || 0;
                count('wireMessages'); count('wireBytes', bytes); record('wireBytes', bytes);
            });
        }
    };
    window.tankPerf = {
        attach, count, sample: record, isEnabled: () => true,
        sent(sequence) {
            if (document.hidden) return;
            const now = performance.now();
            sent.set(sequence, now); if (sent.size > limit) sent.delete(sent.keys().next().value);
            if (eventAt) { record('eventToSendMs', now - eventAt); eventAt = 0; }
            count('inputsSent');
        },
        received(tick, work, interval, broadcast) {
            if (document.hidden) return;
            const now = performance.now();
            if (lastUpdate) record('updateGapMs', now - lastUpdate);
            if (tick <= lastTick) count('duplicateOrReorderedTicks');
            lastTick = tick; lastUpdate = now; received.set(tick, now);
            if (received.size > limit) received.delete(received.keys().next().value);
            record('serverWorkMs', work); record('serverIntervalMs', interval); record('serverBroadcastMs', broadcast); count('updates');
        },
        rendered(tick, sequence) {
            if (document.hidden) return;
            const now = performance.now(); count('gameRenders');
            if (received.has(tick)) { record('receiveToRenderMs', now - received.get(tick)); received.delete(tick); }
            const acknowledged = [];
            for (const [key, inputAt] of sent) if (key <= sequence) {
                record('inputToStateMs', now - inputAt);
                acknowledged.push(inputAt); sent.delete(key);
            }
            if (acknowledged.length) requestAnimationFrame(() => {
                if (!document.hidden) for (const inputAt of acknowledged)
                    record('inputToNextFrameMs', performance.now() - inputAt);
            });
        }
    };
})();
