# Makeup Mechanic Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a makeup application mechanic where players drag cosmetic tools to a character's face to apply eyeshadow, blush, and lipstick, with cream/sponge support actions.

**Architecture:** Hybrid Mediator pattern — Orchestrator subscribes to delegates from CosmeticContainers and DragSystem, wires all systems together. Data layer uses ScriptableObjects loaded via ResourceManager. Drag mechanic uses a fullscreen invisible panel with IDragHandler forwarding to DragSystem.

**Tech Stack:** Unity 6, C#, Unity UI (Canvas Screen Space - Overlay), EventSystem, ScriptableObjects, Resources.Load

**Spec:** `docs/superpowers/specs/2026-03-25-makeup-mechanic-design.md`

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `Assets/Scripts/Data/CosmeticType.cs` | Create | Enum: Eyeshadow, Blush, Lipstick |
| `Assets/Scripts/Data/ICosmetic.cs` | Create | Interface exposing CosmeticItemSO Data |
| `Assets/Scripts/Data/CosmeticItemSO.cs` | Create | SO with type, itemSprite, resultSprite |
| `Assets/Scripts/Data/LevelConfigSO.cs` | Create | SO with availableItems array |
| `Assets/Scripts/Infrastructure/ResourceManager.cs` | Create | Static wrapper for Resources.Load and Instantiate |
| `Assets/Scripts/UI/CosmeticItem.cs` | Create | MonoBehaviour + ICosmetic + IPointerClickHandler on palette items |
| `Assets/Scripts/UI/TabButton.cs` | Create | Toggle → Show/Hide container |
| `Assets/Scripts/Systems/CosmeticContainer.cs` | Create | Spawns CosmeticItems, propagates clicks |
| `Assets/Scripts/Systems/DragPanelHandler.cs` | Create | IDragHandler/IPointerUpHandler forwarder on fullscreen panel |
| `Assets/Scripts/Systems/DragSystem.cs` | Create | Drag state machine, coordinate conversion, face zone check |
| `Assets/Scripts/Systems/CharacterMakeupHandler.cs` | Create | Applies/removes cosmetics on character layers |
| `Assets/Scripts/Core/Orchestrator.cs` | Create | Mediator: init, subscriptions, flow control |

---

### Task 1: Data Layer (Enum, Interface, ScriptableObjects)

**Files:**
- Create: `Assets/Scripts/Data/CosmeticType.cs`
- Create: `Assets/Scripts/Data/ICosmetic.cs`
- Create: `Assets/Scripts/Data/CosmeticItemSO.cs`
- Create: `Assets/Scripts/Data/LevelConfigSO.cs`

- [ ] **Step 1: Create folder structure**

```bash
mkdir -p Assets/Scripts/Data Assets/Scripts/Infrastructure Assets/Scripts/Systems Assets/Scripts/UI Assets/Scripts/Core Assets/Resources/Cosmetics Assets/Prefabs
```

- [ ] **Step 2: Create CosmeticType.cs**

```csharp
// Assets/Scripts/Data/CosmeticType.cs
namespace MakeupMechanic.Data
{
    public enum CosmeticType
    {
        Eyeshadow,
        Blush,
        Lipstick
    }
}
```

- [ ] **Step 3: Create ICosmetic.cs**

```csharp
// Assets/Scripts/Data/ICosmetic.cs
namespace MakeupMechanic.Data
{
    public interface ICosmetic
    {
        CosmeticItemSO Data { get; }
    }
}
```

- [ ] **Step 4: Create CosmeticItemSO.cs**

```csharp
// Assets/Scripts/Data/CosmeticItemSO.cs
using UnityEngine;

namespace MakeupMechanic.Data
{
    [CreateAssetMenu(menuName = "Cosmetic/Item")]
    public class CosmeticItemSO : ScriptableObject
    {
        public CosmeticType type;
        public Sprite itemSprite;
        public Sprite resultSprite;
    }
}
```

- [ ] **Step 5: Create LevelConfigSO.cs**

```csharp
// Assets/Scripts/Data/LevelConfigSO.cs
using UnityEngine;

namespace MakeupMechanic.Data
{
    [CreateAssetMenu(menuName = "Cosmetic/LevelConfig")]
    public class LevelConfigSO : ScriptableObject
    {
        public CosmeticItemSO[] availableItems;
    }
}
```

- [ ] **Step 6: Verify compilation in Unity**

