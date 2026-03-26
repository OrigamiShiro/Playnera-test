using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MakeupMechanic.Data;

namespace MakeupMechanic.Systems
{
    [Serializable]
    public struct BrushConfig
    {
        public CosmeticType type;
        public RectTransform brush;
        public AnimatedTool animatedTool;
        public Transform leftContainer;
        public Transform rightContainer;
    }

    public class MakeupApplyHandler : MonoBehaviour
    {
        [SerializeField] private BrushConfig[] _brushConfigs;
        [SerializeField] private Transform _lipsContainer;
        [SerializeField] private float _moveDuration = 0.3f;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _canvasRect;

        private bool _isAnimating;

        public bool IsAnimating => _isAnimating;
        public event Action OnSequenceComplete;

        public bool TryGetBrushConfig(CosmeticType type, out BrushConfig config)
        {
            foreach (var bc in _brushConfigs)
            {
                if (bc.type == type)
                {
                    config = bc;
                    return true;
                }
            }
            config = default;
            return false;
        }

        public void StartBrushPickup(ICosmetic item, BrushConfig config, DragSystem dragSystem)
        {
            StartCoroutine(BrushPickupSequence(item, config, dragSystem));
        }

        public void StartBrushApply(ICosmetic item, RectTransform tool, BrushConfig config, CharacterMakeupHandler character, Transform returnParent)
        {
            StartCoroutine(BrushApplySequence(item, tool, config, character, returnParent));
        }

        public void StartLipstickPickup(ICosmetic item, DragSystem dragSystem)
        {
            StartCoroutine(LipstickPickupSequence(item, dragSystem));
        }

        public void ReturnBrush(RectTransform brush, Transform returnParent)
        {
            StartCoroutine(ReturnBrushSequence(brush, returnParent));
        }

        public void ReturnLipstick(RectTransform tool)
        {
            RestoreLipstick(tool);
        }

        private IEnumerator ReturnBrushSequence(RectTransform brush, Transform returnParent)
        {
            SetPivot(brush, new Vector2(0.5f, 0.5f));
            yield return MoveToContainer(brush, returnParent);
        }

        public void StartLipstickApply(ICosmetic item, RectTransform tool, CharacterMakeupHandler character, Transform returnParent)
        {
            StartCoroutine(LipstickApplySequence(item, tool, character, returnParent));
        }

        public void StartSponge(AnimatedTool sponge, CharacterMakeupHandler character)
        {
            StartCoroutine(SpongeSequence(sponge, character));
        }

        private IEnumerator BrushPickupSequence(ICosmetic item, BrushConfig config, DragSystem dragSystem)
        {
            _isAnimating = true;

            var brush = config.brush;
            brush.pivot = new Vector2(0.5f, 1f);
            brush.SetParent(_canvas.transform, true);
            brush.gameObject.SetActive(true);

            var itemWorldPos = item.RectTransform.position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                RectTransformUtility.WorldToScreenPoint(null, itemWorldPos),
                null,
                out var targetPos);

            yield return brush.DOAnchorPos(targetPos, _moveDuration)
                .SetEase(Ease.InOutQuad)
                .WaitForCompletion();

            SetPivot(brush, new Vector2(0.5f, 0.5f));
            yield return config.animatedTool.Play();
            SetPivot(brush, new Vector2(0.5f, 1f));

            _isAnimating = false;
            OnSequenceComplete?.Invoke();

            dragSystem.StartDrag(item, brush, brush.anchoredPosition);
        }

        private IEnumerator BrushApplySequence(ICosmetic item, RectTransform brush,
            BrushConfig config, CharacterMakeupHandler character, Transform returnParent)
        {
            _isAnimating = true;

            yield return MoveToContainer(brush, config.leftContainer);
            SetPivot(brush, new Vector2(0.5f, 0.5f));
            yield return config.animatedTool.Play();
            SetPivot(brush, new Vector2(0.5f, 1f));

            yield return MoveToContainer(brush, config.rightContainer);
            SetPivot(brush, new Vector2(0.5f, 0.5f));
            yield return config.animatedTool.Play();

            character.ApplyCosmetic(item);

            SetPivot(brush, new Vector2(0.5f, 0.5f));
            yield return MoveToContainer(brush, returnParent);

            _isAnimating = false;
            OnSequenceComplete?.Invoke();
        }

