// src/shared/api/errors.ts
export type FieldErrors = Record<string, string[]>;

export interface FormActionError {
  message: string;
  fieldErrors?: FieldErrors;
}

// Расширенный универсальный тип ProblemDetails (по стандарту RFC 7807 + ASP.NET Core)
export interface ProblemDetailsLike {
  type?: string | null;
  title?: string | null;
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  // Поддержка обычных объектов ошибок и FormActionError
  message?: string | null;
  // Ошибки валидации (от ModelState / FluentValidation)
  errors?: Record<string, string[] | string>;
  // Дополнительные расширения ASP.NET
  extensions?: {
    errors?: Record<string, string[] | string>;
    [key: string]: unknown;
  };
  [key: string]: unknown;
}

/**
 * Проверяет, является ли объект телом ошибки (ProblemDetails / ValidationProblemDetails),
 * даже если HTTP-статус ответа был ошибочно возвращён как 200 OK.
 */
export function isProblemDetails(data: unknown): data is ProblemDetailsLike {
  if (!data || typeof data !== 'object') {
    return false;
  }

  const obj = data as Record<string, unknown>;

  // 1. Наличие словаря ошибок валидации (ASP.NET ModelState, FluentValidation)
  if ('errors' in obj && obj.errors && typeof obj.errors === 'object') {
    return true;
  }

  // 2. Наличие расширений с ошибками
  if (
    'extensions' in obj &&
    obj.extensions &&
    typeof obj.extensions === 'object' &&
    'errors' in (obj.extensions as Record<string, unknown>)
  ) {
    return true;
  }

  // 3. HTTP-статус ошибки внутри тела (RFC 7807)
  if (typeof obj.status === 'number' && obj.status >= 400) {
    return true;
  }

  // 4. Явный флаг неуспеха из кастомных API
  if ('isSuccess' in obj && obj.isSuccess === false) {
    return true;
  }

  return false;
}

const NETWORK_ERROR_MESSAGE =
  'Сервер временно недоступен. Проверьте подключение к интернету или повторите попытку позже.';

function isNetworkError(msg?: string | null): boolean {
  if (!msg) return false;
  return (
    msg === 'Failed to fetch' ||
    msg === 'NetworkError' ||
    msg.includes('NetworkError') ||
    msg.includes('Failed to fetch')
  );
}

// Приводит ключи полей из PascalCase (бэкенд) к camelCase (форма)
// Пример: "Event.TotalSeats[0]" -> "event.totalSeats[0]"
export function normalizeFieldKey(key: string): string {
  const regex = /^([^[]+)(\[\d+\])?$/;
  return key
    .split('.')
    .map((segment) => {
      const match = regex.exec(segment);
      if (!match) return segment;
      const [, name, index = ''] = match;
      return name.charAt(0).toLowerCase() + name.slice(1) + index;
    })
    .join('.');
}

interface ToFormErrorOptions<T> {
  defaultMessage?: string;
  fieldNameMap?: Record<string, string>;
  // Типизированное поле для локальных ошибок валидации в action
  // Позволяет привязать общую ошибку к конкретному полю формы с автокомплитом
  localFieldName?: Extract<keyof T, string>;
}

/**
 * Универсальный парсер: принимает ProblemDetails, JS Error, строку или неизвестную ошибку
 * и преобразует в удобный для форм объект FormActionError.
 */
export function toFormError<T = unknown>(
  err: unknown,
  options?: ToFormErrorOptions<T>,
): FormActionError | null {
  if (!err) {
    return null;
  }

  const defaultMessage = options?.defaultMessage ?? 'Произошла ошибка';
  const localFieldName = options?.localFieldName as string | undefined;
  const fieldNameMap = options?.fieldNameMap;

  // 1. Обычная строка
  if (typeof err === 'string') {
    const message = isNetworkError(err) ? NETWORK_ERROR_MESSAGE : err;
    return {
      message,
      fieldErrors: localFieldName ? { [localFieldName]: [err] } : undefined,
    };
  }

  // 2. Стандартный JS Error
  if (err instanceof Error) {
    const rawMessage = err.message;
    const message = isNetworkError(rawMessage) ? NETWORK_ERROR_MESSAGE : rawMessage;

    return {
      message,
      fieldErrors: localFieldName ? { [localFieldName]: [message] } : undefined,
    };
  }

  // 3. Объекты / ProblemDetails
  if (typeof err === 'object' && err !== null) {
    const problem = err as ProblemDetailsLike;

    // Ищем понятный текст по приоритету: detail -> title -> message -> defaultMessage
    const rawMessage = problem.detail ?? problem.title ?? problem.message ?? defaultMessage;
    const message = isNetworkError(rawMessage) ? NETWORK_ERROR_MESSAGE : rawMessage;

    // Извлекаем ошибки полей (напрямую или из extensions)
    const rawErrors = problem.errors ?? problem.extensions?.errors;
    const fieldErrors: FieldErrors = {};

    if (rawErrors) {
      Object.entries(rawErrors).forEach(([key, value]) => {
        const normalizedKey = normalizeFieldKey(key);
        const finalKey = fieldNameMap?.[normalizedKey] ?? normalizedKey;
        const messages = Array.isArray(value) ? value.map(String) : [String(value)];

        fieldErrors[finalKey] = [...(fieldErrors[finalKey] ?? []), ...messages];
      });
    }

    if (localFieldName) {
      fieldErrors[localFieldName] = [...(fieldErrors[localFieldName] ?? []), message];
    }

    return {
      message,
      fieldErrors: Object.keys(fieldErrors).length > 0 ? fieldErrors : undefined,
    };
  }

  // 4. Любые другие неизвестные примитивы (числа, boolean и т.д.)
  return {
    message: defaultMessage,
    fieldErrors: localFieldName ? { [localFieldName]: [defaultMessage] } : undefined,
  };
}
