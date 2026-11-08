using UnityEngine;

namespace ColorRoomVR
{
    public class Fan : MonoBehaviour
    {
        // Private serialized fields
        [SerializeField] private PaintableObject baseTarget;
        [SerializeField] private PaintableObject decorationTarget;
        [SerializeField] private Transform pivot;
        [SerializeField] private Vector3 axis = Vector3.forward;
        [SerializeField] private float degreesPerSecond = 360f;

        private bool isBasePainted;
        private bool isDecorationPainted;

        private bool IsSpinning => isBasePainted && isDecorationPainted;

        private void OnEnable()
        {
            baseTarget.OnPainted.AddListener(OnBasePainted);
            decorationTarget.OnPainted.AddListener(OnDecorationPainted);
        }

        private void OnDisable()
        {
            baseTarget.OnPainted.RemoveListener(OnBasePainted);
            decorationTarget.OnPainted.RemoveListener(OnDecorationPainted);
        }

        private void OnBasePainted()
        {
            isBasePainted = true;
        }

        private void OnDecorationPainted()
        {
            isDecorationPainted = true;
        }

        private void Update()
        {
            if (!IsSpinning) return;
            pivot.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
