# CoreEvents API 📅

Современный RESTful API для управления событиями, построенный на базе **ASP.NET Core 10.0**. Проект демонстрирует использование многослойной архитектуры, паттерна "Репозиторий" и централизованной обработки ошибок.

---

## 🛠 Технологический стек

*  __.NET 10.0__
* **API:** ASP.NET Core Web API
* **DI Container:** Встроенный .NET Dependency Injection
* **Documentation:** Swagger / OpenAPI
* **Format:** JSON (Problem Details RFC 7807)

---

## 🚦 Как запустить проект (How to Run)

Для запуска приложения вам понадобится установленный **.NET 10.0 SDK** .

### 1. Клонирование репозитория
Откройте терминал и выполните команду:
```
git clone https://github.com/SanteR1/CoreEvents.git
cd CoreEvents
dotnet build
dotnet run
```
После запуска в консоли появятся URL-адреса (например, https://localhost:7111).

**Swagger UI** (Интерактивная документация): Перейдите по адресу: https://localhost:{порт}/swagger

**Базовый URL API**: https://localhost:{порт}/api/events

---

## 🚀 API Эндпоинты

| Метод | Путь | Описание | Ответы |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/events` | Получить все события | 200 |
| **GET** | `/api/events/{id}` | Получить событие по ID | 200, 404 |
| **POST** | `/api/events` | Создать новое событие | 201, 400 |
| **PUT** | `/api/events/{id}` | Обновить событие | 204, 400, 404 |
| **DELETE** | `/api/events/{id}` | Удалить событие | 204, 404 |

---

## 🧪 Валидация и ошибки

### Пример валидации (Data Annotations)
Для входящих данных используются атрибуты:
* `[Required]` — обязательное поле.
* `[StringLength]` — ограничение длины текста.
* `[Range]` — проверка диапазона дат.

### Формат ошибок (Problem Details)
В случае ошибки API возвращает стандартизированный ответ:
```json
{
  "title": "Not found",
  "status": 404,
  "detail": "Событие не найдено.",
  "instance": "/api/events/550e8400-e29b-41d4-a716-446655440000"
}