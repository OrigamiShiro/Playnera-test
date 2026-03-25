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
