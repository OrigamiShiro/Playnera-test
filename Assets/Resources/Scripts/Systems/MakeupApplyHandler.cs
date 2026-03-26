using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MakeupMechanic.Data;

namespace MakeupMechanic.Systems
{
    public class MakeupApplyHandler : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] _strategies;
        [SerializeField] private AnimatedTool _sponge;

        private Dictionary<CosmeticType, IApplyStrategy> _strategyMap;
        private bool _isSpongeAnimating;

        public event Action OnSequenceComplete;

        private void Awake()
        {
            _strategyMap = new Dictionary<CosmeticType, IApplyStrategy>();
            foreach (var mb in _strategies)
            {
                if (mb is IApplyStrategy strategy)
                {
                    _strategyMap[strategy.Type] = strategy;
                }
            }
        }

        public bool IsAnimating
        {
            get
            {
                if (_isSpongeAnimating) return true;
                foreach (var strategy in _strategyMap.Values)
                {
                    if (strategy.IsAnimating) return true;
                }
                return false;
            }
        }

        public void StartPickup(ICosmetic item, DragSystem dragSystem)
        {
            var strategy = GetStrategy(item.Data.type);
            strategy.Pickup(item, dragSystem);
        }

        public void StartApply(ICosmetic item, RectTransform tool, CharacterMakeupHandler character, Transform returnParent)
        {
            var strategy = GetStrategy(item.Data.type);
            strategy.Apply(item, tool, character, returnParent);
        }

        public void ReturnTool(ICosmetic item, RectTransform tool)
        {
            var strategy = GetStrategy(item.Data.type);
            strategy.ReturnTool(tool);
        }

        public RectTransform GetBrushForType(CosmeticType type)
        {
            if (_strategyMap.TryGetValue(type, out var strategy) && strategy is BrushApplyStrategy brush)
            {
                return brush.Brush;
            }
            return null;
        }

        public void StartSponge(CharacterMakeupHandler character)
        {
            StartCoroutine(SpongeSequence(character));
        }

        private IEnumerator SpongeSequence(CharacterMakeupHandler character)
        {
            _isSpongeAnimating = true;

            yield return _sponge.Play();

            character.RemoveAllMakeup();
            _isSpongeAnimating = false;
            OnSequenceComplete?.Invoke();
        }

        private IApplyStrategy GetStrategy(CosmeticType type)
        {
            return _strategyMap[type];
        }
    }
}
