// src/features/events/api/eventsApi.ts
import { eventsClient } from '@/shared/api';
import type { EventSchema } from '@/shared/api';
// Импортируем типы ошибок
import { toFormError, isProblemDetails, type FormActionError } from '@/shared/api/errors';

type EventCreateDto = EventSchema<'EventCreateDto'>;
type EventUpdateDto = EventSchema<'EventUpdateDto'>;
type EventResponseDto = EventSchema<'EventResponseDto'>;
type EventResponsePaginatedResultDto = EventSchema<'PaginatedResultOfEventResponseDto'>;
type SeatReservationResponseDto = EventSchema<'SeatReservationResponseDto'>;

// 1. Чистые доменные типы
export interface EventCreate {
  title: string;
  description: string | null;
  startAt: Date;
  endAt: Date;
  totalSeats: number;
  price?: number;
  currency?: string;
}

export interface EventUpdate {
  title: string;
  description: string | null;
  startAt: Date;
  endAt: Date;
  price?: number;
  currency?: string;
}

export interface EventResponse {
  id: string;
  title: string;
  description: string | null;
  startAt: Date;
  endAt: Date;
  totalSeats: number;
  availableSeats: number;
  price: number;
  currency: string;
  isActive: boolean;
}

export interface SeatReservationResponse {
  bookingId: string;
  eventId: string;
  seats: number;
  unitPrice: number;
  totalPrice: number;
  currency: string;
  discountAmount: number;
  priceVersion: number;
  status: string;
  rejectionReason: string | null;
  createdAt: Date;
  expiresAt: Date | null;
}

// 2. Размеченные объединения (Discriminated Unions)
export type EventResult =
  | {
      success: true;
      event: EventResponse;
      httpStatus: number;
      error?: never;
    }
  | {
      success: false;
      event?: never;
      httpStatus: number;
      error: FormActionError;
    };

export type CreateEventResult =
  | {
      success: true;
      event: EventResponse;
      httpStatus: number;
      statusUrl: string | null;
      error?: never;
    }
  | {
      success: false;
      event?: never;
      httpStatus: number;
      statusUrl?: never;
      error: FormActionError;
    };

export type UpdateEventResult =
  | {
      success: true;
      event?: EventResponse;
      httpStatus: number;
      error?: never;
    }
  | {
      success: false;
      event?: never;
      httpStatus: number;
      error: FormActionError;
    };

export type DeleteEventResult =
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

export type CancelEventResult =
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

export type EventReservationsResult =
  | {
      success: true;
      reservations: SeatReservationResponse[];
      httpStatus: number;
      error?: never;
    }
  | {
      success: false;
      reservations?: never;
      httpStatus: number;
      error: FormActionError;
    };

export interface EventItem {
  event: EventResponse[] | null;
}

export interface EventPaginatedResult {
  totalCount: number;
  items: EventItem;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}

export type PaginatedResult =
  | {
      success: true;
      event: EventPaginatedResult;
      httpStatus: number;
      error?: never;
    }
  | {
      success: false;
      event?: never;
      httpStatus: number;
      error: FormActionError;
    };

export type TopEventsResult =
  | {
      success: true;
      event: EventResponse[];
      httpStatus: number;
      error?: never;
    }
  | {
      success: false;
      event?: never;
      httpStatus: number;
      error: FormActionError;
    };

// 3. Мапперы
function mapEventResponse(dto: EventResponseDto): EventResponse {
  return {
    id: dto.id ?? '',
    title: dto.title ?? '',
    description: dto.description ?? null,
    startAt: dto.startAt ? new Date(dto.startAt) : new Date(),
    endAt: dto.endAt ? new Date(dto.endAt) : new Date(),
    totalSeats: Number(dto.totalSeats) || 0,
    availableSeats: Number(dto.availableSeats) || 0,
    price: Number(dto.price) || 0,
    currency: dto.currency ?? 'KZT',
    isActive: dto.isActive ?? true,
  };
}

