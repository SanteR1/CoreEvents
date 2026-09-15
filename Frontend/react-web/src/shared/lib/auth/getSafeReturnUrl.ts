/**
 * Валидирует и нормализует URL перенаправления, защищая от атак Open Redirect (CWE-601).
 * Разрешены только относительные пути приложения (начинающиеся с одного слэша '/').
 * Запрещены protocol-relative URL ('//evil.com'), обратные слэши ('/\\evil.com'),
 * внешние схемы ('https://', 'javascript:', etc.).
 */
export function getSafeReturnUrl(target: string | null | undefined): string {
  if (!target || typeof target !== 'string') {
    return '/';
  }

  // URL должен начинаться строго с '/', но не с '//' и не с '/\'
  if (!target.startsWith('/') || target.startsWith('//') || target.startsWith('/\\')) {
    return '/';
  }

  return target;
}
