# School Schedule API (Python)

Это RESTful API для системы управления школьным расписанием, разработанное на **FastAPI**. Данное API полностью совместимо с базой данных вашего основного проекта на C# (WPF).

## Основные возможности
- Просмотр и управление списком предметов (`Subjects`).
- Управление данными учителей (`Teachers`).
- Работа с учебными классами (`AcademicClasses`).
- Просмотр и создание расписания уроков (`Lessons`).
- Интеграция с существующей базой данных SQL Server.

## Технологический стек
- **Python 3.10+**
- **FastAPI** (Веб-фреймворк)
- **SQLAlchemy** (ORM для работы с БД)
- **Pydantic** (Валидация данных)
- **pyodbc** (Драйвер для подключения к SQL Server)

## Инструкция по запуску

### 1. Требования
Убедитесь, что у вас установлен Python и драйвер **ODBC Driver 17 for SQL Server** (или новее).

### 2. Установка зависимостей
```bash
pip install -r requirements.txt
```

### 3. Настройка базы данных
Откройте файл `app/core/database.py` и проверьте строку подключения:
```python
SQLALCHEMY_DATABASE_URL = "mssql+pyodbc://(localdb)\\MSSQLLocalDB/School11_Schedule_DB?driver=ODBC+Driver+17+for+SQL+Server&trusted_connection=yes"
```
Если ваша база данных находится на другом сервере или имеет другое имя, обновите этот параметр.

### 4. Запуск API
```bash
python main.py
```
После запуска API будет доступно по адресу: `http://localhost:8000`

### 5. Интерактивная документация
FastAPI автоматически генерирует документацию:
- **Swagger UI**: `http://localhost:8000/docs`
- **ReDoc**: `http://localhost:8000/redoc`

## Структура проекта
- `main.py` — точка входа и описание эндпоинтов.
- `app/models/` — модели базы данных (SQLAlchemy).
- `app/schemas/` — схемы валидации данных (Pydantic).
- `app/core/` — настройки подключения к БД.
