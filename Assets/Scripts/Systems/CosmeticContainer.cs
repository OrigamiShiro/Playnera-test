using System;
using System.Collections.Generic;
using UnityEngine;
using MakeupMechanic.Data;
using MakeupMechanic.Infrastructure;
using MakeupMechanic.UI;

namespace MakeupMechanic.Systems
{
    public class CosmeticContainer : MonoBehaviour
    {
        [SerializeField] private CosmeticType _type;
        [SerializeField] private GameObject _itemPrefab;

        public event Action<ICosmetic> OnItemClicked;
        public CosmeticType Type => _type;

        private readonly List<CosmeticItem> _spawnedItems = new();

        public void Init(CosmeticItemSO[] items)
        {
            foreach (var so in items)
            {
                var go = ResourceManager.Instantiate(_itemPrefab, transform);
                var cosmeticItem = go.GetComponent<CosmeticItem>();
                cosmeticItem.Init(so);
                cosmeticItem.OnClick += HandleItemClick;
                _spawnedItems.Add(cosmeticItem);
            }
        }

        private void HandleItemClick(ICosmetic item)
        {
            OnItemClicked?.Invoke(item);
        }

        private void OnDestroy()
        {
            foreach (var item in _spawnedItems)
                item.OnClick -= HandleItemClick;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
