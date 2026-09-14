// src/features/events/api/eventsApi.ts
import { eventsClient } from '@/shared/api';
import type { EventSchema } from '@/shared/api';
import { getToken } from '@/shared/lib/auth/sessionStore';
// Импортируем типы ошибок
import { toFormError, isProblemDetails, type FormActionError } from '@/shared/api/errors';

type EventCreateDto = EventSchema<'EventCreateDto'>;
type EventUpdateDto = EventSchema<'EventUpdateDto'>;
type EventResponseDto = EventSchema<'EventResponseDto'>;
type EventResponsePaginatedResultDto = EventSchema<'EventResponseDtoPaginatedResult'>;

// 1. Чистые доменные типы
export interface EventCreate {
  title: string;
  description: string | null;
  startAt: Date;
  endAt: Date;
  totalSeats: number;
}
export interface EventUpdate {
  title: string;
  description: string | null;
  startAt: Date;
  endAt: Date;
}
export interface EventResponse {
  id: string;
  title: string;
  description: string | null;
  startAt: Date;
  endAt: Date;
  totalSeats: number;
  availableSeats: number;
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

export type ItemPaginatedResult =
  | {
      event: EventResponse[];
    }
  | {
      event: null;
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

export interface EventPaginatedResult {
  totalCount: number;
  items: ItemPaginatedResult;
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

export type CreateEventResult =
  | {
      success: true;
      event: EventResponse;
      statusUrl: string | null;
      httpStatus: number;
      error?: never;
    }
  | {
      success: false;
      event?: never;
      statusUrl?: never;
      httpStatus: number;
      error: FormActionError;
    };

export type UpdateEventResult =
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

// 3. Мапперы
function mapEventResponse(dto: EventResponseDto): EventResponse {
  return {
    id: dto.id ?? '',
    title: dto.title ?? '',
    description: dto.description ?? null,
    startAt: dto.startAt ? new Date(dto.startAt) : new Date(),
    endAt: dto.endAt ? new Date(dto.endAt) : new Date(),
    totalSeats: dto.totalSeats ?? 0,
    availableSeats: dto.availableSeats ?? 0,
  };
}

function mapPaginatedEventResponse(dto: EventResponsePaginatedResultDto): EventPaginatedResult {
  return {
    totalCount: dto.totalCount ?? 0,
    items: dto.items ? { event: dto.items.map(mapEventResponse) } : { event: null },
    currentPage: dto.currentPage ?? 0,
    pageSize: dto.pageSize ?? 0,
    totalPages: dto.totalPages ?? 0,
  };
}

export function mapEventCreateToDto(domain: EventCreate): EventCreateDto {
  return {
    title: domain.title.trim(),
    description: domain.description?.trim() ?? null,
    startAt: domain.startAt.toISOString(),
    endAt: domain.endAt.toISOString(),
    totalSeats: domain.totalSeats,
  };
}

export function mapEventUpdateToDto(domain: EventUpdate): EventUpdateDto {
  return {
    title: domain.title.trim(),
    description: domain.description?.trim() ?? null,
    startAt: domain.startAt.toISOString(),
    endAt: domain.endAt.toISOString(),
  };
}

export async function createEvent(event: EventCreate): Promise<CreateEventResult> {
  const token = getToken();
  try {
    const { data, error, response } = await eventsClient.POST('/Events', {
      body: mapEventCreateToDto(event),
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
  const token = getToken();

  try {
    const { data, error, response } = await eventsClient.PUT('/Events/{id}', {
      body: mapEventUpdateToDto(eventUpdate),
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
        error: toFormError<UpdateEventResult>(error ?? data)!,
      };
    }

    return { success: true, httpStatus: response.status };
  } catch (ex) {
    return { success: false, httpStatus: 0, error: toFormError(ex)! };
  }
}

export async function getEventById(id: string): Promise<EventResult> {
  const token = getToken();

  try {
    const { data, error, response } = await eventsClient.GET('/Events/{id}', {
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
    const { data, error, response } = await eventsClient.GET('/Events/top');
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
): Promise<PaginatedResult> {
  const { data, error, response } = await eventsClient.GET('/Events', {
    params: {
      query: { Title, From, To, Page, PageSize },
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
      error: toFormError<PaginatedResult>(error ?? data)!,
    };
  }

  return {
    success: true,
    event: mapPaginatedEventResponse(data),
    httpStatus: response.status,
  };
}

export async function deleteEventById(id: string): Promise<DeleteEventResult> {
  const token = getToken();
  try {
    const { data, error, response } = await eventsClient.DELETE('/Events/{id}', {
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
