# CoreEvents Web — React SPA Client

Современное одностраничное веб-приложение (Single Page Application) для платформы **CoreEvents**, реализующее пользовательский интерфейс для управления событиями, бронирования мест и авторизации.

---

## 📑 Содержание (Table of Contents)

1. [Ключевые возможности (Key Features)](#-ключевые-возможности-key-features)
2. [Технологический стек (Tech Stack)](#-технологический-стек-tech-stack)
3. [Архитектура проекта (Architecture Overview)](#-архитектура-проекта-architecture-overview)
4. [Переменные окружения (Environment Variables)](#-переменные-окружения-environment-variables)
5. [Специфика запуска: Local vs Dev Containers](#-специфика-запуска-local-vs-dev-containers)
6. [Генерация OpenAPI-клиентов и типов (Code Generation)](#-генерация-openapi-клиентов-и-типов-code-generation)
7. [Быстрый старт и NPM-скрипты (Getting Started & Scripts)](#-быстрый-старт-и-npm-скрипты-getting-started--scripts)
8. [Качество кода и тестирование (Quality Assurance)](#-качество-кода-и-тестирование-quality-assurance)
9. [Дорожная карта (Roadmap)](#-дорожная-карта-roadmap)

---

## 🚀 Ключевые возможности (Key Features)

- **Аутентификация и авторизация (Auth & Sessions)**:
  - Вход (`LoginForm`) и регистрация (`RegisterForm`) пользователей.
  - Работа с JWT-токенами, автоматическое декодирование полезной нагрузки (`claims`, `roles`), безопасное хранение в `localStorage`.
  - Защищённые маршруты (`RequireAuth`, `RedirectIfAuthenticated`) с поддержкой безопасного возврата пользователя на исходную страницу (`returnUrl`).
- **Каталог событий (Events Management)**:
  - Список мероприятий с карточками событий (`EventCard`), фильтрацией по статусам и категориям (`EventFilters`).
  - Пагинация страниц со сложным алгоритмом отображения страниц и эллипсисов (`EventPagination`).
  - Создание (`CreateEventPage`), редактирование (`EditEventPage`), просмотр подробной информации (`GetEventById`) и витрина популярных событий (`TopEvents`).
- **Бронирование билетов (Bookings System)**:
  - Создание бронирования с динамическим выбором количества мест, строгой валидацией граничных значений (`BookingCreateForm`).
  - Страница статуса заказа (`BookingStatusPage`) с фоновым поллингом статуса (`Pending` $\to$ `Confirmed` / `Rejected`) и ревалидацией данных.
- **UI/UX и Доступность (A11y & Theme)**:
  - Полная поддержка тёмной и светлой темы (`ThemeProvider`, `ThemeToggle`) с синхронизацией в `localStorage`.
  - Адаптивная вёрстка на Tailwind CSS v4.
  - Доступность интерфейса: соответствие стандартам WCAG 2.1 AA / WAI-ARIA (протестировано через `axe-core`).
- **Централизованная обработка ошибок (Error Handling)**:
  - Парсер спецификации RFC 7807 (ProblemDetails / ValidationProblemDetails) для прозрачного маппинга серверных ошибок бэкенда на поля форм.
  - Глобальный перехватчик сбоев (`RootErrorBoundary`) с дружелюбными экранами 401, 404, 500.

---

## 🛠 Технологический стек (Tech Stack)

- **UI Framework**: [React 19](https://react.dev/) (с поддержкой React Compiler)
- **Язык программирования**: [TypeScript 6.0](https://www.typescriptlang.org/)
- **Сборщик и Dev-сервер**: [Vite 8](https://vite.dev/)
- **Маршрутизация**: [React Router v8](https://reactrouter.com/) (Data Router: `createBrowserRouter`, `loaders`, `actions`)
- **Стилизация**: [Tailwind CSS v4](https://tailwindcss.com/) (`@tailwindcss/vite`)
- **Типобезопасный API-клиент**: [openapi-fetch](https://openapi-ts.dev/openapi-fetch/) и [openapi-typescript](https://openapi-ts.dev/openapi-typescript/)
- **Тестирование**: [Vitest 5](https://vitest.dev/), [React Testing Library](https://testing-library.com/), [jest-dom](https://github.com/testing-library/jest-dom), [axe-core](https://github.com/dequelabs/axe-core)
- **Анализ покрытия**: [@vitest/coverage-v8](https://vitest.dev/guide/coverage.html)
- **Линтинг и форматирование**: [ESLint 10](https://eslint.org/) (Flat Config, `@typescript-eslint/strict-type-checked`), [Prettier 3](https://prettier.io/)

---

## 📂 Архитектура проекта (Architecture Overview)

Структура исходного кода организована по принципам **Feature-Sliced Design (FSD)**:

```
src/
├── app/                  # Инициализация приложения, глобальные провайдеры и роутер
│   ├── providers/        # ThemeProvider, ThemeContext
│   ├── routes/           # router.tsx, loaders.ts, RootFallback.tsx
│   ├── App.tsx           # Корневой лейаут (Header + Outlet)
│   └── main.tsx          # Точка входа React 19 (createRoot)
├── features/             # Бизнес-фичи приложения
│   ├── auth/             # API, формы логина/регистрации, хуки useAuth/useToken
│   ├── bookings/         # API бронирования, форма оформления, детали заказа
│   └── events/           # API событий, карточки, формы создания/редактирования, пагинация
├── pages/                # Компоненты страниц маршрутизатора
│   ├── auth/             # LoginPage, RegisterPage
│   ├── bookings/         # CreateBookingPage, BookingStatusPage
│   ├── events/           # HomePage, CreateEventPage, EditEventPage, GetEventById, TopEvents
│   └── service/          # NotFoundPage
├── shared/               # Переиспользуемый код и инфраструктурные модули
│   ├── api/              # Клиенты openapi-fetch (users, events, bookings), ProblemDetails парсер
│   │   ├── clients/      # usersClient, eventsClient, bookingsClient (с interceptor для JWT)
│   │   └── generated/    # Автогенерируемые TypeScript d.ts типы из Swagger
│   ├── lib/              # Утилиты авторизации (sessionStore, authGuards), темы и тестирования
│   └── ui/               # Базовые UI-компоненты (ThemeToggle, RootErrorBoundary)
└── widgets/              # Композиционные блоки интерфейса
    └── header/           # Навигационная панель (Header)
```

---

## ⚙️ Переменные окружения (Environment Variables)

Для взаимодействия клиента с бэкендом используется единая точка входа — **API Gateway**:

| Переменная     | Назначение                                                                     | Значение по умолчанию (Fallback) |
| :------------- | :----------------------------------------------------------------------------- | :------------------------------- |
| `VITE_API_URL` | Базовый URL API Gateway (единая точка входа для всех запросов к микросервисам) | `http://localhost:5000/v1`       |

### Безопасные значения по умолчанию (Safe Fallbacks)

Все клиенты в `src/shared/api/clients/` используют цепочку операторов `??` для безопасного определения URL:

```typescript
const API_URL =
  import.meta.env.VITE_API_URL ??
  import.meta.env.VITE_API_GATEWAY_URL ??
  import.meta.env.VITE_ < SERVICE > _API_URL ??
  'http://localhost:5000/v1';
```

- По умолчанию все запросы маршрутизируются через шлюз на `http://localhost:5000/v1`.
- Если файл `.env` отсутствует (например, в среде CI или при быстром запуске), приложение не падает с ошибкой, а автоматически использует дефолтный адрес шлюза.
- Для локальной отладки конкретного микросервиса сохранена обратная совместимость: можно точечно переопределить адрес через `VITE_USERS_API_URL`, `VITE_EVENTS_API_URL` или `VITE_BOOKINGS_API_URL`.

Для настройки скопируйте шаблон:

```bash
cp .env.example .env
```

---

## 🐳 Специфика запуска: Local vs Dev Containers

Разработка проекта может вестись как локально на хост-машине, так и внутри изолированного контейнера **Dev Containers (VS Code / Docker)**.

### 1. Доступность Dev-сервера (`--host`)

В `package.json` скрипт запуска настроен как:

```json
"dev": "vite --host"
```

Флаг `--host` привязывает сервер Vite к интерфейсу `0.0.0.0`, благодаря чему запущенное приложение на порту `5173` автоматически пробрасывается наружу и доступно из браузера хост-машины при разработке в Dev Container или WSL2.

### 2. Адресация бэкенда: `localhost:5000` vs `host.docker.internal:5000`

- **В браузере пользователя**: запросы к API отправляются на единый порт шлюза **`http://localhost:5000/v1`**, который проброшен из Docker на хост-машину.
- **Внутри Dev Container**: контейнеру для обращения к запущенному шлюзу при генерации типов из схем OpenAPI требуется доменное имя Docker-моста: `http://host.docker.internal:5000` или внутреннее имя сервиса `http://gateway.api:5000`.

---

## 🔄 Генерация OpenAPI-клиентов и типов (Code Generation)

Типы TypeScript генерируются автоматически напрямую из Swagger/OpenAPI спецификаций запущенных ASP.NET Core микросервисов с помощью `openapi-typescript`:

```bash
# Генерация типов для Users Service (порт 5003)
npm run gen:users

# Генерация типов для Events Service (порт 5004)
npm run gen:events

# Генерация типов для Bookings Service (порт 5005)
npm run gen:bookings

# Сгенерировать все типы одновременно
npm run gen:all
```

Сгенерированные файлы сохраняются в `src/shared/api/generated/*.d.ts` и импортируются в клиенты `openapi-fetch` для 100% статической типизации эндпоинтов, тела запросов, параметров и ответов.

---

## 💻 Быстрый старт и NPM-скрипты (Getting Started & Scripts)

### Установка зависимостей

```bash
npm install
# или при строгой установке по package-lock.json:
npm ci --legacy-peer-deps
```

### Доступные скрипты

| Команда                 | Описание                                                                         |
| :---------------------- | :------------------------------------------------------------------------------- |
| `npm run dev`           | Запуск сервера разработки Vite (`--host`) на `http://localhost:5173`             |
| `npm run build`         | Компиляция TypeScript (`tsc -b`) и сборка production-бандла в директорию `dist/` |
| `npm run preview`       | Локальный запуск production-сборки для проверки                                  |
| `npm run typecheck`     | Проверка типов TypeScript без вывода файлов (`tsc -b --noEmit`)                  |
| `npm run lint`          | Статический анализ кода с помощью ESLint 10                                      |
| `npm run format`        | Автоформатирование кода с помощью Prettier                                       |
| `npm run format:check`  | Проверка соответствия код-стайлу Prettier                                        |
| `npm test`              | Быстрый однократный запуск модульных тестов Vitest                               |
| `npm run test:watch`    | Интерактивный запуск тестов в режиме отслеживания изменений                      |
| `npm run test:coverage` | Запуск тестов со сбором полного отчёта покрытия V8 и проверкой порогов           |

---

## 🛡 Качество кода и тестирование (Quality Assurance)

В проекте настроен строгий контроль качества. Любое изменение валидируется через **5-ступенчатый пайплайн качества**, который запускается как локально перед коммитом, так и в CI GitHub Actions (`frontend-ci.yml`):

$$\text{1. format:check} \longrightarrow \text{2. typecheck} \longrightarrow \text{3. lint} \longrightarrow \text{4. test:coverage} \longrightarrow \text{5. build}$$

```bash
# Полный пайплайн одной командой:
npm run format:check && npm run typecheck && npm run lint && npm run test:coverage && npm run build
```

### Тестовое покрытие (100% Absolute Coverage)

В конфигурации `vitest.config.ts` установлены строгие пороги покрытия кода. На данный момент проект покрыт тестами на **абсолютные 100%**:

| Метрика покрытия      | Текущий результат | Порог качества |       Статус       |
| :-------------------- | :---------------: | :------------: | :----------------: |
| **Lines**             |    **100.00%**    |  $\ge$ 98.00%  | ✅ **PASS (100%)** |
| **Functions**         |    **100.00%**    |  $\ge$ 98.00%  | ✅ **PASS (100%)** |
| **Statements**        |    **100.00%**    |  $\ge$ 98.00%  | ✅ **PASS (100%)** |
| **Branches**          |    **100.00%**    |  $\ge$ 95.00%  | ✅ **PASS (100%)** |
| **Тестовые файлы**    |    **37 / 37**    |       —        | ✅ **100% passed** |
| **Количество тестов** |   **425 / 425**   |       —        | ✅ **100% passed** |

### Аудит доступности (Accessibility / WCAG)

Все интерактивные компоненты, формы, карточки событий и диалоги верифицируются тестами доступности с использованием движка **axe-core** (`expect(await axe(container)).toHaveNoViolations()`), что гарантирует доступность интерфейса для пользователей скринридеров и соответствие стандартам WCAG 2.1 AA.

---

## 🗺 Дорожная карта (Roadmap)

Развитие архитектуры фронтенда, ролевой модели и тестирования:

- [x] **Разработка React SPA**: Каталог событий, оформление бронирования, авторизация, темы, FSD-архитектура.
- [x] **100% Unit, Integration & A11y Coverage**: Доведение покрытия до 100% по всем метрикам (Lines, Functions, Statements, Branches).
- [x] **Автоматизация CI/CD**: Пайплайн GitHub Actions для автоматической проверки PR (`frontend-ci.yml`).
- [x] **Подключение к API Gateway**: Перевод клиентов `openapi-fetch` на единый базовый URL шлюза (`VITE_API_URL=http://localhost:5000/v1`) с сохранением гибких fallback-адресов.
- [ ] **Этап 1. Безопасные сессии и ролевая модель (RBAC)**:
  - **Безопасность сессий (No-JWT Architecture)**:
    - Отказ от хранения JWT в `localStorage` и декодирования токенов в JS в пользу автоматических HttpOnly кук браузера (`credentials: 'include'`).
    - Инициализация сессии через эндпоинт `GET /v1/users/me` при старте приложения (Root Loader).
    - Хук `useAuth()` оперирует типизированным объектом `User { id, userName, role }` и флагами `isAuthenticated`, `isAdmin`.
    - Механизм Silent Refresh: перехватчик `401 Unauthorized` в `openapi-fetch` для фонового вызова `POST /api/auth/refresh` и бесшовного повтора оригинального запроса.
  - **Ролевое разграничение (UX & Client Guards)**:
    - Защита административных маршрутов (`/events/create`, `/events/:id/edit`) через `requireAdminLoader` с проверкой роли `Admin`.
    - Условный рендеринг кнопок создания («Создать событие»), редактирования («Редактировать») и удаления («Удалить») только для пользователей с ролью `Admin`.
    - Проверка прав на отмену бронирования (автор бронирования или администратор).
  - **Тестирование**:
    - Актуализация тестов авторизации, моков и ролевых гардов с сохранением **100% покрытия** (Lines, Branches, Functions, Statements).
- [ ] **Этап 2. Сквозное E2E-тестирование (Playwright)**:
  - Подключение и настройка Playwright для тестирования в реальных браузерах (Chromium, Firefox, WebKit).
  - Сквозные сценарии:
    - Гость: просмотр каталога, валидация редиректов на `/login` при попытке доступа к защищённым разделам.
    - Пользователь (`User`): авторизация с установкой `HttpOnly` Cookies $\to$ попытка зайти в админку (403) $\to$ бронирование события $\to$ отмена своей брони.
    - Администратор (`Admin`): вход $\to$ отображение кнопок управления $\to$ создание, редактирование и удаление мероприятия $\to$ отмена любой брони.
    - Проверка бесшовного обновления сессии (Silent Refresh) при истечении `access_token`.
  - Интеграция E2E-тестов в CI пайплайн GitHub Actions.
