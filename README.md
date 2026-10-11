# MDRG-Modding-Toolkit
A suite of modding tools for MDRG: Runtime Texture Injector, Cuddle Injector, Lightswitch Injector &amp; SuperParser SDK.
# 🛠️ MDRG Modding Toolkit / Набор инструментов для моддинга MDRG

> **Professional runtime texture injection & modding suite for MDRG**
> *Профессиональная система инъекции текстур в рантайме и набор инструментов для моддинга MDRG*

**Author / Автор:** Paul.Epifancev (`#warlockforbugs`)  
**Discord:** `anon.jun`  
**Version / Версия:** 1.0.4 (Stable Release / Стабильный релиз)  
**Compatibility / Совместимость:** PC (MelonLoader required / требуется MelonLoader)

---

## 📖 About / О проекте

[EN]
This repository contains a comprehensive suite of tools designed for **artists, modders, and reverse-engineers** working with MDRG. Built from scratch using deep engine analysis (IL2CPP decompilation, UABEA asset dumps, and Unity hierarchy mapping), this toolkit provides unprecedented control over the game's 2D rendering pipeline.

[RU]
Этот репозиторий содержит комплексный набор инструментов для **художников, моддеров и реверс-инженеров**, работающих с MDRG. Создан с нуля на основе глубокого анализа движка (декомпиляция IL2CPP, дампы ассетов через UABEA и картирование иерархии Unity). Набор предоставляет беспрецедентный контроль над двумерным конвейером рендеринга игры.

### ✨ Key Features / Ключевые возможности

- 🎯 Runtime texture injection via clean in-game GUI (F9) / Инъекция текстур в рантайме через чистый внутриигровой интерфейс (F9)
- 🔍 Dynamic scene detection (auto-find nodes without hardcoded paths) / Динамическое обнаружение сцен (авто-поиск нод без жёстко заданных путей)
- 💾 Support for 21 room nodes + 8 isolated scenes / Поддержка 21 ноды комнаты + 8 изолированных сцен
- 🧩 Three specialized plugins for different injection scenarios / Три специализированных плагина для разных сценариев инъекции
- 📚 Complete technical documentation (`MANIFEST.md`) with node atlas, PPU values, and Lua API reference / Полная техническая документация (`MANIFEST.md`) с атласом нод, значениями PPU и справочником Lua API
- 🌐 Bilingual codebase (English/Russian comments) / Двуязычная кодовая база (английские/русские комментарии)

---

## 📦 Included Plugins / Включённые плагины

| Plugin / Плагин | Purpose / Назначение | Target / Цель |
|---|---|---|
| **MDRG Runtime Texture Injector** | Debug & preview tool for instant texture replacement / Отладочный инструмент для мгновенной замены текстур | Room + all isolated scenes / Комната + все изолированные сцены |
| **Cuddle Ultimate Bedroom Injector** | Continuous texture override for the Cuddle Scene / Непрерывное переопределение текстур в сцене обнимашек | Cuddle Scene (3 nodes) / Сцена обнимашек (3 ноды) |
| **Lightswitch Canvas UI Injector** | UI element (`UnityEngine.UI.Image`) texture override / Переопределение текстур UI-элемента | Lightswitch (`LightswitchOn`) / Выключатель (`LightswitchOn`) |

---

## 🚀 Quick Start / Быстрый старт

