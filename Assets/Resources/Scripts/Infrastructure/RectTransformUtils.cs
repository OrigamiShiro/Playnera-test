using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace MakeupMechanic.Infrastructure
{
    public static class RectTransformUtils
    {
        public static void SetPivot(RectTransform rt, Vector2 newPivot)
        {
            var delta = newPivot - rt.pivot;
            var size = rt.rect.size;
            rt.pivot = newPivot;
            rt.anchoredPosition += new Vector2(delta.x * size.x, delta.y * size.y);
        }

        public static IEnumerator MoveToContainer(RectTransform tool, Transform container, float duration)
        {
            tool.SetParent(container, true);
            yield return tool.DOAnchorPos(Vector2.zero, duration)
                .SetEase(Ease.InOutQuad)
                .WaitForCompletion();
        }
    }
}
