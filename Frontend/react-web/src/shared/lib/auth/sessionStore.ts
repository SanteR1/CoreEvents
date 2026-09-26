import type { User } from './user';

// In-Memory хранилище сессии пользователя (No-JWT Architecture)
let currentUser: User | null = null;
let legacyToken: string | null = null;
const listeners = new Set<() => void>();

function notifyListeners(): void {
  listeners.forEach((listener) => listener());
  try {
    localStorage.setItem('auth_sync', Date.now().toString());
  } catch {
    // Игнорируем ошибки квот или приватного режима
  }
}

export function getUser(): User | null {
  return currentUser;
}

export function setUser(user: User | null): void {
  currentUser = user;
  legacyToken = null;
  notifyListeners();
}

export function clearUser(): void {
  currentUser = null;
  legacyToken = null;
  notifyListeners();
}

export function isAuthenticated(): boolean {
  return Boolean(currentUser);
}

export function isAdmin(): boolean {
  return currentUser?.role === 'Admin';
}

// Подписка для useSyncExternalStore: реагирует на setUser/clearUser
// и на событие 'storage' из других вкладок.
export function subscribe(callback: () => void): () => void {
  listeners.add(callback);
  window.addEventListener('storage', callback);
  return () => {
    listeners.delete(callback);
    window.removeEventListener('storage', callback);
  };
}

// Вспомогательные функции для обратной совместимости
export function getToken(): string | null {
  return legacyToken;
}

export function setToken(token?: unknown): void {
  if (token && typeof token === 'object' && 'id' in token) {
    setUser(token as User);
  } else if (typeof token === 'string' && token.length > 0) {
    legacyToken = token;
    currentUser ??= { id: 'legacy_admin_id', userName: 'Admin', role: 'Admin' };
    notifyListeners();
  }
}

export function clearToken(): void {
  clearUser();
}
