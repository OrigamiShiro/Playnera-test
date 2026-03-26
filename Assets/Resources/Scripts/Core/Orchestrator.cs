using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
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
        [SerializeField] private Button _spongeButton;
        [SerializeField] private Button _creamButton;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _canvasRect;

        [Header("Tools")]
        [SerializeField] private RectTransform _eyeBrush;
        [SerializeField] private RectTransform _blushBrush;
        [SerializeField] private AnimatedTool _sponge;

        [Header("Animation Containers")]
        [SerializeField] private Transform _eyeLeftContainer;
        [SerializeField] private Transform _eyeRightContainer;
        [SerializeField] private Transform _blushLeftContainer;
        [SerializeField] private Transform _blushRightContainer;
        [SerializeField] private Transform _lipsContainer;

        [Header("Config")]
        [SerializeField] private string _levelConfigPath = "LevelConfig";
        [SerializeField] private float _moveDuration = 0.3f;

        private bool _isAnimating;

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
            _spongeButton.onClick.AddListener(HandleSpongeClick);
            _creamButton.onClick.AddListener(HandleCreamClick);
        }

        private void HandleTypeChanged(CosmeticType type)
        {
            var rightPage = _bookView.RightPage;

            _eyeBrush.gameObject.SetActive(false);
            _blushBrush.gameObject.SetActive(false);

            switch (type)
            {
                case CosmeticType.Eyeshadow:
                    _eyeBrush.SetParent(rightPage, false);
                    _eyeBrush.anchoredPosition = Vector2.zero;
                    _eyeBrush.gameObject.SetActive(true);
                    break;
                case CosmeticType.Blush:
                    _blushBrush.SetParent(rightPage, false);
                    _blushBrush.anchoredPosition = Vector2.zero;
                    _blushBrush.gameObject.SetActive(true);
                    break;
            }
        }

        private void HandleItemClick(ICosmetic item)
        {
            if (_isAnimating || _dragSystem.IsDragging) return;

            var type = _bookView.CurrentType;

            switch (type)
            {
                case CosmeticType.Eyeshadow:
                    StartCoroutine(BrushPickupSequence(item, _eyeBrush));
                    break;
                case CosmeticType.Blush:
                    StartCoroutine(BrushPickupSequence(item, _blushBrush));
                    break;
                case CosmeticType.Lipstick:
                    StartLipstickDrag(item);
                    break;
            }
        }

        private IEnumerator BrushPickupSequence(ICosmetic item, RectTransform brush)
        {
            _isAnimating = true;

            // pivot 0.5,1, move to canvas for free positioning
            brush.pivot = new Vector2(0.5f, 1f);
            brush.SetParent(_canvas.transform, true);
            brush.gameObject.SetActive(true);

            // compute target: world pos of tapped item → canvas local
            var itemWorldPos = ((MonoBehaviour)item).GetComponent<RectTransform>().position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                RectTransformUtility.WorldToScreenPoint(null, itemWorldPos),
                null,
                out var targetPos);

            // smoothly move to tapped item position
            yield return brush.DOAnchorPos(targetPos, _moveDuration)
                .SetEase(Ease.InOutQuad)
                .WaitForCompletion();

            // pivot → 0.5,0.5 (preserve pos) → play animator
            SetPivot(brush, new Vector2(0.5f, 0.5f));
            var tool = brush.GetComponent<AnimatedTool>();
            yield return tool.Play();
            // animation done → pivot back to 0.5,1
            SetPivot(brush, new Vector2(0.5f, 1f));

            _isAnimating = false;

            _dragSystem.StartDrag(item, brush, brush.anchoredPosition);
        }

        private void StartLipstickDrag(ICosmetic item)
        {
            var itemRect = ((MonoBehaviour)item).GetComponent<RectTransform>();
            var worldPos = itemRect.position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                RectTransformUtility.WorldToScreenPoint(null, worldPos),
                null,
                out var canvasLocalPos);

            var originalTransform = ((MonoBehaviour)item).transform;
            var gridLayout = originalTransform.parent.GetComponent<GridLayoutGroup>();
            var clone = Instantiate(((MonoBehaviour)item).gameObject, _canvas.transform);
            var cosmeticItem = clone.GetComponent<CosmeticItem>();
            if (cosmeticItem != null) cosmeticItem.enabled = false;
            foreach (var graphic in clone.GetComponentsInChildren<Graphic>())
                graphic.raycastTarget = false;
            var cloneRect = clone.GetComponent<RectTransform>();
            cloneRect.anchorMin = new Vector2(0.5f, 0.5f);
            cloneRect.anchorMax = new Vector2(0.5f, 0.5f);
            cloneRect.pivot = new Vector2(0.5f, 0.5f);
            if (gridLayout != null) cloneRect.sizeDelta = gridLayout.cellSize;
            _dragSystem.StartDrag(item, cloneRect, canvasLocalPos, isClone: true);
        }

        private void HandleApplied(ICosmetic item, RectTransform tool, bool isClone)
        {
            switch (item.Data.type)
            {
                case CosmeticType.Eyeshadow:
                    StartCoroutine(BrushApplySequence(item, tool,
                        _eyeLeftContainer, _eyeRightContainer));
                    break;
                case CosmeticType.Blush:
                    StartCoroutine(BrushApplySequence(item, tool,
                        _blushLeftContainer, _blushRightContainer));
                    break;
                case CosmeticType.Lipstick:
                    StartCoroutine(LipstickApplySequence(item, tool));
                    break;
            }
        }

        private IEnumerator BrushApplySequence(ICosmetic item, RectTransform brush,
            Transform leftContainer, Transform rightContainer)
        {
            _isAnimating = true;
            var tool = brush.GetComponent<AnimatedTool>();

            // move to Left (pivot 0.5,1 during move)
            yield return MoveToContainer(brush, leftContainer);
            // pivot → 0.5,0.5 → animate
            SetPivot(brush, new Vector2(0.5f, 0.5f));
            yield return tool.Play();
            // pivot back → 0.5,1
            SetPivot(brush, new Vector2(0.5f, 1f));

            // move to Right
            yield return MoveToContainer(brush, rightContainer);
            // pivot → 0.5,0.5 → animate
            SetPivot(brush, new Vector2(0.5f, 0.5f));
            yield return tool.Play();

            _character.ApplyCosmetic(item);

            // smoothly return to book
            SetPivot(brush, new Vector2(0.5f, 0.5f));
            yield return MoveToContainer(brush, _bookView.RightPage);

            _isAnimating = false;
        }

        private IEnumerator LipstickApplySequence(ICosmetic item, RectTransform clone)
        {
            _isAnimating = true;

            // move to Lips (pivot 0.5,1 during move)
            SetPivot(clone, new Vector2(0.5f, 1f));
            yield return MoveToContainer(clone, _lipsContainer);
            // pivot → 0.5,0.5 (preserve pos) → animate
            SetPivot(clone, new Vector2(0.5f, 0.5f));

            var tool = clone.GetComponent<AnimatedTool>();
            if (tool != null)
                yield return tool.Play();

            _character.ApplyCosmetic(item);
            Destroy(clone.gameObject);

            _isAnimating = false;
        }

        private IEnumerator MoveToContainer(RectTransform tool, Transform container)
        {
            tool.SetParent(container, true);
            yield return tool.DOAnchorPos(Vector2.zero, _moveDuration)
                .SetEase(Ease.InOutQuad)
                .WaitForCompletion();
        }

        private void HandleSpongeClick()
        {
            if (_isAnimating || _dragSystem.IsDragging) return;
            StartCoroutine(SpongeSequence());
        }

        private IEnumerator SpongeSequence()
        {
            _isAnimating = true;

            yield return _sponge.Play();

            _character.RemoveAllMakeup();
            _isAnimating = false;
        }

        private void HandleCreamClick()
        {
            if (_isAnimating) return;
            _character.RemoveAcne();
        }

        private static void SetPivot(RectTransform rt, Vector2 newPivot)
        {
            var delta = newPivot - rt.pivot;
            var size = rt.rect.size;
            rt.pivot = newPivot;
            rt.anchoredPosition += new Vector2(delta.x * size.x, delta.y * size.y);
        }

        private void OnDestroy()
        {
            _bookView.OnItemClicked -= HandleItemClick;
            _bookView.OnTypeChanged -= HandleTypeChanged;
            _dragSystem.OnApplied -= HandleApplied;
            _spongeButton.onClick.RemoveListener(HandleSpongeClick);
            _creamButton.onClick.RemoveListener(HandleCreamClick);
        }
    }
}
