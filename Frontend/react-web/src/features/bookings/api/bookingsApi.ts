// src/features/bookings/api/bookingsApi.ts
import { bookingsClient } from '@/shared/api';
import type { BookingSchema } from '@/shared/api';
// Импортируем типы ошибок
import { toFormError, isProblemDetails, type FormActionError } from '@/shared/api/errors';

type BookingResponseDto = BookingSchema<'BookingResponseDto'>;
type BookingStatus = BookingSchema<'BookingStatus'>;
type BookingPaginatedResultDto = BookingSchema<'PaginatedResultOfBookingResponseDto'>;
type CancellationReason = BookingSchema<'CancellationReason'>;

// 1. Чистые доменные типы
export interface BookingResponse {
  id: string;
  eventId: string;
  status: BookingStatus;
  createdAt: Date;
  processedAt: Date | null;
  userId?: string;
  seats: number;
  unitPrice: number;
  totalPrice: number;
  discountAmount: number;
  currency: string;
  rejectionReason: string | null;
  cancellationReason: CancellationReason | null;
  cancellationRequestedAt: Date | null;
}

export interface BookingPaginatedResult {
  totalCount: number;
  items: BookingResponse[];
  currentPage: number;
  pageSize: number;
  totalPages: number;
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
      booking?: BookingResponse;
      httpStatus: number;
      error?: never;
    }
  | {
      success: false;
      booking?: never;
      httpStatus: number;
      error: FormActionError;
    };

export type BookingsPaginatedResult =
  | {
      success: true;
      result: BookingPaginatedResult;
      httpStatus: number;
      error?: never;
    }
  | {
      success: false;
      result?: never;
      httpStatus: number;
      error: FormActionError;
    };

export interface BookingFilterParams {
  status?: BookingStatus;
  userId?: string;
  page?: number;
  pageSize?: number;
}

// 3. Мапперы
function mapBookingResponse(dto: BookingResponseDto): BookingResponse {
  return {
    id: dto.id ?? '',
    eventId: dto.eventId ?? '',
    status: dto.status ?? 'Pending',
    createdAt: dto.createdAt ? new Date(dto.createdAt) : new Date(),
    processedAt: dto.processedAt ? new Date(dto.processedAt) : null,
    userId: dto.userId ?? (dto as { userId?: string }).userId,
    seats: Number(dto.seats) || 1,
    unitPrice: Number(dto.unitPrice) || 0,
    totalPrice: Number(dto.totalPrice) || 0,
    discountAmount: Number(dto.discountAmount) || 0,
    currency: dto.currency ?? 'KZT',
    rejectionReason: dto.rejectionReason ?? null,
    cancellationReason: dto.cancellationReason ?? null,
    cancellationRequestedAt: dto.cancellationRequestedAt
      ? new Date(dto.cancellationRequestedAt)
      : null,
  };
}

function mapPaginatedBookingResponse(dto: BookingPaginatedResultDto): BookingPaginatedResult {
  return {
    totalCount: Number(dto.totalCount) || 0,
    items: Array.isArray(dto.items) ? dto.items.map(mapBookingResponse) : [],
    currentPage: Number(dto.currentPage) || 0,
    pageSize: Number(dto.pageSize) || 0,
    totalPages: Number(dto.totalPages) || 0,
  };
}

// 4. API-функции
export async function getBookingById(
  id: string,
  options?: { signal?: AbortSignal },
): Promise<BookingResult> {
  try {
    const { data, error, response } = await bookingsClient.GET('/v1/bookings/{id}', {
      params: { path: { id } },
      signal: options?.signal,
      cache: 'no-store',
      headers: {
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

export async function createBooking(
  id: string,
  seats?: number | string,
): Promise<CreateBookingResult> {
  const parsedSeats = typeof seats === 'string' ? parseInt(seats, 10) : seats;
  const validSeats = parsedSeats && parsedSeats > 0 ? parsedSeats : 1;

  try {
    const { data, error, response } = await bookingsClient.POST('/v1/bookings/{id}/book', {
      params: { path: { id } },
      body: {
        seats: validSeats,
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

export async function deleteBookingById(
  id: string,
  reason: CancellationReason = 'UserCancelled',
): Promise<DeleteBookingResult> {
  try {
    const { data, error, response } = await bookingsClient.DELETE('/v1/bookings/{id}', {
      params: { path: { id } },
      body: {
        reason,
      },
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
      booking: data ? mapBookingResponse(data) : undefined,
      httpStatus: response.status,
    };
  } catch (ex) {
    return { success: false, httpStatus: 0, error: toFormError(ex)! };
  }
}

export async function getMyBookings(
  params?: BookingFilterParams,
): Promise<BookingsPaginatedResult> {
  try {
    const { data, error, response } = await bookingsClient.GET('/v1/bookings', {
      params: {
        query: {
          Status: params?.status,
          UserId: params?.userId,
          Page: params?.page,
          PageSize: params?.pageSize,
        },
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
        error: toFormError<BookingsPaginatedResult>(error ?? data, {
          defaultMessage: 'Не удалось загрузить список бронирований',
        }) ?? { message: 'Не удалось загрузить список бронирований' },
      };
    }

    return {
      success: true,
      result: mapPaginatedBookingResponse(data),
      httpStatus: response.status,
    };
  } catch (ex) {
    return {
      success: false,
      httpStatus: 0,
      error: toFormError(ex, { defaultMessage: 'Не удалось загрузить список бронирований' })!,
    };
  }
}
