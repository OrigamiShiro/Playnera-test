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
        [Header("Systems")]
        [SerializeField] private CharacterMakeupHandler _character;
        [SerializeField] private DragSystem _dragSystem;
        [SerializeField] private CosmeticBookView _bookView;
        [SerializeField] private MakeupApplyHandler _applyHandler;
        [SerializeField] private Button _spongeButton;
        [SerializeField] private Button _creamButton;

        [Header("Tools")]
        [SerializeField] private AnimatedTool _sponge;

        [Header("Config")]
        [SerializeField] private string _levelConfigPath = "LevelConfig";

        private void Start()
        {
            var config = ResourceManager.Load<LevelConfigSO>(_levelConfigPath);
            if (config == null)
            {
                Debug.LogError($"LevelConfig not found at path: {_levelConfigPath}");
                return;
            }

            _bookView.OnItemClicked += HandleItemClick;
            _bookView.OnTypeChanged += HandleTypeChanged;
            _bookView.Init(config.availableItems);

            _dragSystem.OnApplied += HandleApplied;
            _dragSystem.OnMissed += HandleMissed;
            _spongeButton.onClick.AddListener(HandleSpongeClick);
            _creamButton.onClick.AddListener(HandleCreamClick);
        }

        private void HandleTypeChanged(CosmeticType type)
        {
            var rightPage = _bookView.RightPage;

            foreach (var bc in GetBrushConfigs())
            {
                bc.brush.gameObject.SetActive(false);
            }

            if (_applyHandler.TryGetBrushConfig(type, out var config))
            {
                config.brush.SetParent(rightPage, false);
                config.brush.anchoredPosition = Vector2.zero;
                config.brush.gameObject.SetActive(true);
            }
        }

        private void HandleItemClick(ICosmetic item)
        {
            if (_applyHandler.IsAnimating || _dragSystem.IsDragging) return;

            var type = _bookView.CurrentType;

            if (_applyHandler.TryGetBrushConfig(type, out var config))
            {
                _applyHandler.StartBrushPickup(item, config, _dragSystem);
            }
            else if (type == CosmeticType.Lipstick)
            {
                _applyHandler.StartLipstickPickup(item, _dragSystem);
            }
        }

        private void HandleApplied(ICosmetic item, RectTransform tool)
        {
            if (_applyHandler.TryGetBrushConfig(item.Data.type, out var config))
            {
                _applyHandler.StartBrushApply(item, tool, config, _character, _bookView.RightPage);
            }
            else if (item.Data.type == CosmeticType.Lipstick)
            {
                _applyHandler.StartLipstickApply(item, tool, _character, _bookView.CurrentContainer);
            }
        }

        private void HandleMissed(ICosmetic item, RectTransform tool)
        {
            if (_applyHandler.TryGetBrushConfig(item.Data.type, out var config))
            {
                _applyHandler.ReturnBrush(tool, _bookView.RightPage);
            }
            else if (item.Data.type == CosmeticType.Lipstick)
            {
                _applyHandler.ReturnLipstick(tool);
            }
        }

        private void HandleSpongeClick()
        {
            if (_applyHandler.IsAnimating || _dragSystem.IsDragging) return;
            _applyHandler.StartSponge(_sponge, _character);
        }

        private void HandleCreamClick()
        {
            if (_applyHandler.IsAnimating) return;
            _character.RemoveAcne();
        }

        private BrushConfig[] GetBrushConfigs()
        {
            var configs = new System.Collections.Generic.List<BrushConfig>();
            foreach (CosmeticType type in System.Enum.GetValues(typeof(CosmeticType)))
            {
                if (_applyHandler.TryGetBrushConfig(type, out var config))
                {
                    configs.Add(config);
                }
            }
            return configs.ToArray();
        }

        private void OnDestroy()
        {
            _bookView.OnItemClicked -= HandleItemClick;
            _bookView.OnTypeChanged -= HandleTypeChanged;
            _dragSystem.OnApplied -= HandleApplied;
            _dragSystem.OnMissed -= HandleMissed;
            _spongeButton.onClick.RemoveListener(HandleSpongeClick);
            _creamButton.onClick.RemoveListener(HandleCreamClick);
        }
    }
}
