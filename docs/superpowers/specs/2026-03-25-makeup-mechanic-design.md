# Makeup Mechanic — Design Spec

## Overview

Механика нанесения макияжа для мобильной игры. Игрок выбирает косметику (тени, румяна, помада), drag'ом подносит инструмент к лицу персонажа, макияж применяется. Дополнительно: крем убирает акне, спонжик сбрасывает весь макияж.

Платформы: Android + PC (кроссплатформенный input через Unity EventSystem).

## Architecture Overview

Гибридный подход: Mediator (Orchestrator) управляет системами, подписывается на делегаты компонентов. Системы не знают друг о друге, вся логика связей — в оркестраторе.

```
Orchestrator (Mediator)
├── подписывается на делегаты
├── управляет инициализацией
│
├── CosmeticContainer.OnItemClicked → HandleItemClick → DragSystem.StartDrag
├── DragSystem.OnApplied → HandleApplied → CharacterMakeupHandler.ApplyCosmetic
├── SpongeButton.onClick → HandleSpongeClick → CharacterMakeupHandler.RemoveAllMakeup
├── CreamButton.onClick → HandleCreamClick → CharacterMakeupHandler.RemoveAcne
```

## Data Layer

### Enum

```csharp
public enum CosmeticType
{
    Eyeshadow,
    Blush,
    Lipstick
}
```

### CosmeticItemSO

ScriptableObject — чистые данные.

```csharp
[CreateAssetMenu(menuName = "Cosmetic/Item")]
public class CosmeticItemSO : ScriptableObject
{
    public CosmeticType type;
    public Sprite itemSprite;      // спрайт в палетке (кружок или помада)
    public Sprite resultSprite;    // спрайт результата на персонаже
}
```

Хранятся в `Resources/Cosmetics/`.

### LevelConfigSO

```csharp
[CreateAssetMenu(menuName = "Cosmetic/LevelConfig")]
public class LevelConfigSO : ScriptableObject
{
    public CosmeticItemSO[] availableItems;
}
```

Определяет набор доступной косметики на уровне. Загружается через `ResourceManager` по пути, переданному в оркестратор (параметризировано, не захардкожено).

### ICosmetic

```csharp
public interface ICosmetic
{
    CosmeticItemSO Data { get; }
}
```

Единственное свойство — доступ к SO. Потребители достают `Data.type`, `Data.resultSprite` самостоятельно.

### CosmeticItem

MonoBehaviour на инстансе в контейнере. Runtime-обёртка над SO.

```csharp
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
```

## Infrastructure

### ResourceManager

Статический класс. Делегирует ответственность за загрузку и инстансинг.

```csharp
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
```

## Systems

### CosmeticContainer

На сцене 3 контейнера. Каждый знает свой `CosmeticType`, имеет ссылку на префаб айтема. Получает отфильтрованные SO от оркестратора.

```csharp
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
```

### DragSystem

Управляет drag-механикой через fullscreen панель (`Image`, alpha=0).

На DragPanel висит отдельный компонент `DragPanelHandler` (реализует `IDragHandler`, `IPointerUpHandler`), который форвардит события в `DragSystem`.

Все элементы живут в Canvas (Screen Space - Overlay), поэтому позиционирование через `RectTransformUtility`, не `Camera.ScreenToWorldPoint`.

Две кисти на сцене (тени, румяна) — выключены по умолчанию. Помада — создаётся клон инстанса, оригинал остаётся в палетке.

Кисти и клон помады — прямые children `_canvas.transform`, чтобы `anchoredPosition` был в едином координатном пространстве. Позиция из `HandleItemClick` конвертируется в canvas-space перед передачей.

```csharp
// Компонент на DragPanel — форвардит события в DragSystem
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
```

```csharp
public class DragSystem : MonoBehaviour
{
    [SerializeField] private RectTransform _dragPanel;
    [SerializeField] private RectTransform _eyeBrush;
    [SerializeField] private RectTransform _blushBrush;
    [SerializeField] private RectTransform _faceZone;
    [SerializeField] private Canvas _canvas;

    private ICosmetic _currentItem;
    private RectTransform _activeTool;
    private Vector2 _startPosition;
    private bool _isDragging;
    private GameObject _lipstickClone;

    public event Action<ICosmetic> OnApplied;

    public bool IsDragging => _isDragging;

    public void StartDrag(ICosmetic item, Vector2 anchoredPosition)
    {
        if (_isDragging) return;  // защита от двойного тапа

        _isDragging = true;
        _currentItem = item;
        _activeTool = GetToolByType(item.Data.type);
        _activeTool.anchoredPosition = anchoredPosition;
        _startPosition = anchoredPosition;
        _activeTool.gameObject.SetActive(true);
        _dragPanel.gameObject.SetActive(true);
    }

    // IDragHandler на панели
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

    // IPointerUpHandler на панели
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
            Object.Destroy(_lipstickClone);
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
                _lipstickClone = Object.Instantiate(
                    ((MonoBehaviour)_currentItem).gameObject,
                    _canvas.transform);
                // Отключаем CosmeticItem на клоне, чтобы не стрелял OnClick
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
```