Open Unity, wait for recompilation. Console should show no errors.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Data/
git commit -m "feat: add data layer — CosmeticType, ICosmetic, CosmeticItemSO, LevelConfigSO"
```

---

### Task 2: Infrastructure (ResourceManager)

**Files:**
- Create: `Assets/Scripts/Infrastructure/ResourceManager.cs`

- [ ] **Step 1: Create ResourceManager.cs**

```csharp
// Assets/Scripts/Infrastructure/ResourceManager.cs
using UnityEngine;

namespace MakeupMechanic.Infrastructure
{
    public static class ResourceManager
    {
        public static T Load<T>(string path) where T : Object
        {
            return Resources.Load<T>(path);
        }

        public static GameObject Instantiate(GameObject prefab, Transform parent)
        {
            return Object.Instantiate(prefab, parent);
        }
    }
}
```

- [ ] **Step 2: Verify compilation in Unity**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Infrastructure/
git commit -m "feat: add ResourceManager static wrapper"
```

---

### Task 3: CosmeticItem + CosmeticContainer

**Files:**
- Create: `Assets/Scripts/UI/CosmeticItem.cs`
- Create: `Assets/Scripts/Systems/CosmeticContainer.cs`

- [ ] **Step 1: Create CosmeticItem.cs**

```csharp
// Assets/Scripts/UI/CosmeticItem.cs
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MakeupMechanic.Data;

namespace MakeupMechanic.UI
{
    public class CosmeticItem : MonoBehaviour, ICosmetic, IPointerClickHandler
    {
        private CosmeticItemSO _data;

        public CosmeticItemSO Data => _data;
        public event Action<ICosmetic> OnClick;

        public void Init(CosmeticItemSO data)
        {
            _data = data;
            GetComponent<Image>().sprite = data.itemSprite;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(this);
        }
    }
}
```

- [ ] **Step 2: Create CosmeticContainer.cs**

```csharp
// Assets/Scripts/Systems/CosmeticContainer.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using MakeupMechanic.Data;
using MakeupMechanic.Infrastructure;
using MakeupMechanic.UI;

namespace MakeupMechanic.Systems
{
    public class CosmeticContainer : MonoBehaviour
    {
        [SerializeField] private CosmeticType _type;
        [SerializeField] private GameObject _itemPrefab;

        public event Action<ICosmetic> OnItemClicked;
        public CosmeticType Type => _type;

        private readonly List<CosmeticItem> _spawnedItems = new();

        public void Init(CosmeticItemSO[] items)
        {
            foreach (var so in items)
            {
                var go = ResourceManager.Instantiate(_itemPrefab, transform);
                var cosmeticItem = go.GetComponent<CosmeticItem>();
                cosmeticItem.Init(so);
                cosmeticItem.OnClick += HandleItemClick;
                _spawnedItems.Add(cosmeticItem);
            }
        }

        private void HandleItemClick(ICosmetic item)
        {
            OnItemClicked?.Invoke(item);
        }

        private void OnDestroy()
        {
            foreach (var item in _spawnedItems)
                item.OnClick -= HandleItemClick;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
```

- [ ] **Step 3: Verify compilation in Unity**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/CosmeticItem.cs Assets/Scripts/Systems/CosmeticContainer.cs
git commit -m "feat: add CosmeticItem and CosmeticContainer"
```

---

### Task 4: TabButton

**Files:**
- Create: `Assets/Scripts/UI/TabButton.cs`

- [ ] **Step 1: Create TabButton.cs**

```csharp
// Assets/Scripts/UI/TabButton.cs
using UnityEngine;
using UnityEngine.UI;
using MakeupMechanic.Data;
using MakeupMechanic.Systems;

namespace MakeupMechanic.UI
{
    public class TabButton : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private CosmeticType _contentType;

        private CosmeticContainer _container;

        public CosmeticType ContentType => _contentType;

        public void Init(CosmeticContainer container)
        {
            _container = container;
            _toggle.onValueChanged.AddListener(OnToggleChanged);
            if (_toggle.isOn) _container.Show();
            else _container.Hide();
        }

        private void OnToggleChanged(bool isOn)
        {
            if (isOn) _container.Show();
            else _container.Hide();
        }

        private void OnDestroy()
        {
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }
    }
}
```

- [ ] **Step 2: Verify compilation in Unity**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/UI/TabButton.cs
git commit -m "feat: add TabButton — toggle show/hide for containers"
```

---

### Task 5: CharacterMakeupHandler

**Files:**
- Create: `Assets/Scripts/Systems/CharacterMakeupHandler.cs`

- [ ] **Step 1: Create CharacterMakeupHandler.cs**

