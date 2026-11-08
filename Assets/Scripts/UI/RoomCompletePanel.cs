using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ColorRoomVR
{
    public class RoomCompletePanel : MonoBehaviour
    {
        [SerializeField] private PaintProgressManager progress;
        [SerializeField] private GameObject panel;
        [SerializeField] private WorldPanelPlacer placer;
        [SerializeField] private ParticleSystem celebration;
        [SerializeField] private Text summaryText;
        [SerializeField] private string summaryFormat = "{0} objects painted";
        [SerializeField] private Button restartButton;
        [SerializeField] private Button closeButton;

        [Header("Events")]
        [Tooltip("Fired when the panel is shown (hook for the final sound).")]
        public UnityEvent OnShown;

        private void Awake()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        private void OnEnable()
        {
            progress.OnRoomCompleted.AddListener(Show);
            if (restartButton != null) restartButton.onClick.AddListener(progress.ResetRoom);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        private void OnDisable()
        {
            progress.OnRoomCompleted.RemoveListener(Show);
            if (restartButton != null) restartButton.onClick.RemoveListener(progress.ResetRoom);
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
        }

        public void Show()
        {
            if (summaryText != null)
                summaryText.text = string.Format(summaryFormat, progress.Total);

            if (placer != null)
                placer.PlaceInFront();
            if (panel != null)
                panel.SetActive(true);
            if (celebration != null)
            {
                // The particle system lives outside the (scaled) canvas; rain confetti from above the panel.
                celebration.transform.position = panel.transform.position + Vector3.up * 1f;
                celebration.transform.rotation = panel.transform.rotation;

                celebration.Play();
            }

            OnShown?.Invoke();
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }
}
