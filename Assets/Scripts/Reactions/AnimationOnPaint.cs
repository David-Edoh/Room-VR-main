using UnityEngine;

namespace ColorRoomVR
{
    public class AnimationOnPaint : MonoBehaviour
    {
        // Private serialized fields
        [SerializeField] private PaintableObject target;

        // Private fields
        private Animator animator;

        private void OnEnable()
        {
            target.OnPainted.AddListener(OnPainted);
        }

        private void OnDisable()
        {
            target.OnPainted.RemoveListener(OnPainted);
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnPainted()
        {
            if (animator != null)
                animator.SetTrigger("Play");
        }
    }
}
