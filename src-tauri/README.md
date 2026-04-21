# WorkBase Desktop — Tauri Wrapper (POC)

Proof-of-concept Tauri v2 wrapper for the WorkBase React SPA.

## Prerequisites

- [Rust](https://rustup.rs/) (stable, ≥ 1.77)
- Node.js 20+
- Platform-specific dependencies: see [Tauri prerequisites](https://v2.tauri.app/start/prerequisites/)

## Development

```bash
# Install frontend deps
cd frontend && npm install && cd ..

# Run in dev mode (hot-reload)
cd src-tauri
cargo tauri dev
```

## Build

```bash
cd src-tauri
cargo tauri build
```

Output binaries will be in `src-tauri/target/release/bundle/`.

## Features

- **Hash routing**: Automatically uses `HashRouter` when Tauri is detected (`window.__TAURI__`)
- **System tray**: Clock-in, clock-out, and break actions from the system tray
- **Notifications**: Uses `tauri-plugin-notification` for desktop notifications
- **Single window**: 1280x800 default, resizable, with minimum 900x600

## Architecture

```
src-tauri/
├── Cargo.toml          # Rust dependencies
├── tauri.conf.json     # Tauri configuration
├── build.rs            # Build script
└── src/
    ├── main.rs         # Entry point
    ├── lib.rs          # App setup, plugins, commands
    └── tray.rs         # System tray menu + actions
```

The frontend communicates with the tray via `window.__WORKBASE_TRAY_ACTION__` callback, consumed by the `useTrayActions` hook.

## Environment Variables

Set `VITE_ROUTER_MODE=hash` to force hash routing in development (not needed in Tauri builds — auto-detected).
