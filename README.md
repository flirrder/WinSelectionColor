# WinSelectionColor

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D7?style=flat-square&logo=windows" alt="Platform: Windows 10/11" />
  <img src="https://img.shields.io/badge/Framework-.NET%20Framework%204.0%2B-512BD4?style=flat-square" alt=".NET 4.0+" />
  <img src="https://img.shields.io/badge/Binary%20Size-~38%20KB-22C55E?style=flat-square" alt="Size: 38 KB" />
  <img src="https://img.shields.io/badge/Dependencies-Zero-blue?style=flat-square" alt="No Dependencies" />
  <img src="https://img.shields.io/badge/License-MIT-green.svg?style=flat-square" alt="License: MIT" />
</p>

<p align="center">
  <b>Легковесная (~38 КБ), портативная утилита для изменения цвета системного выделения в Windows 10 и 11 с экранной пипеткой под цвет обоев.</b><br>
  <i>A lightweight (~38 KB) portable utility to customize the Windows 10/11 selection highlight color with a built-in screen eyedropper.</i>
</p>

---
<img width="664" height="811" alt="изображение" src="https://github.com/user-attachments/assets/652c2e08-a326-4d1e-8fef-1da44b439588" />

## 🇷🇺 Описание на русском

**WinSelectionColor** позволяет в пару кликов изменить стандартный синий цвет рамки выделения на Рабочем столе, в Проводнике и текстовых полях на любой оттенок, гармонирующий с вашими обоями.

### 🌟 Основные возможности

1. **Экранная пипетка со встроенной лупой**:
   - Полноэкранный режим захвата без мерцания.
   - Увеличительная лупа (сетка 11x11 пикселей с перекрестием в центре), следующая за курсором.
   - Отображение точного образца цвета, HEX (`#RRGGBB`) и RGB-составляющих в реальном времени.
   - **Управление**:
     - `ЛКМ` / `Пробел` / `Enter` — зафиксировать цвет.
     - `Колесико мыши` — масштаб зума пикселей.
     - `Стрелки клавиатуры` — попиксельная микро-доводка курсора.
     - `Esc` или `ПКМ` — отмена.
     - Поддержка конфигураций из нескольких мониторов (`VirtualScreen`).

2. **Автоматическая палитра из обоев рабочего стола**:
   - Автоматически находит файл текущих обоев Windows.
   - Извлекает доминирующие и гармоничные оттенки в виде кликабельных образцов (свотчей). Выбор цвета в один клик.

3. **Интерактивный предварительный просмотр (Live Preview)**:
   - **Рамка выделения**: реалистичная имитация полупрозрачного прямоугольника выделения на Рабочем столе поверх обоев.
   - **Текст**: предпросмотр выделения текста (как в Блокноте и Проводнике).
   - **Авто-контраст**: программа сама подбирает белый или черный цвет текста в зависимости от яркости фона, чтобы текст оставался читаемым.

4. **Тонкая настройка**:
   - Ползунки R, G, B без лишних засечек для плавной регулировки.
   - Поле ввода и копирования HEX-кода.
   - Кнопка системной расширенной палитры Windows («Палитра...»).

5. **Синхронизация системного акцента Windows**:
   - Опция **«Также применить как системный акцент Windows»**: позволяет в один клик окрасить не только выделение, но и системную тему Windows (меню «Пуск», панель задач, плитки, границы окон DWM).

6. **Безопасность и применение в системе**:
   - **«✔ Применить цвет выделения»** — запись в системный реестр (`HKCU\Control Panel\Colors`) и вызов Win32 API `SetSysColors`.
   - **«🔄 Перезапустить Проводник»** — мягкий перезапуск `explorer.exe` прямо из программы (~1 секунда), чтобы новая рамка сразу применилась на Рабочем столе без перезагрузки ПК или выхода из системы.
   - **«↩ Сбросить по умолчанию»** — моментальный возврат к заводскому синему цвету Windows 10 (`#0078D7`, RGB `0 120 215`).
   - Автоматическое создание резервной копии исходных цветов в `selection_color_backup.ini` при первом запуске.

---

### 🚀 Быстрый старт

#### Вариант 1: Сборка в один клик (без сторонних программ)
В Windows 10/11 уже встроен компилятор C# (`csc.exe`). Запустите в папке проекта:
```cmd
build.bat
```
Скрипт за пару секунд создаст готовый исполняемый файл `WinSelectionColor.exe` (~38 КБ).

#### Вариант 2: Запуск
Дважды кликните по `WinSelectionColor.exe`.

---

## 🇬🇧 English Summary

**WinSelectionColor** is an ultra-lightweight (~38 KB), dependency-free Windows application designed to customize the system selection box and highlight colors in Windows 10 and 11.

### Features:
- **Screen Eyedropper & Loupe**: Sample any pixel from your desktop, wallpaper, or any window with an 11x11 magnified grid and crosshair reticle.
- **Auto Wallpaper Palette**: Automatically reads your current desktop wallpaper and suggests dominant accent swatches.
- **Live Preview**: Real-time mockup of both the translucent marquee selection rectangle and highlighted text.
- **Automatic Text Contrast**: Dynamically switches text between white and dark for maximum readability.
- **System Integration**: Updates `HKCU\Control Panel\Colors` (`Hilight`, `HotTrackingColor`, `HilightText`, `MenuHilight`) and calls Win32 `SetSysColors`.
- **One-Click Explorer Restart**: Instantly refreshes Desktop & Explorer without signing out or rebooting.
- **Safety**: Factory reset button to restore default Windows 10 blue (`#0078D7`) + automatic initial backup.

### Build from source:
Simply run `build.bat` on any Windows 10/11 machine. It uses the built-in `.NET Framework` compiler (`csc.exe`). No Visual Studio or external SDK needed.

---

## 📁 Структура проекта / Project Structure

```
├── src/
│   ├── Program.cs          # Точка входа, High-DPI awareness
│   ├── MainForm.cs         # Главное окно, предпросмотр, палитра, слайдеры
│   ├── EyedropperForm.cs   # Полноэкранная экранная пипетка со встроенной лупой
│   ├── ColorRegistry.cs    # Реестр Windows, Win32 SetSysColors, перезапуск Explorer
│   └── WallpaperHelper.cs  # Чтение обоев и алгоритм извлечения палитры
├── app.manifest            # Манифест Windows (High-DPI Per-Monitor V2, Common Controls v6)
├── build.bat               # Скрипт сборки через встроенный csc.exe
├── .gitignore              # Исключение временных файлов и бинарников
├── LICENSE                 # MIT License
└── README.md               # Документация (RU / EN)
```

---

## ⚖️ Лицензия / License

Распространяется под лицензией **MIT**. Подробнее см. в файле [LICENSE](LICENSE).
