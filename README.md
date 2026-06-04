# 🚚 DeliveryService

Система онлайн доставки заказов. Разработано на C# WPF.

## Возможности

- **Диспетчерская** — интерактивная карта с позициями курьеров и точками доставки в реальном времени
- **Управление заказами** — создание, назначение курьера, отслеживание статусов
- **Курьеры** — список, статус онлайн/офлайн, статистика по выполненным заказам
- **Симуляция доставки** — визуализация движения курьера по маршруту на карте

## Технологии

| Слой | Технология |
|---|---|
| UI | WPF (.NET 8), XAML |
| Паттерн | MVVM |
| БД | PostgreSQL + Entity Framework Core |
| Карта | Yandex Maps API |
| DI | Microsoft.Extensions.DependencyInjection |

## Структура проекта

```
DeliveryService/                          # корень репозитория
├── DeliveryService.slnx
└── DeliveryService/
   ├── App.xaml
    ├── App.xaml.cs                       # DI
├── appsettings.json                     
    ├── Models/                           # сущности БД             
    │
    ├── Data/                             # EF Core
    │   
    │
    ├── Migrations/                       # миграции
    │   
    │
    ├── Repositories/                     # слой доступа к данным
    │  
    │
    ├── Services/                         # бизнес-логика
    │  
    │
    ├── DTO/                              # объекты передачи данных
    │ 
    │
    ├── ViewModels/                       # MVVM
    │   
    │
    ├── Views/                            # XAML-экраны
    │  
    │
    ├── Commands/                         # RelayCommand для MVVM
    │   
    │
    ├── Utils/                            # конвертеры, карта, скрипт
    │  
    │
    ├── Styles/                           # общие стили WPF
    │   
    │
    └── Images/                           # ресурсы меню
       
```

## Запуск

### Требования

- Visual Studio 2022
- .NET 8 SDK
- PostgreSQL 15+
- Процессор (CPU):	2 ядра, 2.0 ГГц
- Оперативная память (RAM):	2 ГБ (для ОС + PostgreSQL)
- Дисковая подсистема (HDD/SSD):	20 ГБ свободного места, HDD (7200 rpm)	SSD NVMe/SATA
- Сеть:	100 Мбит/с, фиксированный IP или DNS-имя
### Установка

1. Клонировать репозиторий:
   ```bash
   git clone https://github.com/aurusxd/DeliveryService
   cd DeliveryService
   ```

2. Создать базу данных в PostgreSQL:
   ```sql
   CREATE DATABASE delivery_db;
   ```

3. Настроить строку подключения в `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "Default": "Host=localhost;Port=5432;Database=delivery_db;Username=postgres;Password=ваш пароль от postgres"
     }
   }
   ```

4. Открыть `DeliveryService.sln` в Visual Studio и запустить (F5)

## Команда

| Участник | Зона ответственности |
|---|---|
| aurusxd | Backend + Frontend + TL|
| DanilKozlov1 | Backend |
| s-k-1-n-1 | Frontend |
| romchik-ww | Frontend |


---
## Регламент восстановления после сбоев БД
| Проблема | Быстрое решение |
|----------|----------------|
| Служба PostgreSQL не запускается | `services.msc` → перезапустить; если ошибка — посмотреть лог в `C:\Program Files\PostgreSQL\15\data\log\` |
| Закончилось место на диске C: | Очистка диска → удалить временные файлы → удалить старые дампы |
| БД повреждена, есть резервная копия | `psql -U postgres -d delivery_db -f backup.sql` |
| Нет резервной копии | Visual Studio → `Update-Database` (восстановит структуру, но данные потеряются) |

> **⚠️ Важно:** При отсутствии резервной копии данные о заказах, курьерах и истории доставок будут безвозвратно утеряны. Настоятельно рекомендуется настроить автоматическое резервное копирование!
---

