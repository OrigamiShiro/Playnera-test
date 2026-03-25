using UnityEngine;

namespace MakeupMechanic.Data
{
    [CreateAssetMenu(menuName = "Cosmetic/LevelConfig")]
    public class LevelConfigSO : ScriptableObject
    {
        public CosmeticItemSO[] availableItems;
    }
}
