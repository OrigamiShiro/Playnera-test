using System.Collections;
using UnityEngine;

namespace MakeupMechanic.Systems
{
    public class AnimatedTool : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        public Coroutine Play()
        {
            return StartCoroutine(PlayAnimation());
        }

        private IEnumerator PlayAnimation()
        {
            _animator.enabled = true;
            _animator.Play(0, 0, 0f);
            yield return null;

            var state = _animator.GetCurrentAnimatorStateInfo(0);
            while (state.normalizedTime < 1f)
            {
                yield return null;
                state = _animator.GetCurrentAnimatorStateInfo(0);
            }

            _animator.enabled = false;
        }
    }
}