function mapSeatReservationResponse(dto: SeatReservationResponseDto): SeatReservationResponse {
  return {
    bookingId: dto.bookingId ?? '',
    eventId: dto.eventId ?? '',
    seats: Number(dto.seats) || 0,
    unitPrice: Number(dto.unitPrice) || 0,
    totalPrice: Number(dto.totalPrice) || 0,
    currency: dto.currency ?? 'KZT',
    discountAmount: Number(dto.discountAmount) || 0,
    priceVersion: Number(dto.priceVersion) || 0,
    status: dto.status ?? 'Pending',
    rejectionReason:
      dto.rejectionReason !== null && dto.rejectionReason !== undefined
        ? String(dto.rejectionReason)
        : null,
    createdAt: dto.createdAt ? new Date(dto.createdAt) : new Date(),
    expiresAt: dto.expiresAt ? new Date(dto.expiresAt) : null,
  };
}

function mapPaginatedEventResponse(dto: EventResponsePaginatedResultDto): EventPaginatedResult {
  return {
    totalCount: Number(dto.totalCount) || 0,
    items: dto.items ? { event: dto.items.map(mapEventResponse) } : { event: null },
    currentPage: Number(dto.currentPage) || 0,
    pageSize: Number(dto.pageSize) || 0,
    totalPages: Number(dto.totalPages) || 0,
  };
}

export function mapEventCreateToDto(domain: EventCreate): EventCreateDto {
  return {
    title: domain.title.trim(),
    description: domain.description?.trim() ?? null,
    startAt: domain.startAt.toISOString(),
    endAt: domain.endAt.toISOString(),
    totalSeats: domain.totalSeats,
    price: domain.price ?? 0,
    currency: domain.currency?.trim() ?? 'KZT',
  };
}

export function mapEventUpdateToDto(domain: EventUpdate): EventUpdateDto {
  return {
    title: domain.title.trim(),
    description: domain.description?.trim() ?? null,
    startAt: domain.startAt.toISOString(),
    endAt: domain.endAt.toISOString(),
    price: domain.price,
    currency: domain.currency?.trim(),
  };
}

export async function createEvent(event: EventCreate): Promise<CreateEventResult> {
  try {
    const { data, error, response } = await eventsClient.POST('/v1/events', {
      body: mapEventCreateToDto(event),
    });

    const isProblem = isProblemDetails(data);

    if (!response.ok || error || !data || isProblem) {
      return {
        success: false,
        httpStatus:
          isProblem && typeof (data as Record<string, unknown>)?.status === 'number'
            ? Number((data as Record<string, unknown>).status)
            : response.status,
        error: toFormError<EventCreate>(error ?? data)!,
      };
    }

    return {
      success: true,
      event: mapEventResponse(data),
      httpStatus: response.status,
      statusUrl: response.headers.get('Location') ?? null,
    };
  } catch (ex) {
    return {
      success: false,
      httpStatus: 0,
      error: toFormError(ex)!,
    };
  }
}

export async function updateEventById(
  id: string,
  eventUpdate: EventUpdate,
): Promise<UpdateEventResult> {
  try {
    const { data, error, response } = await eventsClient.PUT('/v1/events/{id}', {
      body: mapEventUpdateToDto(eventUpdate),
      params: { path: { id } },
    });

    const isProblem = isProblemDetails(data);

    if (!response.ok || error || isProblem) {
      return {
        success: false,
        httpStatus:
          isProblem && typeof (data as Record<string, unknown>)?.status === 'number'
            ? Number((data as Record<string, unknown>).status)
            : response.status,
        error: toFormError<UpdateEventResult>(error ?? data)!,
      };
    }

    return {
      success: true,
      event: data ? mapEventResponse(data) : undefined,
      httpStatus: response.status,
    };
  } catch (ex) {
    return { success: false, httpStatus: 0, error: toFormError(ex)! };
  }
}

export async function getEventById(id: string): Promise<EventResult> {
  try {
    const { data, error, response } = await eventsClient.GET('/v1/events/{id}', {
      params: { path: { id } },
    });

    const isProblem = isProblemDetails(data);

    if (!response.ok || error || !data || isProblem) {
      return {
        success: false,
        httpStatus:
          isProblem && typeof (data as Record<string, unknown>)?.status === 'number'
            ? Number((data as Record<string, unknown>).status)
            : response.status,
        error: toFormError<EventResult>(error ?? data)!,
      };
    }

    return {
      success: true,
      event: mapEventResponse(data),
      httpStatus: response.status,
    };
  } catch (ex) {
    return { success: false, httpStatus: 0, error: toFormError(ex)! };
  }
}