```csharp
// Assets/Scripts/Systems/CharacterMakeupHandler.cs
using System;
using UnityEngine;
using UnityEngine.UI;
using MakeupMechanic.Data;

namespace MakeupMechanic.Systems
{
    public class CharacterMakeupHandler : MonoBehaviour
    {
        [SerializeField] private Image _eyeshadowLayer;
        [SerializeField] private Image _blushLayer;
        [SerializeField] private Image _lipstickLayer;
        [SerializeField] private GameObject _acne;

        public void ApplyCosmetic(ICosmetic item)
        {
            var layer = GetLayer(item.Data.type);
            layer.sprite = item.Data.resultSprite;
            layer.gameObject.SetActive(true);
        }

        public void RemoveAllMakeup()
        {
            _eyeshadowLayer.gameObject.SetActive(false);
            _blushLayer.gameObject.SetActive(false);
            _lipstickLayer.gameObject.SetActive(false);
        }

        public void RemoveAcne()
        {
            _acne.SetActive(false);
        }

        private Image GetLayer(CosmeticType type) => type switch
        {
            CosmeticType.Eyeshadow => _eyeshadowLayer,
            CosmeticType.Blush => _blushLayer,
            CosmeticType.Lipstick => _lipstickLayer,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
```

- [ ] **Step 2: Verify compilation in Unity**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Systems/CharacterMakeupHandler.cs
git commit -m "feat: add CharacterMakeupHandler — apply/remove cosmetics on character"
```

---

### Task 6: DragSystem + DragPanelHandler

**Files:**
- Create: `Assets/Scripts/Systems/DragPanelHandler.cs`
- Create: `Assets/Scripts/Systems/DragSystem.cs`

- [ ] **Step 1: Create DragPanelHandler.cs**

```csharp
// Assets/Scripts/Systems/DragPanelHandler.cs
using UnityEngine;
using UnityEngine.EventSystems;

namespace MakeupMechanic.Systems
{
    public class DragPanelHandler : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        private DragSystem _dragSystem;

        public void Init(DragSystem dragSystem)
        {
            _dragSystem = dragSystem;
        }

        public void OnPointerDown(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            _dragSystem.OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragSystem.OnPointerUp(eventData);
        }
    }
}
```

- [ ] **Step 2: Create DragSystem.cs**

```csharp
// Assets/Scripts/Systems/DragSystem.cs
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using MakeupMechanic.Data;
using MakeupMechanic.UI;

namespace MakeupMechanic.Systems
{
    public class DragSystem : MonoBehaviour
    {
        [SerializeField] private RectTransform _dragPanel;
        [SerializeField] private RectTransform _eyeBrush;
        [SerializeField] private RectTransform _blushBrush;
        [SerializeField] private RectTransform _faceZone;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private DragPanelHandler _dragPanelHandler;

        private ICosmetic _currentItem;
        private RectTransform _activeTool;
        private Vector2 _startPosition;
        private bool _isDragging;
        private GameObject _lipstickClone;

        public event Action<ICosmetic> OnApplied;
        public bool IsDragging => _isDragging;

        private void Awake()
        {
            _dragPanelHandler.Init(this);
        }

        public void StartDrag(ICosmetic item, Vector2 anchoredPosition)
        {
            if (_isDragging) return;

            _isDragging = true;
            _currentItem = item;
            _activeTool = GetToolByType(item.Data.type);
            _activeTool.anchoredPosition = anchoredPosition;
            _startPosition = anchoredPosition;
            _activeTool.gameObject.SetActive(true);
            _dragPanel.gameObject.SetActive(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);

            _activeTool.anchoredPosition = localPoint;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragging) return;

            if (IsInFaceZone(eventData.position))
                OnApplied?.Invoke(_currentItem);

            Reset();
        }

        private void Reset()
        {
            _activeTool.anchoredPosition = _startPosition;
            _activeTool.gameObject.SetActive(false);
            _dragPanel.gameObject.SetActive(false);

            if (_lipstickClone != null)
            {
                Destroy(_lipstickClone);
                _lipstickClone = null;
            }

            _currentItem = null;
            _isDragging = false;
        }

