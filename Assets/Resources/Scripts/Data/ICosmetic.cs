using UnityEngine;

namespace MakeupMechanic.Data
{
    public interface ICosmetic
    {
        CosmeticItemSO Data { get; }
        RectTransform RectTransform { get; }
    }
}
