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
