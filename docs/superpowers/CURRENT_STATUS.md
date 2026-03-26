# Makeup Mechanic — Current Status

## Что сделано

### Код (все скрипты написаны и закоммичены)

| Файл | Статус |
|------|--------|
| `Assets/Scripts/Data/CosmeticType.cs` | Done |
| `Assets/Scripts/Data/ICosmetic.cs` | Done |
| `Assets/Scripts/Data/CosmeticItemSO.cs` | Done |
| `Assets/Scripts/Data/LevelConfigSO.cs` | Done |
| `Assets/Scripts/Infrastructure/ResourceManager.cs` | Done |
| `Assets/Scripts/UI/CosmeticItem.cs` | Done |
| `Assets/Scripts/UI/TabButton.cs` | Done |
| `Assets/Scripts/Systems/CosmeticContainer.cs` | Done (обновлён: leftPage/rightPage) |
| `Assets/Scripts/Systems/DragPanelHandler.cs` | Done |
| `Assets/Scripts/Systems/DragSystem.cs` | Done |
| `Assets/Scripts/Systems/CharacterMakeupHandler.cs` | Done |
| `Assets/Scripts/Core/Orchestrator.cs` | Done (обновлён: _canvasRect поле) |
| `Assets/Scripts/Editor/CosmeticAssetCreator.cs` | Done |

### Ассеты
- Editor скрипт для создания SO — готов (`Tools → Create Cosmetic Assets`)
- SO ассеты и LevelConfig — **нужно подтвердить что созданы**
- Префабы CosmeticCircle / LipstickItem — **созданы пользователем**

## Что осталось — Scene Wiring

MCP нужно переподключить на порт `http://localhost:21858` (playnera-test). После этого прокинуть SerializeField:

### Orchestrator (на Canvas)
| Поле | Привязка |
|------|----------|
| `_character` | GO с CharacterMakeupHandler |
| `_dragSystem` | GO с DragSystem |
| `_containers` | массив из 3 CosmeticContainer (Eyeshadow, Blush, Lipstick) |
| `_tabs` | массив из 3 TabButton |
| `_spongeButton` | Button на спонжике |
| `_creamButton` | Button на креме |
| `_canvasRect` | RectTransform самого Canvas |
| `_levelConfigPath` | `"LevelConfig"` (дефолт) |

### DragSystem
| Поле | Привязка |
|------|----------|
| `_dragPanel` | RectTransform DragPanel (Image alpha=0) |
| `_eyeBrush` | RectTransform кисти теней (выключена) |
| `_blushBrush` | RectTransform кисти румян (выключена) |
| `_faceZone` | RectTransform FaceZone (child персонажа) |
| `_canvas` | Canvas |
| `_dragPanelHandler` | DragPanelHandler на DragPanel |

### DragPanel GO (нужно создать если нет)
- Image: alpha=0, Raycast Target=true
- DragPanelHandler компонент
- RectTransform: stretch на весь Canvas
- SetActive: **false** по умолчанию

### CharacterMakeupHandler (на персонаже)
| Поле | Привязка |
|------|----------|
| `_eyeshadowLayer` | Image тени (child персонажа, выключен) |
| `_blushLayer` | Image румяна (child персонажа, выключен) |
| `_lipstickLayer` | Image помада (child персонажа, выключен) |
| `_acne` | GO прыщей |

### CosmeticContainer (×3: Eyeshadow, Blush, Lipstick)
| Поле | Привязка |
|------|----------|
| `_type` | enum (Eyeshadow / Blush / Lipstick) |
| `_itemPrefab` | CosmeticCircle или LipstickItem префаб |
| `_leftPage` | Transform левой страницы (GridLayout) |
| `_rightPage` | Transform правой страницы (GridLayout) |
| `_itemsPerPage` | колонки × ряды на одной странице |

### TabButton (×3)
| Поле | Привязка |
|------|----------|
| `_toggle` | Toggle на этом же GO |
| `_contentType` | enum (Eyeshadow / Blush / Lipstick) |

### Tabs parent GO
- ToggleGroup компонент, первый таб `isOn = true`

### FaceZone (нужно создать если нет)
- Empty GO, child персонажа
- RectTransform sized to face area

## После wiring

1. **Integration Testing** — Play Mode, проверить все флоу
2. **APK Build** — Android билд
3. **README** — описание технических решений

## Документация

- Спека: `docs/superpowers/specs/2026-03-25-makeup-mechanic-design.md`
- План: `docs/superpowers/plans/2026-03-25-makeup-mechanic.md`
