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

            foreach (CosmeticType t in System.Enum.GetValues(typeof(CosmeticType)))
            {
                var brush = _applyHandler.GetBrushForType(t);
                if (brush != null)
                {
                    brush.gameObject.SetActive(false);
                }
            }

            var activeBrush = _applyHandler.GetBrushForType(type);
            if (activeBrush != null)
            {
                activeBrush.SetParent(rightPage, false);
                activeBrush.anchoredPosition = Vector2.zero;
                activeBrush.gameObject.SetActive(true);
            }
        }

        private void HandleItemClick(ICosmetic item)
        {
            if (_applyHandler.IsAnimating || _dragSystem.IsDragging) return;

            _applyHandler.StartPickup(item, _dragSystem);
        }

        private void HandleApplied(ICosmetic item, RectTransform tool)
        {
            _applyHandler.StartApply(item, tool, _character, _bookView.RightPage);
        }

        private void HandleMissed(ICosmetic item, RectTransform tool)
        {
            _applyHandler.ReturnTool(item, tool);
        }

        private void HandleSpongeClick()
        {
            if (_applyHandler.IsAnimating || _dragSystem.IsDragging) return;
            _applyHandler.StartSponge(_character);
        }

        private void HandleCreamClick()
        {
            if (_applyHandler.IsAnimating) return;
            _character.RemoveAcne();
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
