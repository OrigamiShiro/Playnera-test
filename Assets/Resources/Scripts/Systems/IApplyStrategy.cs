using UnityEngine;
using MakeupMechanic.Data;

namespace MakeupMechanic.Systems
{
    public interface IApplyStrategy
    {
        CosmeticType Type { get; }
        void Pickup(ICosmetic item, DragSystem dragSystem);
        void Apply(ICosmetic item, RectTransform tool, CharacterMakeupHandler character, Transform returnParent);
        void ReturnTool(RectTransform tool);
        bool IsAnimating { get; }
    }
}
