using UnityEngine;
using UnityEngine.UI;

namespace ColorRoomVR
{
    public class PaintProgressUI : MonoBehaviour
    {
        [SerializeField] private PaintProgressManager progress;
        [SerializeField] private Text counterText;
        [SerializeField] private string format = "{0} / {1} painted";

        private void OnEnable()
        {
            progress.OnProgressChanged.AddListener(Refresh);
            Refresh(progress.PaintedCount, progress.Total);
        }

        private void OnDisable()
        {
            progress.OnProgressChanged.RemoveListener(Refresh);
        }

        private void Refresh(int painted, int total)
        {
            if (counterText != null)
                counterText.text = string.Format(format, painted, total);
        }
    }
}