        private RectTransform GetToolByType(CosmeticType type)
        {
            switch (type)
            {
                case CosmeticType.Eyeshadow: return _eyeBrush;
                case CosmeticType.Blush: return _blushBrush;
                case CosmeticType.Lipstick:
                    _lipstickClone = Instantiate(
                        ((MonoBehaviour)_currentItem).gameObject,
                        _canvas.transform);
                    var cosmeticItem = _lipstickClone.GetComponent<CosmeticItem>();
                    if (cosmeticItem != null) cosmeticItem.enabled = false;
                    return _lipstickClone.GetComponent<RectTransform>();
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private bool IsInFaceZone(Vector2 screenPos)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                _faceZone, screenPos, null);
        }
    }
}
```

- [ ] **Step 3: Verify compilation in Unity**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Systems/DragPanelHandler.cs Assets/Scripts/Systems/DragSystem.cs
git commit -m "feat: add DragSystem + DragPanelHandler — drag mechanic with face zone detection"
```

---

### Task 7: Orchestrator

**Files:**
- Create: `Assets/Scripts/Core/Orchestrator.cs`

- [ ] **Step 1: Create Orchestrator.cs**

```csharp
// Assets/Scripts/Core/Orchestrator.cs
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MakeupMechanic.Data;
using MakeupMechanic.Infrastructure;
using MakeupMechanic.Systems;
using MakeupMechanic.UI;

namespace MakeupMechanic.Core
{
    public class Orchestrator : MonoBehaviour
    {
        [SerializeField] private CharacterMakeupHandler _character;
        [SerializeField] private DragSystem _dragSystem;
        [SerializeField] private CosmeticContainer[] _containers;
        [SerializeField] private TabButton[] _tabs;
        [SerializeField] private Button _spongeButton;
        [SerializeField] private Button _creamButton;
        [SerializeField] private string _levelConfigPath = "LevelConfig";

        private void Start()
        {
            var config = ResourceManager.Load<LevelConfigSO>(_levelConfigPath);
            if (config == null)
            {
                Debug.LogError($"LevelConfig not found at path: {_levelConfigPath}");
                return;
            }

            foreach (var container in _containers)
            {
                var filtered = config.availableItems
                    .Where(i => i.type == container.Type)
                    .ToArray();

                container.Init(filtered);
                container.OnItemClicked += HandleItemClick;
            }

            foreach (var tab in _tabs)
            {
                var container = _containers.FirstOrDefault(c => c.Type == tab.ContentType);
                if (container != null)
                    tab.Init(container);
            }

            _dragSystem.OnApplied += HandleApplied;
            _spongeButton.onClick.AddListener(HandleSpongeClick);
            _creamButton.onClick.AddListener(HandleCreamClick);
        }

        private void HandleItemClick(ICosmetic item)
        {
            if (_dragSystem.IsDragging) return;

            var itemRect = ((MonoBehaviour)item).GetComponent<RectTransform>();
            var worldPos = itemRect.position;
            var canvasRect = _dragSystem.GetComponent<Transform>().parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                RectTransformUtility.WorldToScreenPoint(null, worldPos),
                null,
                out var canvasLocalPos);

            _dragSystem.StartDrag(item, canvasLocalPos);
        }

        private void HandleApplied(ICosmetic item)
        {
            _character.ApplyCosmetic(item);
        }

        private void HandleSpongeClick()
        {
            if (_dragSystem.IsDragging) return;
            _character.RemoveAllMakeup();
        }

        private void HandleCreamClick()
        {
            _character.RemoveAcne();
        }

        private void OnDestroy()
        {
            foreach (var container in _containers)
                container.OnItemClicked -= HandleItemClick;

            _dragSystem.OnApplied -= HandleApplied;
            _spongeButton.onClick.RemoveListener(HandleSpongeClick);
            _creamButton.onClick.RemoveListener(HandleCreamClick);
        }
    }
}
```

- [ ] **Step 2: Verify compilation in Unity**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/Orchestrator.cs
git commit -m "feat: add Orchestrator — mediator wiring all systems together"
```

---

### Task 8: Create ScriptableObject Assets & Prefabs

This task is done manually in Unity Editor.

- [ ] **Step 1: Create CosmeticItemSO assets**

In Unity: Right-click `Assets/Resources/Cosmetics/` → Create → Cosmetic → Item.

Create one SO per cosmetic color:
- `eyeshadow_01` through `eyeshadow_09` (type=Eyeshadow, assign itemSprite from `Цвета мейка/`, resultSprite from `Макияж/Тени/`)
- `blush_01` through `blush_09` (type=Blush, assign itemSprite from `Цвета румян/`, resultSprite from `Макияж/Румяна/`)
- `lipstick_01` through `lipstick_06` (type=Lipstick, assign itemSprite from `Помады/`, resultSprite from `Макияж/Помады/`)

- [ ] **Step 2: Create LevelConfigSO asset**

Right-click `Assets/Resources/` → Create → Cosmetic → LevelConfig.
Name: `LevelConfig`.
Drag all created CosmeticItemSO assets into `availableItems` array.

- [ ] **Step 3: Create CosmeticCircle prefab**

Create a UI Image GO with:
- `CosmeticItem` component
- `Image` component (sprite assigned at runtime via Init)
- Raycast Target = true
- Size matching the color circles in the reference

Save to `Assets/Prefabs/CosmeticCircle.prefab`.

- [ ] **Step 4: Create LipstickItem prefab**

Same as CosmeticCircle but sized for lipstick sprites.
Save to `Assets/Prefabs/LipstickItem.prefab`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Resources/ Assets/Prefabs/
git commit -m "feat: add SO assets, LevelConfig, and item prefabs"
```

