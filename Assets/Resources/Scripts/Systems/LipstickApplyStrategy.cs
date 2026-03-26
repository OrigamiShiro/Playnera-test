using System.Collections;
using UnityEngine;
using DG.Tweening;
using MakeupMechanic.Data;
using MakeupMechanic.Infrastructure;

namespace MakeupMechanic.Systems
{
    public class LipstickApplyStrategy : MonoBehaviour, IApplyStrategy
    {
        [SerializeField] private Transform _lipsContainer;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private float _moveDuration = 0.3f;

        private bool _isAnimating;
        private RectState _lipstickSavedState;

        public CosmeticType Type => CosmeticType.Lipstick;
        public bool IsAnimating => _isAnimating;

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

        public void Pickup(ICosmetic item, DragSystem dragSystem)
        {
            StartCoroutine(LipstickPickupSequence(item, dragSystem));
        }

        public void Apply(ICosmetic item, RectTransform tool, CharacterMakeupHandler character, Transform returnParent)
        {
            StartCoroutine(LipstickApplySequence(item, tool, character, returnParent));
        }

        public void ReturnTool(RectTransform tool)
        {
            RestoreLipstick(tool);
        }

        private IEnumerator LipstickPickupSequence(ICosmetic item, DragSystem dragSystem)
        {
            _isAnimating = true;

            var rt = item.RectTransform;
            _lipstickSavedState = RectState.Save(rt);

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

            foreach (var graphic in rt.GetComponentsInChildren<UnityEngine.UI.Graphic>())
            {
                graphic.raycastTarget = false;
            }

            var targetPos = rt.anchoredPosition + new Vector2(0, 20f);
            yield return rt.DOAnchorPos(targetPos, _moveDuration)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();

            RectTransformUtils.SetPivot(rt, new Vector2(0.5f, 1f));

            _isAnimating = false;

            dragSystem.StartDrag(item, rt, rt.anchoredPosition);
        }

        private IEnumerator LipstickApplySequence(ICosmetic item, RectTransform tool,
            CharacterMakeupHandler character, Transform returnParent)
        {
            _isAnimating = true;

            RectTransformUtils.SetPivot(tool, new Vector2(0.5f, 1f));
            yield return RectTransformUtils.MoveToContainer(tool, _lipsContainer, _moveDuration);
            RectTransformUtils.SetPivot(tool, new Vector2(0.5f, 0.5f));

            var animTool = tool.GetComponent<AnimatedTool>();
            if (animTool != null)
            {
                yield return animTool.Play();
            }

            character.ApplyCosmetic(item);
            RestoreLipstick(tool);

            _isAnimating = false;
        }

        private void RestoreLipstick(RectTransform tool)
        {
            _lipstickSavedState.Restore(tool);

            foreach (var graphic in tool.GetComponentsInChildren<UnityEngine.UI.Graphic>())
            {
                graphic.raycastTarget = true;
            }
        }
    }
}
