import { api } from '@/api/client';
import { appStorage } from '@/shared';

const VAPID_PUBLIC_KEY_STORAGE = 'workbase_vapid_public_key';

export async function isPushSupported(): Promise<boolean> {
  return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
}

export async function getPushPermission(): Promise<NotificationPermission> {
  return Notification.permission;
}

export async function requestPushPermission(): Promise<NotificationPermission> {
  return await Notification.requestPermission();
}

export async function subscribeToPush(vapidPublicKey: string): Promise<boolean> {
  try {
    const supported = await isPushSupported();
    if (!supported) return false;

    const permission = await requestPushPermission();
    if (permission !== 'granted') return false;

    const registration = await navigator.serviceWorker.ready;

    const subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(vapidPublicKey),
    });

    const json = subscription.toJSON();
    if (!json.endpoint || !json.keys) return false;

    await api.post('/api/notifications/push/subscribe', {
      endpoint: json.endpoint,
      p256dh: json.keys.p256dh,
      auth: json.keys.auth,
      deviceInfo: navigator.userAgent.substring(0, 200),
    });

    appStorage.local.set(VAPID_PUBLIC_KEY_STORAGE, vapidPublicKey);
    return true;
  } catch {
    return false;
  }
}

export async function unsubscribeFromPush(): Promise<boolean> {
  try {
    const registration = await navigator.serviceWorker.ready;
    const subscription = await registration.pushManager.getSubscription();
    if (!subscription) return true;

    const json = subscription.toJSON();

    await subscription.unsubscribe();

    if (json.endpoint) {
      await api.post('/api/notifications/push/unsubscribe', {
        endpoint: json.endpoint,
      });
    }

    localStorage.removeItem(VAPID_PUBLIC_KEY_STORAGE);
    return true;
  } catch {
    return false;
  }
}

export async function isSubscribed(): Promise<boolean> {
  try {
    if (!await isPushSupported()) return false;
    const registration = await navigator.serviceWorker.ready;
    const subscription = await registration.pushManager.getSubscription();
    return subscription !== null;
  } catch {
    return false;
  }
}

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);
  for (let i = 0; i < rawData.length; i++) {
    outputArray[i] = rawData.charCodeAt(i);
  }
  return outputArray;
}
