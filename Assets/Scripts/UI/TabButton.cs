// Assets/Scripts/UI/TabButton.cs
using UnityEngine;
using UnityEngine.UI;
using MakeupMechanic.Data;
using MakeupMechanic.Systems;

namespace MakeupMechanic.UI
{
    public class TabButton : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private CosmeticType _contentType;

        private CosmeticContainer _container;

        public CosmeticType ContentType => _contentType;

        public void Init(CosmeticContainer container)
        {
            _container = container;
            _toggle.onValueChanged.AddListener(OnToggleChanged);
            if (_toggle.isOn) _container.Show();
            else _container.Hide();
        }

        private void OnToggleChanged(bool isOn)
        {
            if (isOn) _container.Show();
            else _container.Hide();
        }

        private void OnDestroy()
        {
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }
    }
}
