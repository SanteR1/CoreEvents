# CoreEvents API 📅

Современный RESTful API для управления событиями, построенный на базе **ASP.NET Core 10.0**. Проект демонстрирует использование многослойной архитектуры, паттерна "Репозиторий" и централизованной обработки ошибок.

---

## 🛠 Технологический стек

*  __.NET 10.0__
* **API:** ASP.NET Core Web API
* **DI Container:** Встроенный .NET Dependency Injection
* **Documentation:** Swagger / OpenAPI
* **Testing:** xUnit, Moq
* **Format:** JSON (Problem Details RFC 7807)

---

## 🚦 Как запустить проект (How to Run)

Для запуска приложения вам понадобится установленный **.NET 10.0 SDK**.

### 1. Клонирование репозитория
Откройте терминал и выполните команду:
```sh
git clone https://github.com/SanteR1/CoreEvents.git
cd CoreEvents
dotnet build
dotnet run
```

После запуска в консоли появятся URL-адреса (например, <code>https://localhost:7111</code>).

**Swagger UI** (Интерактивная документация): Перейдите по адресу: <code>https://localhost:{port}/swagger</code>

**Базовый URL API**: <code>https://localhost:{port}/events</code>

---

## 🔬 Как запустить тесты (два варианта):
Нужно находиться в каталоге с проектом (`cd CoreEvents`)
### 1. Запуск всех тестов
Откройте терминал и выполните команду:
```sh
dotnet test
```
### 2. Запуск всех тестов с подробным отчетом
Откройте терминал и выполните команду:
```sh
dotnet test --logger "console;verbosity=detailed"
```
---

## 🚀 API Эндпоинты

| Метод | Путь | Описание | Параметры (Query) | Ответы |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/events` | Получение списка событий | `Page` (номер страницы, **default: 1**),<br> `PageSize` (количество элементов на странице, **default: 10**),<br> `From` (мин. дата),<br> `To` (макс. дата),<br> `Title` (фильтр по названию) | 200 |
| **GET** | `/events/{id}` | Получение события по ID | `id` (GUID события) | 200, 404 |
| **POST** | `/events` | Создание нового события | `body` (JSON: Title, Description, StartAt, EndAt) | 201, 400 |
| **PUT** | `/events/{id}` | Обновление события | `id` (GUID события),<br> `body` (JSON: Title, Description, StartAt, EndAt) | 204, 400, 404 |
| **DELETE** | `/events/{id}` | Удаление события | `id` (GUID события) | 204, 404 |

### Пример запроса **<code>GET /events</code>** с пагинацией и фильтрами

Для получения первой страницы событий с даты 2026-05-25 по 2026-05-25, заголовком "C# Event 11", описанием "Ежегодная встреча разработчиков", с 10 элементами на странице:

```curl
curl -X 'GET' \
  'https://localhost:7111/Events?Title=C%23%20Event%2011&From=2026-05-25&To=2026-05-25&Page=1&PageSize=10' \
  -H 'accept: application/json'
```

**Пример тела ответа:**
```json
{
  "totalEvents": 1,
  "events": [
    {
      "id": "c132de70-de3c-42a5-9576-d9bc4c69421e",
      "title": "C# Event 11",
      "description": "Ежегодная встреча разработчиков",
      "startAt": "2026-05-25T11:00:00",
      "endAt": "2026-05-25T18:00:00"
    }
  ],
  "currentPage": 1,
  "pageSize": 10
}
```
### Пример запроса **<code>GET /events/\{id\}</code>** для получения события по ID

Для получения события с ID "c132de70-de3c-42a5-9576-d9bc4c69421e":

```curl
curl -X 'GET' \
  'https://localhost:7111/Events/c132de70-de3c-42a5-9576-d9bc4c69421e' \
  -H 'accept: application/json'
```

**Пример тела ответа:**

```json
{
  "id": "c132de70-de3c-42a5-9576-d9bc4c69421e",
  "title": "C# Event 11",
  "description": "Ежегодная встреча разработчиков",
  "startAt": "2026-05-25T11:00:00",
  "endAt": "2026-05-25T18:00:00"
}
```

### Пример запроса **<code>POST /events</code>** для создания нового события

Для создания нового cобытия (Event) :

```curl
curl -X 'POST' \
  'https://localhost:7111/Events' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
  "title": "C# Event 11",
  "description": "Ежегодная встреча разработчиков",
  "startAt": "2026-05-25T11:00:00",
  "endAt": "2026-05-25T18:00:00"
}'
```
**Пример тела ответа:**

```json
{
  "id": "c132de70-de3c-42a5-9576-d9bc4c69421e",
  "title": "C# Event 11",
  "description": "Ежегодная встреча разработчиков",
  "startAt": "2026-05-25T11:00:00",
  "endAt": "2026-05-25T18:00:00"
}
```

### Пример запроса **<code>PUT /events/\{id\}</code>** для обновления события

Для обновления cобытия с ID "c132de70-de3c-42a5-9576-d9bc4c69421e":

```curl
curl -X 'PUT' \
  'https://localhost:7111/Events/c132de70-de3c-42a5-9576-d9bc4c69421e' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
  "title": "Updated C# Event 11",
  "description": "Updated Ежегодная встреча разработчиков",
  "startAt": "2026-06-20T15:00:00",
  "endAt": "2026-06-20T20:00:00"
}'
```
**Ответ: 204 No Content (без тела)**


### Пример запроса **<code>DELETE /events/\{id\}</code>** для удаления события

Для удаления cобытия с ID "c132de70-de3c-42a5-9576-d9bc4c69421e":

```curl
curl -X 'DELETE' \
  'https://localhost:7111/Events/c132de70-de3c-42a5-9576-d9bc4c69421e' \
  -H 'accept: application/json'
