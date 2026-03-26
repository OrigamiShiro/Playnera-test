// Assets/Scripts/Systems/DragPanelHandler.cs
using UnityEngine;
using UnityEngine.EventSystems;

namespace MakeupMechanic.Systems
{
    public class DragPanelHandler : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        private DragSystem _dragSystem;

        public void Init(DragSystem dragSystem)
        {
            _dragSystem = dragSystem;
        }

        public void OnPointerDown(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            _dragSystem.OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragSystem.OnPointerUp(eventData);
        }
    }
}
