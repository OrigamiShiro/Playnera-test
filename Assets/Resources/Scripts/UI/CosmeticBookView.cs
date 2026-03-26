using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MakeupMechanic.Data;
using MakeupMechanic.Infrastructure;

namespace MakeupMechanic.UI
{
    [Serializable]
    public struct CosmeticPageConfig
    {
        public CosmeticType type;
        public Transform container;
        public GameObject itemPrefab;
    }

    public class CosmeticBookView : MonoBehaviour
    {
        [SerializeField] private Transform _rightPage;
        [SerializeField] private Toggle _eyeshadowTab;
        [SerializeField] private Toggle _blushTab;
        [SerializeField] private Toggle _lipstickTab;
        [SerializeField] private CosmeticPageConfig[] _pages;

        private CosmeticType _currentType;
        private bool _initialized;

        public event Action<ICosmetic> OnItemClicked;
        public event Action<CosmeticType> OnTypeChanged;
        public CosmeticType CurrentType => _currentType;
        public Transform RightPage => _rightPage;

        public void Init(CosmeticItemSO[] allItems)
        {
            // Group and spawn once per container
            var grouped = new Dictionary<CosmeticType, CosmeticItemSO[]>();
            foreach (CosmeticType type in Enum.GetValues(typeof(CosmeticType)))
            {
                var items = allItems.Where(i => i.type == type).ToArray();
                if (items.Length > 0)
                    grouped[type] = items;
            }

            foreach (var page in _pages)
            {
                if (!grouped.TryGetValue(page.type, out var items)) continue;

                foreach (var so in items)
                {
                    var go = ResourceManager.Instantiate(page.itemPrefab, page.container);
                    var cosmeticItem = go.GetComponent<CosmeticItem>();
                    cosmeticItem.Init(so);
                    cosmeticItem.OnClick += HandleItemClick;
                }
            }

            _eyeshadowTab.onValueChanged.AddListener(on => { if (on) ShowType(CosmeticType.Eyeshadow); });
            _blushTab.onValueChanged.AddListener(on => { if (on) ShowType(CosmeticType.Blush); });
            _lipstickTab.onValueChanged.AddListener(on => { if (on) ShowType(CosmeticType.Lipstick); });

            _initialized = true;
            _eyeshadowTab.isOn = true;
            ShowType(CosmeticType.Eyeshadow);
        }

        public void ShowType(CosmeticType type)
        {
            _currentType = type;

            foreach (var page in _pages)
                page.container.gameObject.SetActive(page.type == type);

            OnTypeChanged?.Invoke(type);
        }

        private void HandleItemClick(ICosmetic item)
        {
            OnItemClicked?.Invoke(item);
        }
    }
}