```

**Ответ: 204 No Content (без тела)**


---

## 🧪 Валидация и ошибки (Data Annotations)

Для обеспечения целостности данных в проекте используются различные модели (DTO) для операций чтения и записи. Ниже приведены правила валидации для основных сценариев.

### 🔍 1. Фильтрация и поиск (GET)
*Все параметры в поисковых запросах являются необязательными (nullable). Если параметр передан, к результатам применяется соответствующий фильтр.*

| Поле | Тип | Обязательно | Описание | Валидация |
| :--- | :--- | :--- | :--- | :--- |
| **Id** | `Guid?` | ❌ Нет | Уникальный ID конкретного события. | `Optional` |
| **Title** | `string?` | ❌ Нет | Поиск по частичному совпадению в заголовке. | `Optional`, `AllowEmptyStrings = false` |
| **From** | `DateTime?` | ❌ Нет | Начальная дата для фильтрации событий. | `Optional` |
| **To** | `DateTime?` | ❌ Нет | Конечная дата для фильтрации событий. | `Optional` |
| **Page** | `int` | ❌ Нет | Номер запрашиваемой страницы. | `Optional`, Default: `1`, Max: `100000` |
| **PageSize** | `int` | ❌ Нет | Количество элементов на странице. | `Optional`, Default: `10`, Max: `100` |

---

### 🆕 2. Создание события (POST)
*Правила для формирования запроса на создание новой записи. Поля, отмеченные как **Required**, обязательны для заполнения.*

| Поле | Тип | Обязательно | Описание | Валидация |
| :--- | :--- | :---: | :--- | :--- |
| **Title** | `string` | ✅ Да | Заголовок события. | `Required` |
| **Description** | `string?` | ❌ Нет | Подробное описание мероприятия. | `Optional` |
| **StartAt** | `DateTime` | ✅ Да | Дата и время начала. | `Required` |
| **EndAt** | `DateTime` | ✅ Да | Дата и время завершения. | `Required`, `EndAt` должен быть `> StartAt` |

## ⚠️ Формат ошибок (Problem Details)
В случае ошибки API возвращает стандартизированный ответ (Problem Details RFC 7807):
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not found",
  "status": 404,
  "detail": "Событие с ID 3fa85f64-5717-4562-b3fc-2c963f66afa6 не найдено.",
  "instance": "/Events/3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "traceId": "00-15823b20f6c84f4c4cf8d49fe4290053-a28986f321187b1b-00"
}
```