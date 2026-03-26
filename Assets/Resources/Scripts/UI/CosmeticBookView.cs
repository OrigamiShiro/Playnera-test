using System;
using System.Collections;
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

    [Serializable]
    public struct TabConfig
    {
        public CosmeticType type;
        public Toggle toggle;
    }

    public class CosmeticBookView : MonoBehaviour
    {
        [SerializeField] private Transform _rightPage;
        [SerializeField] private TabConfig[] _tabs;
        [SerializeField] private CosmeticPageConfig[] _pages;

        private CosmeticType _currentType;
        private readonly List<CosmeticItem> _spawnedItems = new List<CosmeticItem>();

        public event Action<ICosmetic> OnItemClicked;
        public event Action<CosmeticType> OnTypeChanged;
        public CosmeticType CurrentType => _currentType;
        public Transform RightPage => _rightPage;
        public Transform CurrentContainer { get; private set; }

        public void Init(CosmeticItemSO[] allItems)
        {
            var grouped = new Dictionary<CosmeticType, CosmeticItemSO[]>();
            foreach (CosmeticType type in Enum.GetValues(typeof(CosmeticType)))
            {
                var items = allItems.Where(i => i.type == type).ToArray();
                if (items.Length > 0)
                {
                    grouped[type] = items;
                }
            }

            foreach (var page in _pages)
            {
                if (!grouped.TryGetValue(page.type, out var items))
                {
                    continue;
                }

                foreach (var so in items)
                {
                    var go = ResourceManager.Instantiate(page.itemPrefab, page.container);
                    var cosmeticItem = go.GetComponent<CosmeticItem>();
                    cosmeticItem.Init(so);
                    cosmeticItem.OnClick += HandleItemClick;
                    _spawnedItems.Add(cosmeticItem);
                }
            }

            StartCoroutine(InitAfterLayout());
        }

        public void ShowType(CosmeticType type)
        {
            _currentType = type;

            foreach (var page in _pages)
            {
                bool active = page.type == type;
                page.container.gameObject.SetActive(active);
                if (active)
                {
                    CurrentContainer = page.container;
                }
            }

            OnTypeChanged?.Invoke(type);
        }

        private void HandleItemClick(ICosmetic item)
        {
            OnItemClicked?.Invoke(item);
        }

        private IEnumerator InitAfterLayout()
        {
            // all containers active → wait for layout rebuild
            yield return null;

            // disable grids — positions are now baked
            foreach (var page in _pages)
            {
                var grid = page.container.GetComponent<GridLayoutGroup>();
                if (grid != null)
                {
                    grid.enabled = false;
                }
            }

            // now setup tabs and show first type
            foreach (var tab in _tabs)
            {
                var type = tab.type;
                tab.toggle.onValueChanged.AddListener(on =>
                {
                    if (on)
                    {
                        ShowType(type);
                    }
                });
            }

            if (_tabs.Length > 0)
            {
                _tabs[0].toggle.isOn = true;
                ShowType(_tabs[0].type);
            }
        }

        private void OnDestroy()
        {
            foreach (var item in _spawnedItems)
            {
                if (item != null)
                {
                    item.OnClick -= HandleItemClick;
                }
            }
        }
    }
}
