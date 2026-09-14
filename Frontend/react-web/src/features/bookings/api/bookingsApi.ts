// src/features/bookings/api/bookingsApi.ts
import { bookingsClient } from '@/shared/api';
import type { BookingSchema } from '@/shared/api';
import { getToken } from '@/shared/lib/auth/sessionStore';
// Импортируем типы ошибок
import { toFormError, isProblemDetails, type FormActionError } from '@/shared/api/errors';

type BookingResponseDto = BookingSchema<'BookingResponseDto'>;
type BookingStatus = BookingSchema<'BookingStatus'>;

// 1. Чистые доменные типы
export interface BookingResponse {
  id: string;
  eventId: string;
  status: BookingStatus;
  createdAt: Date;
  processedAt: Date | null;
}

// 2. Размеченные объединения (Discriminated Unions)
export type BookingResult =
  | {
      success: true;
      booking: BookingResponse;
      httpStatus: number;
      error?: never;
    }
  | {
      success: false;
      booking?: never;
      httpStatus: number;
      error: FormActionError;
    };

export type CreateBookingResult =
  | {
      success: true;
      booking: BookingResponse;
      statusUrl: string | null;
      httpStatus: number;
      error?: never;
    }
  | {
      success: false;
      booking?: never;
      statusUrl?: never;
      httpStatus: number;
      error: FormActionError;
    };

export type DeleteBookingResult =
  | {
      success: true;
      httpStatus: number;
      error?: never;
    }
  | {
      success: false;
      httpStatus: number;
      error: FormActionError;
    };

// 3. Мапперы
function mapBookingResponse(dto: BookingResponseDto): BookingResponse {
  return {
    id: dto.id ?? '',
    eventId: dto.eventId ?? '',
    status: dto.status ?? 'Pending',
    createdAt: dto.createdAt ? new Date(dto.createdAt) : new Date(),
    processedAt: dto.processedAt ? new Date(dto.processedAt) : null,
  };
}

// 4. API-функции
export async function getBookingById(
  id: string,
  options?: { signal?: AbortSignal },
): Promise<BookingResult> {
  const token = getToken();
  try {
    const { data, error, response } = await bookingsClient.GET('/Bookings/{id}', {
      params: { path: { id } },
      signal: options?.signal,
      headers: {
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        Pragma: 'no-cache',
      },
    });

    const isProblem = isProblemDetails(data);

    if (!response.ok || error || !data || isProblem) {
      return {
        success: false,
        httpStatus:
          isProblem && typeof (data as Record<string, unknown>)?.status === 'number'
            ? Number((data as Record<string, unknown>).status)
            : response.status,
        error: toFormError<BookingResult>(error ?? data)!,
      };
    }

    return {
      success: true,
      booking: mapBookingResponse(data),
      httpStatus: response.status,
    };
  } catch (ex) {
    if (ex instanceof DOMException && ex.name === 'AbortError') {
      throw ex;
    }
    return { success: false, httpStatus: 0, error: toFormError(ex)! };
  }
}

export async function createBooking(id: string, seats?: string): Promise<CreateBookingResult> {
  void seats;
  const token = getToken();
  try {
    const { data, error, response } = await bookingsClient.POST('/Bookings/{id}/book', {
      params: { path: { id } },
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });

    const isProblem = isProblemDetails(data);

    if (!response.ok || error || !data || isProblem) {
      return {
        success: false,
        httpStatus:
          isProblem && typeof (data as Record<string, unknown>)?.status === 'number'
            ? Number((data as Record<string, unknown>).status)
            : response.status,
        error: toFormError<CreateBookingResult>(
          error ?? data ?? 'Сервер не вернул данные бронирования',
        )!,
      };
    }

    return {
      success: true,
      booking: mapBookingResponse(data),
      statusUrl: response.headers.get('Location'),
      httpStatus: response.status,
    };
  } catch (ex) {
    return {
      success: false,
      httpStatus: 0,
      error: toFormError(ex)!,
    };
  }
}

export async function deleteBookingById(id: string): Promise<DeleteBookingResult> {
  const token = getToken();
  try {
    const { data, error, response } = await bookingsClient.DELETE('/Bookings/{id}', {
      params: { path: { id } },
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });

    const isProblem = isProblemDetails(data);

    if (!response.ok || error || isProblem) {
      return {
        success: false,
        httpStatus:
          isProblem && typeof (data as Record<string, unknown>)?.status === 'number'
            ? Number((data as Record<string, unknown>).status)
            : response.status,
        error: toFormError<DeleteBookingResult>(error ?? data)!,
      };
    }

    return {
      success: true,
      httpStatus: response.status,
    };
  } catch (ex) {
    return { success: false, httpStatus: 0, error: toFormError(ex)! };
  }
}
