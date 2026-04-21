use tauri::Manager;

mod tray;

#[tauri::command]
fn greet(name: &str) -> String {
    format!("Cześć, {}! WorkBase Desktop działa.", name)
}

pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_shell::init())
        .setup(|app| {
            tray::create_tray(app.handle())?;
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![greet])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
