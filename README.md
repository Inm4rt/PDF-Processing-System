# PDF Processing System

Микросервисная система для загрузки PDF-документов и асинхронного извлечения текста.

## Архитектура

- **API Gateway** (ASP.NET Core) – загрузка PDF, получение списка документов, получение извлечённого текста.
- **Background Worker** (Worker Service) – потребление сообщений из RabbitMQ, извлечение текста (PdfPig), обновление статуса в PostgreSQL.
- **RabbitMQ** – брокер очередей.
- **PostgreSQL** – хранение файлов, статусов и извлечённого текста.

## Технологии

C# / .NET 8, ASP.NET Core, Entity Framework Core, PostgreSQL, RabbitMQ, Docker, Serilog, PdfPig.


## Основные эндпоинты API

- `POST /api/documents` – загрузка PDF файла (multipart/form-data)
- `GET /api/documents` – список всех документов
- `GET /api/documents/{id}/text` – извлечённый текст документа (после обработки)
