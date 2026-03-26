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
        private bool _isClone;
        private Transform _originalParent;

        public event Action<ICosmetic, RectTransform, bool> OnApplied;
        public event Action OnMissed;
        public bool IsDragging => _isDragging;

        private void Awake()
        {
            _dragPanelHandler.Init(this);
        }

        public void StartDrag(ICosmetic item, RectTransform tool, Vector2 anchoredPosition, bool isClone = false)
        {
            if (_isDragging) return;

            _isDragging = true;
            _currentItem = item;
            _activeTool = tool;
            _isClone = isClone;

            if (!isClone)
            {
                _originalParent = _activeTool.parent;
                _activeTool.pivot = new Vector2(0.5f, 1f);
                _activeTool.SetParent(_canvas.transform, true);
            }

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
                var isClone = _isClone;
                ClearState();
                OnApplied?.Invoke(item, tool, isClone);
            }
            else
            {
                ResetTool();
                ClearState();
                OnMissed?.Invoke();
            }
        }

        private void ResetTool()
        {
            if (_isClone)
            {
                Destroy(_activeTool.gameObject);
            }
            else
            {
                _activeTool.pivot = new Vector2(0.5f, 0.5f);
                _activeTool.SetParent(_originalParent, false);
                _activeTool.anchoredPosition = Vector2.zero;
            }
        }

        private void ClearState()
        {
            _dragPanel.gameObject.SetActive(false);
            _currentItem = null;
            _activeTool = null;
            _originalParent = null;
            _isDragging = false;
            _isClone = false;
        }

        private bool IsInFaceZone(Vector2 screenPos)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                _faceZone, screenPos, null);
        }
    }
}
