# learn-middle-sharp
Для проектов курса Яндекс Практикум "Продвинутая разработка на C# и .NET"

## Спринт 1, проект "Базовый REST API"
Реализовать базовый REST API с CRUD операциями на основе шаблона webapi (ASP.NET Core Web API)

## Как был создан
Запустить из корневой папки команду

	dotnet new webapi --use-program-main -n EventApi

## Как строить
Запустить из корневой папки команду

	dotnet build

## Как запускать
Запустить из корневой папки команду

	dotnet run

## Как тестировать
С помощью Swagger: в браузере перейти по ссылке:
- либо `https://localhost:7001/swagger/index.html`
- либо `http://localhost:5001/swagger/index.html`

## Краткое описание API
Все методы относительно корневого пути [protocol]//[host]:[port]

| HTTP метод | Путь | Параметры | Назначение |
| --- | :--- | :--- | :--- |
| GET | /events | | Возвращает список событий |
| GET | /events/\{id\} | [FromRoute] id: Id события | Возвращает событие по Id |
| POST | /events |  [FromBody] eventDto: структура события | Добавляет новое событие |
| PUT | /events/\{id\} |  [FromRoute] id: Id события, [FromBody] eventDto: структура события | Обновляет событие данными из eventDto |
| DELETE | /events/\{id\} | [FromRoute] id: Id события | Удаляет событие по Id |

Структура события:

	{
	  "title": "string" [required],
	  "description": "string",
	  "startAt": "2026-09-28T16:42:50.411Z" [required],
	  "endAt": "2026-09-28T18:42:50.411Z" [required][must be: endAt > startAt]
	}
