# Сторонние компоненты

MapHints использует BepInEx, Il2CppInterop и интерфейсы Unity из собственной установки пользователя. Их двоичные файлы не входят в репозиторий и установочный ZIP MapHints.

При отсутствии нужных файлов установщик скачивает закреплённые зависимости с серверов проекта BepInEx:

- [BepInEx 6.0.0-be.788](https://builds.bepinex.dev/projects/bepinex_be), исходники [5b766a3](https://github.com/BepInEx/BepInEx/tree/5b766a3), [лицензия BepInEx](https://github.com/BepInEx/BepInEx/blob/5b766a3/LICENSE).
- [Базовые библиотеки Unity 6000.0.66](https://unity.bepinex.dev/libraries/6000.0.66.zip) для подготовки IL2CPP interop.

Адреса и SHA-256 зафиксированы в `release-settings.json`. Компоненты сохраняют условия своих правообладателей. MapHints не присваивает им собственную лицензию.

Проект является неофициальным модом для Heroes of Might and Magic: Olden Era. Игровые ресурсы отдельно не распространяются.

Лицензия на собственный код MapHints пока не выбрана автором.
