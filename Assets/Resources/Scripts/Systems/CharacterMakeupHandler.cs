using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MakeupMechanic.Data;

namespace MakeupMechanic.Systems
{
    [Serializable]
    public struct MakeupLayer
    {
        public CosmeticType type;
        public Image layer;
    }

    public class CharacterMakeupHandler : MonoBehaviour
    {
        [SerializeField] private MakeupLayer[] _layers;
        [SerializeField] private GameObject _acne;

        private Dictionary<CosmeticType, Image> _layerMap;

        private void Awake()
        {
            _layerMap = new Dictionary<CosmeticType, Image>();
            foreach (var ml in _layers)
            {
                _layerMap[ml.type] = ml.layer;
            }
        }

        public void ApplyCosmetic(ICosmetic item)
        {
            var layer = GetLayer(item.Data.type);
            layer.sprite = item.Data.resultSprite;
            layer.gameObject.SetActive(true);
        }

        public void RemoveAllMakeup()
        {
            foreach (var ml in _layers)
            {
                ml.layer.gameObject.SetActive(false);
            }
        }

        public void RemoveAcne()
        {
            _acne.SetActive(false);
        }

        private Image GetLayer(CosmeticType type)
        {
            return _layerMap[type];
        }
    }
}
