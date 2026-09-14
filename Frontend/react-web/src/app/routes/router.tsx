import { createBrowserRouter } from 'react-router';
import { requireAuthLoader, anonymousOnlyLoader } from '@/shared/lib/auth';

// Импорт страниц
import { HomePage, loader as homeLoader } from '@/pages/events/HomePage';
import { LoginPage, action as loginAction } from '@/pages/auth/LoginPage';
import { CreateEventPage, action as createEventAction } from '@/pages/events/CreateEventPage';
import {
  CreateBookingPage,
  action as createBookingAction,
  loader as createBookingLoader,
} from '@/pages/bookings/CreateBookingPage';
import {
  GetBookingPage,
  loader as bookingLoader,
  action as bookingAction,
} from '@/pages/bookings/BookingStatusPage';
import { TopEventsPage, loader as topEventsLoader } from '@/pages/events/TopEvents';
import App from '@/app/App';
import { RegisterPage, action as registerAction } from '@/pages/auth/RegisterPage';
import {
  GetEventByIdPage,
  loader as eventDetailsLoader,
  action as deleteEventAction,
} from '@/pages/events/GetEventById';
import {
  EditEventPage,
  loader as editEventLoader,
  action as editEventAction,
} from '@/pages/events/EditEventPage';
import { RootErrorBoundary } from '@/shared/ui/error/RootErrorBoundary';
import { NotFoundPage } from '@/pages/service/NotFoundPage';

import { RootFallback } from './RootFallback';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <App />,
    ErrorBoundary: RootErrorBoundary,
    HydrateFallback: RootFallback,
    children: [
      // 1. Публичные маршруты
      {
        index: true,
        element: <HomePage />,
        loader: homeLoader,
      },
      {
        path: 'events/topevents',
        element: <TopEventsPage />,
        loader: topEventsLoader,
      },
      {
        path: 'events/:id',
        element: <GetEventByIdPage />,
        loader: eventDetailsLoader,
        action: deleteEventAction,
      },

      // 2. Маршруты только для гостей
      {
        path: 'login',
        element: <LoginPage />,
        loader: anonymousOnlyLoader,
        action: loginAction,
      },
      {
        path: 'register',
        element: <RegisterPage />,
        loader: anonymousOnlyLoader,
        action: registerAction,
      },

      // 3. Защищенные маршруты событий
      {
        path: 'events/create',
        element: <CreateEventPage />,
        loader: requireAuthLoader,
        action: createEventAction,
      },
      {
        path: 'events/:id/edit',
        element: <EditEventPage />,
        loader: editEventLoader,
        action: editEventAction,
      },

      // 4. Защищенные маршруты бронирований
      {
        path: 'bookings',
        children: [
          {
            path: 'create/:eventId',
            element: <CreateBookingPage />,
            action: createBookingAction,
            loader: createBookingLoader,
          },
          {
            path: ':bookingId',
            element: <GetBookingPage />,
            action: bookingAction,
            loader: bookingLoader,
          },
        ],
      },

      // 5. Fallback на случай несуществующего пути (404)
      {
        path: '*',
        element: <NotFoundPage />,
      },
    ],
  },
]);
