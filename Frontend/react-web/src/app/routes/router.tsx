import { createBrowserRouter } from 'react-router';
import { requireAdminLoader, anonymousOnlyLoader, rootLoader } from '@/shared/lib/auth';
import App from '@/app/App';
import { RootErrorBoundary } from '@/shared/ui/error/RootErrorBoundary';
import { RootFallback } from './RootFallback';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <App />,
    loader: rootLoader,
    ErrorBoundary: RootErrorBoundary,
    HydrateFallback: RootFallback,
    children: [
      // 1. Публичные маршруты
      {
        index: true,
        lazy: async () => {
          const { HomePage, loader } = await import('@/pages/events/HomePage');
          return { Component: HomePage, loader };
        },
      },
      {
        path: 'events/topevents',
        lazy: async () => {
          const { TopEventsPage, loader } = await import('@/pages/events/TopEvents');
          return { Component: TopEventsPage, loader };
        },
      },
      {
        path: 'events/:id',
        lazy: async () => {
          const { GetEventByIdPage, loader, action } = await import('@/pages/events/GetEventById');
          return { Component: GetEventByIdPage, loader, action };
        },
      },

      // 2. Маршруты только для гостей
      {
        path: 'login',
        loader: anonymousOnlyLoader,
        lazy: async () => {
          const { LoginPage, action } = await import('@/pages/auth/LoginPage');
          return { Component: LoginPage, action };
        },
      },
      {
        path: 'register',
        loader: anonymousOnlyLoader,
        lazy: async () => {
          const { RegisterPage, action } = await import('@/pages/auth/RegisterPage');
          return { Component: RegisterPage, action };
        },
      },

      // 3. Административные маршруты событий
      {
        path: 'events/create',
        loader: requireAdminLoader,
        lazy: async () => {
          const { CreateEventPage, action } = await import('@/pages/events/CreateEventPage');
          return { Component: CreateEventPage, action };
        },
      },
      {
        path: 'events/:id/edit',
        loader: requireAdminLoader,
        lazy: async () => {
          const { EditEventPage, loader, action } = await import('@/pages/events/EditEventPage');
          return { Component: EditEventPage, loader, action };
        },
      },

      // 4. Защищенные маршруты бронирований
      {
        path: 'bookings',
        children: [
          {
            path: 'create/:eventId',
            lazy: async () => {
              const { CreateBookingPage, loader, action } =
                await import('@/pages/bookings/CreateBookingPage');
              return { Component: CreateBookingPage, loader, action };
            },
          },
          {
            path: ':bookingId',
            lazy: async () => {
              const { GetBookingPage, loader, action } =
                await import('@/pages/bookings/BookingStatusPage');
              return { Component: GetBookingPage, loader, action };
            },
          },
        ],
      },

      // 5. Fallback на случай несуществующего пути (404)
      {
        path: '*',
        lazy: async () => {
          const { NotFoundPage } = await import('@/pages/service/NotFoundPage');
          return { Component: NotFoundPage };
        },
      },
    ],
  },
]);
