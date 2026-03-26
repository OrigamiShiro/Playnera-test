using System;
using UnityEngine;
using UnityEngine.EventSystems;
using MakeupMechanic.Data;

namespace MakeupMechanic.Systems
{
    public class DragSystem : MonoBehaviour
    {
        [SerializeField] private RectTransform _dragPanel;
        [SerializeField] private RectTransform _faceZone;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private DragPanelHandler _dragPanelHandler;

        private ICosmetic _currentItem;
        private RectTransform _activeTool;
        private bool _isDragging;

        public event Action<ICosmetic, RectTransform> OnApplied;
        public event Action<ICosmetic, RectTransform> OnMissed;
        public bool IsDragging => _isDragging;

        private void Awake()
        {
            _dragPanelHandler.Init(this);
        }

        public void StartDrag(ICosmetic item, RectTransform tool, Vector2 anchoredPosition)
        {
            if (_isDragging) return;

            _isDragging = true;
            _currentItem = item;
            _activeTool = tool;
            _activeTool.anchoredPosition = anchoredPosition;
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
            {
                var item = _currentItem;
                var tool = _activeTool;
                ClearState();
                OnApplied?.Invoke(item, tool);
            }
            else
            {
                var item = _currentItem;
                var tool = _activeTool;
                ClearState();
                OnMissed?.Invoke(item, tool);
            }
        }

        private void ClearState()
        {
            _dragPanel.gameObject.SetActive(false);
            _currentItem = null;
            _activeTool = null;
            _isDragging = false;
        }

        private bool IsInFaceZone(Vector2 screenPos)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                _faceZone, screenPos, null);
        }
    }
}
