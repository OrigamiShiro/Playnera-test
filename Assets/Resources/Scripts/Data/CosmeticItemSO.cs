using UnityEngine;

namespace MakeupMechanic.Data
{
    [CreateAssetMenu(menuName = "Cosmetic/Item")]
    public class CosmeticItemSO : ScriptableObject
    {
        public CosmeticType type;
        public Sprite itemSprite;
        public Sprite resultSprite;
    }
}