**State machine:**

```
IDLE → StartDrag() → DRAGGING (OnDrag) → OnPointerUp → CHECK_ZONE → IDLE
```

### CharacterMakeupHandler

На персонаже. Принимает `ICosmetic`, применяет по типу.

```csharp
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
```

### TabButton

Toggle + контейнер. Связь устанавливается оркестратором через `Init()`.

```csharp
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
```

## Orchestrator

Центральный медиатор. Инициализирует системы, подписывается на делегаты, управляет флоу.

```csharp
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

        // Связываем табы с контейнерами
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

        // Конвертируем позицию из пространства контейнера в пространство canvas
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
        if (_dragSystem.IsDragging) return;  // блокируем во время drag'а
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
```

## Scene Hierarchy

```
Scene
├── Main Camera
├── EventSystem
├── Canvas
│   ├── Background
│   ├── Character
│   │   ├── EyeshadowLayer (Image, выключен)
│   │   ├── BlushLayer (Image, выключен)
│   │   ├── LipstickLayer (Image, выключен)
│   │   ├── Acne (Image)
│   │   └── FaceZone (empty GO + RectTransform, sized to face area)
│   ├── ToolMenu
│   │   ├── Cream (Button)
│   │   └── Sponge (Button)
│   ├── Tabs (ToggleGroup)
│   │   ├── TabEyeshadow (Toggle + TabButton)
│   │   ├── TabBlush (Toggle + TabButton)
│   │   └── TabLipstick (Toggle + TabButton)
│   ├── Containers
│   │   ├── EyeshadowContainer (CosmeticContainer)
│   │   ├── BlushContainer (CosmeticContainer)
│   │   └── LipstickContainer (CosmeticContainer)
│   ├── EyeBrush (выключен, Animator)
│   ├── BlushBrush (выключен, Animator)
│   ├── DragPanel (Image alpha=0, IDragHandler, выключен)
│   └── Orchestrator
```

## Data Flow

```
1. Start:
   Orchestrator → ResourceManager.Load(LevelConfig) → фильтрует → Container.Init()

2. Tap on cosmetic item:
   CosmeticItem.OnClick → Container.OnItemClicked → Orchestrator.HandleItemClick
   → DragSystem.StartDrag(item, position)

3. Drag:
   DragPanel.OnDrag → DragSystem перемещает activeTool

4. Release in face zone:
   DragPanel.OnPointerUp → DragSystem.OnApplied → Orchestrator.HandleApplied
   → CharacterMakeupHandler.ApplyCosmetic(item)

5. Sponge:
   Button.onClick → Orchestrator.HandleSpongeClick → CharacterMakeupHandler.RemoveAllMakeup

6. Cream:
   Button.onClick → Orchestrator.HandleCreamClick → CharacterMakeupHandler.RemoveAcne
```

## File Structure

```
Assets/
├── Scripts/
│   ├── Data/
│   │   ├── CosmeticType.cs
│   │   ├── CosmeticItemSO.cs
│   │   ├── LevelConfigSO.cs
│   │   └── ICosmetic.cs
│   ├── Infrastructure/
│   │   └── ResourceManager.cs
│   ├── Systems/
│   │   ├── DragSystem.cs
│   │   ├── DragPanelHandler.cs
│   │   ├── CharacterMakeupHandler.cs
│   │   └── CosmeticContainer.cs
│   ├── UI/
│   │   ├── TabButton.cs
│   │   └── CosmeticItem.cs
│   └── Core/
│       └── Orchestrator.cs
├── Resources/
│   ├── Cosmetics/          # CosmeticItemSO assets
│   └── LevelConfig.asset   # LevelConfigSO
├── Prefabs/
│   ├── CosmeticCircle.prefab   # кружок для теней/румян
│   └── LipstickItem.prefab     # помада
```
