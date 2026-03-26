using System.Collections;
using UnityEngine;
using DG.Tweening;
using MakeupMechanic.Data;
using MakeupMechanic.Infrastructure;

namespace MakeupMechanic.Systems
{
    public class BrushApplyStrategy : MonoBehaviour, IApplyStrategy
    {
        [SerializeField] private CosmeticType _type;
        [SerializeField] private RectTransform _brush;
        [SerializeField] private AnimatedTool _animatedTool;
        [SerializeField] private Transform _leftContainer;
        [SerializeField] private Transform _rightContainer;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private float _moveDuration = 0.3f;

        private bool _isAnimating;
        private Transform _returnParent;

        public CosmeticType Type => _type;
        public bool IsAnimating => _isAnimating;
        public RectTransform Brush => _brush;

        public void Pickup(ICosmetic item, DragSystem dragSystem)
        {
            StartCoroutine(BrushPickupSequence(item, dragSystem));
        }

        public void Apply(ICosmetic item, RectTransform tool, CharacterMakeupHandler character, Transform returnParent)
        {
            StartCoroutine(BrushApplySequence(item, tool, character, returnParent));
        }

        public void ReturnTool(RectTransform tool)
        {
            StartCoroutine(ReturnBrushSequence(tool));
        }

        private IEnumerator BrushPickupSequence(ICosmetic item, DragSystem dragSystem)
        {
            _isAnimating = true;

            _returnParent = _brush.parent;
            _brush.pivot = new Vector2(0.5f, 1f);
            _brush.SetParent(_canvas.transform, true);
            _brush.gameObject.SetActive(true);

            var itemWorldPos = item.RectTransform.position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                RectTransformUtility.WorldToScreenPoint(null, itemWorldPos),
                null,
                out var targetPos);

            yield return _brush.DOAnchorPos(targetPos, _moveDuration)
                .SetEase(Ease.InOutQuad)
                .WaitForCompletion();

            RectTransformUtils.SetPivot(_brush, new Vector2(0.5f, 0.5f));
            yield return _animatedTool.Play();
            RectTransformUtils.SetPivot(_brush, new Vector2(0.5f, 1f));

            _isAnimating = false;

            dragSystem.StartDrag(item, _brush, _brush.anchoredPosition);
        }

        private IEnumerator BrushApplySequence(ICosmetic item, RectTransform brush,
            CharacterMakeupHandler character, Transform returnParent)
        {
            _isAnimating = true;

            yield return RectTransformUtils.MoveToContainer(brush, _leftContainer, _moveDuration);
            RectTransformUtils.SetPivot(brush, new Vector2(0.5f, 0.5f));
            yield return _animatedTool.Play();
            RectTransformUtils.SetPivot(brush, new Vector2(0.5f, 1f));

            yield return RectTransformUtils.MoveToContainer(brush, _rightContainer, _moveDuration);
            RectTransformUtils.SetPivot(brush, new Vector2(0.5f, 0.5f));
            yield return _animatedTool.Play();

            character.ApplyCosmetic(item);

            RectTransformUtils.SetPivot(brush, new Vector2(0.5f, 0.5f));
            yield return RectTransformUtils.MoveToContainer(brush, returnParent, _moveDuration);

            _isAnimating = false;
        }

        private IEnumerator ReturnBrushSequence(RectTransform brush)
        {
            RectTransformUtils.SetPivot(brush, new Vector2(0.5f, 0.5f));
            yield return RectTransformUtils.MoveToContainer(brush, _returnParent, _moveDuration);
        }
    }
}
