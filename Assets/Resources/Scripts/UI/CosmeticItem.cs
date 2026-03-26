using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MakeupMechanic.Data;

namespace MakeupMechanic.UI
{
    public class CosmeticItem : MonoBehaviour, ICosmetic, IPointerClickHandler
    {
        private CosmeticItemSO _data;
        private RectTransform _rectTransform;

        public CosmeticItemSO Data => _data;
        public RectTransform RectTransform => _rectTransform;
        public event Action<ICosmetic> OnClick;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Init(CosmeticItemSO data)
        {
            _data = data;
            GetComponentInChildren<Image>().sprite = data.itemSprite;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(this);
        }
    }
}
