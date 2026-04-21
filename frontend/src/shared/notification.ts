/**
 * shared/notification — Platform-agnostic notification abstraction.
 * Web: Push API, Tauri: tauri-plugin-notification, Capacitor: @capacitor/push-notifications.
 */

export interface NotificationAdapter {
  isSupported(): Promise<boolean>;
  requestPermission(): Promise<'granted' | 'denied' | 'default'>;
  show(title: string, options?: { body?: string; icon?: string }): Promise<void>;
}

class WebNotificationAdapter implements NotificationAdapter {
  async isSupported() {
    return typeof window !== 'undefined' && 'Notification' in window;
  }
  async requestPermission() {
    return await Notification.requestPermission();
  }
  async show(title: string, options?: { body?: string; icon?: string }) {
    if (Notification.permission === 'granted') {
      new Notification(title, options);
    }
  }
}

class NoopNotificationAdapter implements NotificationAdapter {
  async isSupported() { return false; }
  async requestPermission() { return 'denied' as const; }
  async show() { /* noop */ }
}

let _adapter: NotificationAdapter = typeof window !== 'undefined' && 'Notification' in window
  ? new WebNotificationAdapter()
  : new NoopNotificationAdapter();

export function setNotificationAdapter(adapter: NotificationAdapter) {
  _adapter = adapter;
}

export const appNotification = {
  isSupported: () => _adapter.isSupported(),
  requestPermission: () => _adapter.requestPermission(),
  show: (title: string, options?: { body?: string; icon?: string }) => _adapter.show(title, options),
};
