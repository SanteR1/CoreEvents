// Клиенты
export { usersClient } from './clients/usersClient';
export { eventsClient } from './clients/eventsClient';
export { bookingsClient } from './clients/bookingsClient';

// export type * as UsersApiTypes from './generated/users';
// export type * as EventsApiTypes from './generated/events';
// export type * as BookingsApiTypes from './generated/bookings';

// Хелперы типов для удобного импорта DTO
import type { components as UsersComponents } from './generated/users';
import type { components as EventsComponents } from './generated/events';
import type { components as BookingsComponents } from './generated/bookings';

// Хелпер для быстрого получения схем Users API
export type UserSchema<T extends keyof UsersComponents['schemas']> = UsersComponents['schemas'][T];
// Хелпер для быстрого получения схем Events API
export type EventSchema<T extends keyof EventsComponents['schemas']> =
  EventsComponents['schemas'][T];
// Хелпер для быстрого получения схем Bookings API
export type BookingSchema<T extends keyof BookingsComponents['schemas']> =
  BookingsComponents['schemas'][T];
