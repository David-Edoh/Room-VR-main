using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ColorRoomVR
{
    /// <summary>
    /// Step-by-step tutorial shown the first time the game runs. Can be replayed with <see cref="StartTutorial"/>.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public enum AdvanceTrigger { NextButton, ColorChanged, ObjectPainted }

        [Serializable]
        public class Step
        {
            public string title;
            [TextArea] public string body;
            public AdvanceTrigger advanceOn;
        }

        private const string DonePrefKey = "ColorRoom_TutorialDone";

        [Header("References")]
        [SerializeField] private ColorPaletteController palette;
        [SerializeField] private WorldPanelPlacer placer;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text stepCounterText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button doneButton;

        [Tooltip("Used to block painting while the tutorial hasn't reached the step that unlocks it.")]
        [SerializeField] private ObjectDetection objectDetection;

        [Header("Steps")]
        [Tooltip("Index (0-based) of the first step where the palette is shown and painting is allowed.")]
        [SerializeField, Min(0)] private int unlockAtStep = 1;
        [SerializeField] private Step[] steps =
        {
            new Step { title = "Welcome!", body = "Choose a surface finish from the selector on your left hand. You can switch to the color wheel whenever you like.", advanceOn = AdvanceTrigger.NextButton },
            new Step { title = "Choose a finish", body = "Pick a texture swatch on your left hand, or open COLOR WHEEL to paint with a solid color.", advanceOn = AdvanceTrigger.NextButton },
            new Step { title = "Apply a design", body = "Aim at an object with your right hand and press the trigger to apply the selected finish or color.", advanceOn = AdvanceTrigger.ObjectPainted },
            new Step { title = "Things react", body = "Some objects come alive when painted. Try to find them!", advanceOn = AdvanceTrigger.NextButton },
            new Step { title = "Track your progress", body = "The counter next to the palette shows how many objects you have painted.", advanceOn = AdvanceTrigger.NextButton },
            new Step { title = "Have fun!", body = "Switch between textures and the color wheel from the selector on your left hand. Rearrange furniture with MOVE.", advanceOn = AdvanceTrigger.NextButton },
        };

        private int _index = -1;
        private CanvasGroup _paletteGroup;

        private void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            if (palette != null)
            {
                var canvas = palette.GetComponentInParent<Canvas>();
                if (canvas != null && !canvas.TryGetComponent(out _paletteGroup))
                    _paletteGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            }

            // Lock before the first frame so nothing can be painted while the tutorial is about to start.
            if (PlayerPrefs.GetInt(DonePrefKey, 0) == 0)
                SetLocked(true);
        }

        // Hides the palette and blocks painting while locked.
        private void SetLocked(bool locked)
        {
            if (_paletteGroup != null)
            {
                _paletteGroup.alpha = locked ? 0f : 1f;
                _paletteGroup.interactable = !locked;
                _paletteGroup.blocksRaycasts = !locked;
            }
            if (objectDetection != null)
                objectDetection.PaintingEnabled = !locked;
        }

        private void OnEnable()
        {
            if (nextButton != null) nextButton.onClick.AddListener(Next);
            if (skipButton != null) skipButton.onClick.AddListener(Finish);
            if (doneButton != null) doneButton.onClick.AddListener(Finish);
            if (palette != null) palette.OnColorChange.AddListener(OnColorPicked);
        }

        private void OnDisable()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(Next);
            if (skipButton != null) skipButton.onClick.RemoveListener(Finish);
            if (doneButton != null) doneButton.onClick.RemoveListener(Finish);
            if (palette != null) palette.OnColorChange.RemoveListener(OnColorPicked);
            Unsubscribe();
            SetLocked(false);
        }

        private IEnumerator Start()
        {
            if (PlayerPrefs.GetInt(DonePrefKey, 0) == 1)
                yield break;

            // Wait a frame so the headset (or the simulator) has a valid pose before placing the panel.
            yield return null;
            StartTutorial();
        }

        public void StartTutorial()
        {
            if (steps == null || steps.Length == 0) return;

            if (placer != null)
                placer.PlaceInFront();
            if (panel != null)
                panel.SetActive(true);
            ShowStep(0);
        }

        private void ShowStep(int index)
        {
            Unsubscribe();
            _index = index;
            SetLocked(index < unlockAtStep);
            var step = steps[index];
            bool last = index == steps.Length - 1;

            if (titleText != null) titleText.text = step.title;
            if (bodyText != null) bodyText.text = step.body;
            if (stepCounterText != null) stepCounterText.text = $"{index + 1} / {steps.Length}";
            if (nextButton != null) nextButton.gameObject.SetActive(step.advanceOn == AdvanceTrigger.NextButton && !last);
            if (doneButton != null) doneButton.gameObject.SetActive(last);
            if (skipButton != null) skipButton.gameObject.SetActive(!last);

            if (step.advanceOn == AdvanceTrigger.ObjectPainted && ColorsDataManager.Instance != null)
            {
                ColorsDataManager.Instance.OnColorChanged += OnObjectPainted;
                if (InteriorDesignManager.Instance != null)
                    InteriorDesignManager.Instance.OnFinishApplied += OnObjectPainted;
            }
        }

        private void Unsubscribe()
        {
            if (ColorsDataManager.Instance != null)
                ColorsDataManager.Instance.OnColorChanged -= OnObjectPainted;
            if (InteriorDesignManager.Instance != null)
                InteriorDesignManager.Instance.OnFinishApplied -= OnObjectPainted;
        }

        private void OnColorPicked(Color _)
        {
            if (IsWaitingFor(AdvanceTrigger.ColorChanged))
                Next();
        }

        private void OnObjectPainted(string _)
        {
            if (IsWaitingFor(AdvanceTrigger.ObjectPainted))
                Next();
        }

        private bool IsWaitingFor(AdvanceTrigger trigger) =>
            _index >= 0 && panel != null && panel.activeSelf && steps[_index].advanceOn == trigger;

        private void Next()
        {
            if (_index < steps.Length - 1)
                ShowStep(_index + 1);
        }

        private void Finish()
        {
            Unsubscribe();
            _index = -1;
            SetLocked(false);
            PlayerPrefs.SetInt(DonePrefKey, 1);
            PlayerPrefs.Save();
            if (panel != null)
                panel.SetActive(false);
            InteriorDesignManager.Instance?.SetToolbarVisible(true);
        }
    }
}
