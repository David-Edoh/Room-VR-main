using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColorRoomVR
{
    /// <summary>
    /// Plays the AudioManager's UI click and hover sounds for the Toggle on this object.
    /// Click is detected from the pointer (not onValueChanged), so programmatic value changes and
    /// ToggleGroup deselections stay silent.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class UIToggleSound : MonoBehaviour, IPointerClickHandler
    {
        private Toggle _toggle;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_toggle.IsInteractable())
                AudioManager.Instance?.PlayUIClick();
        }
    }
}
