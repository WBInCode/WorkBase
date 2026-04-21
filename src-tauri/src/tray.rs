use tauri::{
    AppHandle,
    menu::{Menu, MenuItem},
    tray::{TrayIcon, TrayIconBuilder},
};

pub fn create_tray(app: &AppHandle) -> Result<TrayIcon, Box<dyn std::error::Error>> {
    let clock_in = MenuItem::with_id(app, "clock_in", "Rozpocznij pracę", true, None::<&str>)?;
    let clock_out = MenuItem::with_id(app, "clock_out", "Zakończ pracę", true, None::<&str>)?;
    let break_start = MenuItem::with_id(app, "break_start", "Przerwa", true, None::<&str>)?;
    let separator = MenuItem::with_id(app, "sep", "─────────", false, None::<&str>)?;
    let show = MenuItem::with_id(app, "show", "Otwórz WorkBase", true, None::<&str>)?;
    let quit = MenuItem::with_id(app, "quit", "Zamknij", true, None::<&str>)?;

    let menu = Menu::with_items(app, &[
        &clock_in,
        &clock_out,
        &break_start,
        &separator,
        &show,
        &quit,
    ])?;

    let tray = TrayIconBuilder::new()
        .tooltip("WorkBase")
        .menu(&menu)
        .on_menu_event(move |app, event| {
            match event.id.as_ref() {
                "clock_in" => {
                    emit_tray_action(app, "clock-in");
                }
                "clock_out" => {
                    emit_tray_action(app, "clock-out");
                }
                "break_start" => {
                    emit_tray_action(app, "break-start");
                }
                "show" => {
                    if let Some(window) = app.get_webview_window("main") {
                        let _ = window.show();
                        let _ = window.set_focus();
                    }
                }
                "quit" => {
                    app.exit(0);
                }
                _ => {}
            }
        })
        .build(app)?;

    Ok(tray)
}

fn emit_tray_action(app: &AppHandle, action: &str) {
    if let Some(window) = app.get_webview_window("main") {
        let _ = window.eval(&format!(
            "window.__WORKBASE_TRAY_ACTION__ && window.__WORKBASE_TRAY_ACTION__('{}')",
            action
        ));
    }
}
