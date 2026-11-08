using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColorRoomVR
{
    /// <summary>Plays the AudioManager's UI click and hover sounds for the Button on this object.</summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            AudioManager.Instance?.PlayUIClick();
        }
    }
}
