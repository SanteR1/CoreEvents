// Эта логика не привязана к React — её можно вызывать откуда угодно,
// в том числе из action/loader роутера, у которых нет доступа к контексту.
const TOKEN_KEY = 'auth_token';
const listeners = new Set<() => void>();

// TODO: Переделать на HttpOnly cookie, чтобы токен не был доступен в JS и не мог быть украден XSS-атакой.
// TODO: добавить проверку на refreshToken, если будет реализован

/**
 * Проверяет, не истек ли срок действия JWT токена.
 * Не валидирует подпись (это делает бэкенд), но отсекает протухшие токены.
 */
function isTokenExpired(token: string): boolean {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return true;

    // Декодируем только payload для чтения поля "exp"
    const payloadJson = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'));
    const payload = JSON.parse(payloadJson) as { exp?: number };

    if (!payload.exp) {
      return false; // Если поле exp отсутствует, считаем токен условно валидным
    }

    const currentTimeInSeconds = Math.floor(Date.now() / 1000);
    // Добавляем буфер 5 секунд на погрешность времени сети
    return payload.exp <= currentTimeInSeconds + 5;
  } catch {
    return true; // Любая ошибка парсинга означает невалидный токен
  }
}

export function getToken(): string | null {
  const token = localStorage.getItem(TOKEN_KEY);

  if (!token || token === 'undefined' || token === 'null') {
    return null;
  }

  // Если токен протух — сразу зачищаем хранилище и возвращаем null
  if (isTokenExpired(token)) {
    // Тихо удаляем мусор из хранилища, чтобы не занимал место.
    // Главное — НЕ вызываем listeners.forEach(), так как это ломает рендер React.
    // listeners.forEach((listener) => listener());

    localStorage.removeItem(TOKEN_KEY);
    return null;
  }

  return token;
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
  listeners.forEach((listener) => listener());
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY);
  listeners.forEach((listener) => listener());
}

// Подписка для useSyncExternalStore: реагирует и на локальные вызовы setToken/clearToken,
// и на изменение localStorage из другой вкладки.
export function subscribe(callback: () => void): () => void {
  listeners.add(callback);
  window.addEventListener('storage', callback);
  return () => {
    listeners.delete(callback);
    window.removeEventListener('storage', callback);
  };
}
