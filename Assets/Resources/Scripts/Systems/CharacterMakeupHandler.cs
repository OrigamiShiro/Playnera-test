using System;
using UnityEngine;
using UnityEngine.UI;
using MakeupMechanic.Data;

namespace MakeupMechanic.Systems
{
    public class CharacterMakeupHandler : MonoBehaviour
    {
        [SerializeField] private Image _eyeshadowLayer;
        [SerializeField] private Image _blushLayer;
        [SerializeField] private Image _lipstickLayer;
        [SerializeField] private GameObject _acne;

        public void ApplyCosmetic(ICosmetic item)
        {
            var layer = GetLayer(item.Data.type);
            layer.sprite = item.Data.resultSprite;
            layer.gameObject.SetActive(true);
        }

        public void RemoveAllMakeup()
        {
            _eyeshadowLayer.gameObject.SetActive(false);
            _blushLayer.gameObject.SetActive(false);
            _lipstickLayer.gameObject.SetActive(false);
        }

        public void RemoveAcne()
        {
            _acne.SetActive(false);
        }

        private Image GetLayer(CosmeticType type) => type switch
        {
            CosmeticType.Eyeshadow => _eyeshadowLayer,
            CosmeticType.Blush => _blushLayer,
            CosmeticType.Lipstick => _lipstickLayer,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