---

### Task 9: Scene Setup & Wiring

This task is done in Unity Editor — wire all components on the scene.

- [ ] **Step 1: Add FaceZone**

Add empty GO as child of Character. Add RectTransform sized to cover face area. Name: `FaceZone`.

- [ ] **Step 2: Add DragPanel**

Add Image to Canvas (last child for highest sort). Set alpha=0, Raycast Target=true. Add `DragPanelHandler` component. Set `SetActive(false)`.

- [ ] **Step 3: Wire CosmeticContainers**

On each container GO (EyeshadowContainer, BlushContainer, LipstickContainer):
- Add `CosmeticContainer` component
- Set `_type` to matching CosmeticType
- Set `_itemPrefab` to CosmeticCircle (or LipstickItem for lipstick)

- [ ] **Step 4: Wire TabButtons**

On each tab GO:
- Add `TabButton` component
- Set `_toggle` reference
- Set `_contentType` to matching type
- Ensure ToggleGroup is assigned on parent Tabs GO
- Set first tab (Eyeshadow) toggle `isOn = true`

- [ ] **Step 5: Wire DragSystem**

Add `DragSystem` component to a GO on scene. Set references:
- `_dragPanel` → DragPanel RectTransform
- `_eyeBrush` → EyeBrush RectTransform
- `_blushBrush` → BlushBrush RectTransform
- `_faceZone` → FaceZone RectTransform
- `_canvas` → Canvas
- `_dragPanelHandler` → DragPanelHandler on DragPanel

- [ ] **Step 6: Wire CharacterMakeupHandler**

Add `CharacterMakeupHandler` component on Character GO. Set references:
- `_eyeshadowLayer` → EyeshadowLayer Image
- `_blushLayer` → BlushLayer Image
- `_lipstickLayer` → LipstickLayer Image
- `_acne` → Acne GO

- [ ] **Step 7: Wire Orchestrator**

Add `Orchestrator` component on Orchestrator GO. Set references:
- `_character` → CharacterMakeupHandler
- `_dragSystem` → DragSystem
- `_containers` → array of 3 CosmeticContainers
- `_tabs` → array of 3 TabButtons
- `_spongeButton` → Sponge Button
- `_creamButton` → Cream Button
- `_levelConfigPath` → "LevelConfig"

- [ ] **Step 8: Save scene and verify**

Enter Play mode. Check:
- Tabs switch content visibility
- Console has no errors

- [ ] **Step 9: Commit**

```bash
git add Assets/Scenes/
git commit -m "feat: wire all components on scene"
```

---

### Task 10: Integration Testing & Polish

- [ ] **Step 1: Test eyeshadow flow**

Play mode → click eyeshadow color circle → brush appears at circle position → drag to face → release → eyeshadow layer appears on character with correct sprite.

- [ ] **Step 2: Test blush flow**

Same flow with blush color → blush brush → face zone.

- [ ] **Step 3: Test lipstick flow**

Click lipstick → clone follows finger → drag to face → lips change color. Original lipstick stays in palette.

- [ ] **Step 4: Test sponge**

Apply any cosmetic → click sponge → all makeup layers hidden. Verify sponge is blocked during active drag.

- [ ] **Step 5: Test cream**

Click cream → acne GO disappears.

- [ ] **Step 6: Test edge cases**

- Double-tap during drag → no second drag starts
- Release outside face zone → tool resets, no cosmetic applied
- Switch tabs during drag → verify no crash
- Apply new color over existing → sprite overwrites

- [ ] **Step 7: Fix any issues found**

- [ ] **Step 8: Final commit**

```bash
git add -A
git commit -m "fix: integration testing fixes"
```