[EN]
1. Make sure you have [MelonLoader](https://github.com/LavaGang/MelonLoader) installed
2. Download `MDRG_Runtime_Texture_Injector_v1.0.4.dll` from [Releases](../../releases)
3. Place it in your game's `Mods/` folder
4. Launch the game
5. Press **F9** to open the injector GUI
6. Select a node (e.g., `Bed` or `Window`), click "OPEN WINDOWS EXPLORER", choose your PNG
7. Done! Texture injected instantly, no game restart required

[RU]
1. Убедитесь, что у вас установлен [MelonLoader](https://github.com/LavaGang/MelonLoader)
2. Скачайте файл `MDRG_Runtime_Texture_Injector_v1.0.4.dll` из раздела [Releases](../../releases)
3. Поместите его в папку `Mods/` в директории вашей игры
4. Запустите игру
5. Нажмите клавишу **F9** для открытия интерфейса инжектора
6. Выберите нужную ноду (например, `Bed` или `Window`), нажмите кнопку "OPEN WINDOWS EXPLORER" и выберите ваш PNG-файл
7. Готово — текстура заменена мгновенно, без перезапуска игры

---

## 🎥 Video Demonstration / Видео-демонстрация

[EN]
Watch the MDRG Runtime Texture Injector in action. The main video shows all scenes except the Shower Scene (which contains adult content).

[RU]
Смотрите MDRG Runtime Texture Injector в действии. Основное видео показывает все сцены, кроме сцены в душе, которая содержит контент для взрослых.

### 🟢 Main Demo (YouTube - Safe for Work) / Основное демо (YouTube - безопасный контент)

- **Shows / Показано:** Room (21 nodes), Annalie's Clothier Shop, Melissa's Grocery Shop, Cuddle Scene, Missionary Scene, Dynamic View Scenes / Комната (21 нода), магазин Аннали, магазин Мелиссы, сцена обнимашек, миссионерская сцена, динамические сцены
- **Duration / Длительность:** ~5-7 minutes / минут
- **Link / Ссылка:** `[YOUTUBE_LINK]`

### 🔴 Shower Scene Demo (External - NSFW Warning) / Демо сцены в душе (внешний ресурс - предупреждение: контент для взрослых)

- **Shows / Показано:** Texture injection in the Shower Scene (contains adult content) / Инъекция текстур в сцене душа (содержит откровенный контент)
- ⚠️ **WARNING / ПРЕДУПРЕЖДЕНИЕ:** This video contains explicit content (18+) / Данное видео содержит откровенный контент (18+)
- **Platform / Платформа:** GitLab (external hosting / внешний хостинг)

<details>
<summary><b>🔞 Click to expand Shower Scene link (NSFW 18+) / Нажмите, чтобы развернуть ссылку на сцену душа (18+)</b></summary>
<br>
[EN] The Shower Scene demonstration is hosted on an external platform due to content restrictions.
[RU] Демонстрация сцены в душе размещена на внешней платформе в связи с ограничениями на контент.
<br><br>
🔗 <a href="[GITLAB_LINK]" target="_blank">Watch Shower Scene Demo on GitLab / Смотреть демо сцены в душе на GitLab</a>
<br><br>
<em>[EN] This video is intended solely for demonstrating the technical capabilities of the MDRG Runtime Texture Injector. It shows how custom textures can be injected into the Shower Scene nodes (Shower Wall, Shower Back, Shower Back Props, Foreground).</em>
<em>[RU] Это видео предназначено исключительно для демонстрации технических возможностей MDRG Runtime Texture Injector. Оно показывает процесс инъекции пользовательских текстур в ноды сцены душа (Shower Wall, Shower Back, Shower Back Props, Foreground).</em>
</details>

---

## ⚠️ Known Issues / Известные проблемы

### 🔴 Issue #1: Texture Offset in Annalie's Clothier Shop (UNRESOLVED) / Смещение текстур в магазине Аннали (НЕ РЕШЕНО)

[EN]
**Affected scene / Затронутая сцена:** `/Views/Clothier Scene` (Manager ID: `"Clothier"`)  
**Affected nodes / Затронутые ноды:** `Background`, `Blink`, `Halloween Decoration`, `Foreground`, `Halloween Cat`

**Problem / Проблема:**
When injecting custom PNGs into the Clothier Shop scene via the F9 injector, textures may appear **shifted** relative to their original position (usually by several pixels vertically).

**Root cause / Причина:**
This is caused by a **PPU (Pixels Per Unit) mismatch** between the original game sprites and the runtime-injected sprites. The game uses non-standard PPU values:
- `Background`: PPU = **29.860819**, size 1024×708
- `Blink`: PPU = **29.860819**, size 1024×708
- `Halloween Decoration`: PPU = **28.209368**, size 1024×669
- `Halloween Cat`: PPU = **42.737896**, size 256×128
- `Computer` (PC desk): PPU = **47.93725**, size 1024×825

**Current status / Текущий статус:**
⚠️ **UNRESOLVED / НЕ РЕШЕНА** — I have not yet found a solution to this problem, neither through the C# DLL injector nor through the Lua API. Even with correct PPU values extracted from UABEA dumps, the Unity `Sprite.Create()` method doesn't apply them correctly at runtime.

**Workaround / Обходной путь:**
- For **instant preview / мгновенного превью:** Use the F9 injector, but expect some offset. This is fine for quick visual checks.
- For **permanent installation / постоянной установки:** Use the Lua mod approach (see `MANIFEST.md`). The Lua API handles PPU/pivot automatically because it uses the game's native sprite loading pipeline.

**Community help needed / Нужна помощь сообщества:**
If you know how to properly extract and apply PPU/pivot values for runtime sprite creation, or if you've solved this problem in your own mods, please reach out via [Discord](#-community--support--сообщество-и-поддержка) or open an issue on GitHub.

---

### 🔴 Issue #2: Texture Offset in Shower Scene (UNRESOLVED) / Смещение текстур в сцене душа (НЕ РЕШЕНО)

[EN]
**Affected scene / Затронутая сцена:** `/Views/Shower Scene` (Manager ID: `"shower"`)  
**Affected nodes / Затронутые ноды:** `Shower Wall`, `Shower Back`, `Shower Back Props`, `Foreground`

Same root cause as Issue #1. Custom textures injected into the Shower Scene may appear shifted because the original sprites use non-standard PPU/pivot values.

**Visual symptom / Визуальный симптом:**
- The texture appears in the correct location but is offset by a few pixels (usually vertically) / Текстура появляется в правильной области, но смещена на несколько пикселей (обычно по вертикали)
- In some cases, the texture may appear scaled slightly larger or smaller than expected / В некоторых случаях текстура может выглядеть немного больше или меньше ожидаемого размера

**Workaround / Обходной путь:** Same as Issue #1 — use F9 injector for preview, Lua mods for permanent installation / То же, что и в проблеме №1 — F9-инжектор для превью, Lua-моды для постоянной установки.

---

### 📋 Scene Compatibility Table / Таблица совместимости сцен

| Scene / Сцена | Manager ID | Offset Issue? / Проблема смещения? |
|---|---|---|
| Room (Player Bedroom) / Комната | `"Room"` | ❌ No / Нет |
| Clothier Shop (Annalie) / Магазин Аннали | `"Clothier"` | ⚠️ Yes (PPU mismatch) / Да (несоответствие PPU) |
| Changing Room (Annalie Fitting) / Примерочная | `""` | ❌ No / Нет |
| Shower Scene / Сцена в душе | `"shower"` | ⚠️ Yes (PPU mismatch) / Да (несоответствие PPU) |
| Missionary Scene / Миссионерская сцена | `"room_missionary"` | ❌ No / Нет |
| Melissa Grocery Shop / Магазин Мелиссы | `"Shop"` | ❌ No / Нет |
| Cuddle Scene / Сцена обнимашек | *(no manager, direct inject)* | ❌ No / Нет |

---

## 🔧 Specialized Plugins / Специализированные плагины

### Lightswitch Canvas UI Injector (Separate DLL) / Lightswitch Canvas UI Injector (отдельная DLL)

[EN]
The **Lightswitch** (`LightswitchOn`) is a UI element (`UnityEngine.UI.Image`), not a 2D scene sprite. The `TextureOverriderManager` does not support Canvas UI sprites, so the Lightswitch requires a **separate C# DLL plugin** for texture injection.

[RU]
**Выключатель** (`LightswitchOn`) — это UI элемент (`UnityEngine.UI.Image`), а не двумерный спрайт сцены. Компонент `TextureOverriderManager` не поддерживает спрайты Canvas UI, поэтому для замены текстуры выключателя требуется **отдельный C# DLL-плагин**.

- **File / Файл:** `Lightswitch_Canvas_UI_Injector.dll`
- **Status / Статус:** ✅ Fully functional / Полностью работоспособен
- **Compatibility / Совместимость:** PC only (UI injection via MelonLoader) / Только PC (инъекция UI через MelonLoader)

---

### Cuddle Ultimate Bedroom Injector (Separate DLL) / Cuddle Ultimate Bedroom Injector (отдельная DLL)

[EN]
The **Cuddle Scene** does not have a `TextureOverriderManager` attached. Unlike other isolated scenes (Shower, Clothier Shop, etc.), the Cuddle Scene requires **direct C# DLL injection** via a dedicated plugin.

[RU]
**Сцена обнимашек** не имеет прикреплённого компонента `TextureOverriderManager`. В отличие от других изолированных сцен (душ, магазин Аннали и т. д.), сцена обнимашек требует **прямой инъекции через C# DLL** с помощью специализированного плагина.

- **File / Файл:** `Cuddle_Ultimate_Bedroom_Injector.dll`
- **Status / Статус:** ✅ Fully functional / Полностью работоспособен
- **Compatibility / Совместимость:** PC only (direct `SpriteRenderer` injection) / Только PC (прямая инъекция `SpriteRenderer`)

**Why a separate plugin? / Почему отдельный плагин?**
[EN]
- The Cuddle Scene lacks a manager context for Lua-based overrides
- Direct injection bypasses the need for a manager, allowing instant texture replacement
- This plugin loads custom textures from the `Mods/` folder and applies them continuously

[RU]
- Сцена обнимашек не имеет контекста менеджера для Lua-переопределений
- Прямая инъекция обходит эту необходимость, позволяя мгновенно заменять текстуры
- Этот плагин загружает пользовательские текстуры из папки `Mods/` и применяет их непрерывно

> **Note / Примечание:** [EN] All other Lua mods (Room, Shower, Clothier Shop, etc.) work identically on **both PC and Android**. Only the Cuddle Scene and Lightswitch require PC-specific DLL plugins.
> [RU] Все остальные Lua-моды (комната, душ, магазин Аннали и т. д.) работают одинаково как на **PC, так и на Android**. Только сцена обнимашек и выключатель света требуют отдельных DLL-плагинов, совместимых исключительно с PC.

---

## 💬 Community & Support / Сообщество и поддержка

[EN]
Join our Discord server for:
- **Lua mods** (PC & Android compatible)
- **Technical support** and troubleshooting
- **Texture packs** and custom artwork
- **Bug reports** and feature requests
- **Development updates** and early access to new tools

[RU]
Присоединяйтесь к нашему Discord-серверу:
- **Lua-моды** (совместимы с PC и Android)
- **Техническая поддержка** и решение проблем
- **Текстур-паки** и пользовательские арты
- **Отчёты об ошибках** и предложения новых функций
- **Новости разработки** и ранний доступ к новым инструментам

🔗 **Discord Server / Discord-сервер:** [discord.gg/nwcvMUZgM](https://discord.gg/nwcvMUZgM)

---

## 📚 Documentation / Документация

[EN]
For complete technical documentation, including:
- Full atlas of all 21 room nodes with paths and original sizes
- Lighting system mechanics (Morning Light time-lock bypass)
- Isolated scene manager IDs and Lua API usage
- Notable aspect ratios and integration notes

**See: [MANIFEST.md](MANIFEST.md)** — Complete Core Managers Runtime Passport

[RU]
Полная техническая документация доступна в файле **[MANIFEST.md](MANIFEST.md)** и включает:
- Полный атлас всех 21 узлов комнаты с путями и оригинальными размерами
- Механику системы освещения (обход временной блокировки `Morning Light`)
- Идентификаторы менеджеров изолированных сцен и справочник Lua API
- Примечания о соотношениях сторон и особенностях интеграции

---

## 👤 About the Author / Об авторе

[EN]
This project is created by **Paul.Epifancev** (`#warlockforbugs`).

My journey in game modification began with 5 years of deep engine analysis and edge-case testing in World of Warcraft (2013–2018), where I specialized in collision logic, state manipulation, and rendering quirks. Today, I apply the same analytical approach and reverse-engineering skills to Unity/IL2CPP environments.

This toolkit is designed not just to replace textures, but to give creators precise, real-time control over the game's rendering pipeline.

[RU]
Этот проект создан **Paul.Epifancev** (`#warlockforbugs`).

Мой путь в модификации игр начался с пяти лет глубокого анализа движка и тестирования краевых случаев в World of Warcraft (2013–2018), где я специализировался на логике коллизий, манипуляции состояниями и особенностях рендеринга. Сегодня я применяю тот же аналитический подход и навыки реверс-инжиниринга к среде Unity/IL2CPP.

Этот инструментарий создан не просто для замены текстур — он даёт создателям точный контроль над конвейером рендеринга игры в реальном времени.

---

## 📄 License / Лицензия

[EN] This project is released under the MIT License. See [LICENSE](LICENSE) for details.
[RU] Этот проект распространяется под лицензией MIT. Подробности см. в файле [LICENSE](LICENSE).

---

## ⚠️ Disclaimer / Отказ от ответственности

[EN]
This toolkit is provided **as-is** for educational and artistic purposes. It does not modify original game files on disk. All texture changes are applied in RAM and reset on scene reload (unless using Lua mods). Use responsibly.

[RU]
Данный инструментарий предоставляется **«как есть»** в образовательных и художественных целях. Он **не модифицирует** оригинальные файлы игры на диске. Все изменения текстур применяются исключительно в оперативной памяти и сбрасываются при перезагрузке сцены (если не используются Lua-моды). Используйте ответственно.

---

**Made with ❤️ by Paul.Epifancev (`#warlockforbugs`)**  
*"Не пойман — не багер" ©*