export async function getTopEvents(): Promise<TopEventsResult> {
  try {
    const { data, error, response } = await eventsClient.GET('/v1/events/top');
    const isProblem = isProblemDetails(data) || !Array.isArray(data);

    if (!response.ok || error || !data || isProblem) {
      return {
        success: false,
        httpStatus:
          isProblem && typeof (data as Record<string, unknown>)?.status === 'number'
            ? Number((data as Record<string, unknown>).status)
            : response.status,
        error: toFormError<TopEventsResult>(error ?? data)!,
      };
    }

    return {
      success: true,
      event: data.map(mapEventResponse),
      httpStatus: response.status,
    };
  } catch (ex) {
    return { success: false, httpStatus: 0, error: toFormError(ex)! };
  }
}

export async function getAllEvents(
  Title?: string,
  From?: string,
  To?: string,
  Page?: number,
  PageSize?: number,
  MinPrice?: number,
  MaxPrice?: number,
  Currency?: string,
  IsActive?: boolean,
): Promise<PaginatedResult> {
  try {
    const { data, error, response } = await eventsClient.GET('/v1/events', {
      params: {
        query: {
          Title,
          From,
          To,
          Page,
          PageSize,
          MinPrice,
          MaxPrice,
          Currency,
          IsActive,
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
        error: toFormError<PaginatedResult>(error ?? data, {
          defaultMessage: 'Не удалось загрузить события',
        }) ?? { message: 'Не удалось загрузить события' },
      };
    }

    return {
      success: true,
      event: mapPaginatedEventResponse(data),
      httpStatus: response.status,
    };
  } catch (ex) {
    return {
      success: false,
      httpStatus: 0,
      error: toFormError(ex, { defaultMessage: 'Не удалось загрузить события' })!,
    };
  }
}

export async function deleteEventById(id: string): Promise<DeleteEventResult> {
  try {
    const { data, error, response } = await eventsClient.DELETE('/v1/events/{id}', {
      params: { path: { id } },
    });

    const isProblem = isProblemDetails(data);

    if (!response.ok || error || isProblem) {
      return {
        success: false,
        httpStatus:
          isProblem && typeof (data as Record<string, unknown>)?.status === 'number'
            ? Number((data as Record<string, unknown>).status)
            : response.status,
        error: toFormError<DeleteEventResult>(error ?? data)!,
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

export async function cancelEventById(id: string): Promise<CancelEventResult> {
  try {
    const { data, error, response } = await eventsClient.POST('/v1/events/{id}/cancel', {
      params: { path: { id } },
    });

    const isProblem = isProblemDetails(data);

    if (!response.ok || error || isProblem) {
      return {
        success: false,
        httpStatus:
          isProblem && typeof (data as Record<string, unknown>)?.status === 'number'
            ? Number((data as Record<string, unknown>).status)
            : response.status,
        error: toFormError<CancelEventResult>(error ?? data)!,
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

export async function getEventReservations(id: string): Promise<EventReservationsResult> {
  try {
    const { data, error, response } = await eventsClient.GET('/v1/events/{id}/reservations', {
      params: { path: { id } },
    });

    const isProblem = isProblemDetails(data) || !Array.isArray(data);

    if (!response.ok || error || !data || isProblem) {
      return {
        success: false,
        httpStatus:
          isProblem && typeof (data as Record<string, unknown>)?.status === 'number'
            ? Number((data as Record<string, unknown>).status)
            : response.status,
        error: toFormError<EventReservationsResult>(error ?? data)!,
      };
    }

    return {
      success: true,
      reservations: data.map(mapSeatReservationResponse),
      httpStatus: response.status,
    };
  } catch (ex) {
    return { success: false, httpStatus: 0, error: toFormError(ex)! };
  }
}