        private struct RectState
        {
            public Transform parent;
            public Vector2 pivot;
            public Vector2 anchoredPosition;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 sizeDelta;

            public static RectState Save(RectTransform rt)
            {
                return new RectState
                {
                    parent = rt.parent,
                    pivot = rt.pivot,
                    anchoredPosition = rt.anchoredPosition,
                    anchorMin = rt.anchorMin,
                    anchorMax = rt.anchorMax,
                    sizeDelta = rt.sizeDelta
                };
            }

            public void Restore(RectTransform rt)
            {
                rt.SetParent(parent, false);
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot = pivot;
                rt.sizeDelta = sizeDelta;
                rt.anchoredPosition = anchoredPosition;
            }
        }

        private RectState _lipstickSavedState;

        private IEnumerator LipstickPickupSequence(ICosmetic item, DragSystem dragSystem)
        {
            _isAnimating = true;

            var rt = item.RectTransform;
            _lipstickSavedState = RectState.Save(rt);

            // convert screen pos to canvas-space
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                RectTransformUtility.WorldToScreenPoint(null, rt.position),
                null,
                out var canvasPos);

            rt.SetParent(_canvas.transform, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = _lipstickSavedState.sizeDelta;
            rt.anchoredPosition = canvasPos;

            foreach (var graphic in rt.GetComponentsInChildren<Graphic>())
            {
                graphic.raycastTarget = false;
            }

            // slide up with center pivot
            var targetPos = rt.anchoredPosition + new Vector2(0, 20f);
            yield return rt.DOAnchorPos(targetPos, _moveDuration)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();

            // now set drag pivot
            SetPivot(rt, new Vector2(0.5f, 1f));

            _isAnimating = false;

            dragSystem.StartDrag(item, rt, rt.anchoredPosition);
        }

        private IEnumerator LipstickApplySequence(ICosmetic item, RectTransform tool,
            CharacterMakeupHandler character, Transform returnParent)
        {
            _isAnimating = true;

            SetPivot(tool, new Vector2(0.5f, 1f));
            yield return MoveToContainer(tool, _lipsContainer);
            SetPivot(tool, new Vector2(0.5f, 0.5f));

            var animTool = tool.GetComponent<AnimatedTool>();
            if (animTool != null)
            {
                yield return animTool.Play();
            }

            character.ApplyCosmetic(item);
            RestoreLipstick(tool);

            _isAnimating = false;
            OnSequenceComplete?.Invoke();
        }

        private void RestoreLipstick(RectTransform tool)
        {
            _lipstickSavedState.Restore(tool);

            foreach (var graphic in tool.GetComponentsInChildren<Graphic>())
            {
                graphic.raycastTarget = true;
            }
        }

        private IEnumerator SpongeSequence(AnimatedTool sponge, CharacterMakeupHandler character)
        {
            _isAnimating = true;

            yield return sponge.Play();

            character.RemoveAllMakeup();
            _isAnimating = false;
            OnSequenceComplete?.Invoke();
        }

        private IEnumerator MoveToContainer(RectTransform tool, Transform container)
        {
            tool.SetParent(container, true);
            yield return tool.DOAnchorPos(Vector2.zero, _moveDuration)
                .SetEase(Ease.InOutQuad)
                .WaitForCompletion();
        }

        public static void SetPivot(RectTransform rt, Vector2 newPivot)
        {
            var delta = newPivot - rt.pivot;
            var size = rt.rect.size;
            rt.pivot = newPivot;
            rt.anchoredPosition += new Vector2(delta.x * size.x, delta.y * size.y);
        }
    }
}
