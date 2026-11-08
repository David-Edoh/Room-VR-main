using System.Collections;
using TMPro;
using UnityEngine;

namespace ColorRoomVR
{
    public class CatSpeechBubble : MonoBehaviour
    {
        // Private serialized fields
        [SerializeField] private PaintableObject target;
        [SerializeField] private TMP_Text label;
        [Tooltip("Messages shown in a loop. Only the one matching Meow Message also plays the meow sound.")]
        [SerializeField] private string[] messages = { "Meow", "Zzzz", "Zzz...", "Mmm...", "Zzzz", "Zzz..." };
        [SerializeField] private float secondsPerMessage = 3f;
        [Tooltip("Optional. With no clip the meow message is silent.")]
        [SerializeField] private AudioClip meowClip;
        private const string meowMessage = "Meow";

        private Coroutine loop;
        private Transform bubble;
        private Camera cam;

        private void Awake()
        {
            bubble = label.transform.parent;
        }

        private void OnEnable()
        {
            target.OnPainted.AddListener(OnPainted);
        }

        private void OnDisable()
        {
            target.OnPainted.RemoveListener(OnPainted);
            loop = null;
        }

        private void OnPainted()
        {
            if (loop != null || messages.Length == 0) return;
            loop = StartCoroutine(MessageLoop());
        }

        private IEnumerator MessageLoop()
        {
            var wait = new WaitForSeconds(secondsPerMessage);
            int index = 0;
            while (true)
            {
                label.text = messages[index];
                if (meowClip != null && messages[index] == meowMessage)
                    AudioManager.Instance?.Play(meowClip, transform.position);
                index = (index + 1) % messages.Length;
                yield return wait;
            }
        }

        private void LateUpdate()
        {
            if (loop == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            // The canvas reads correctly when its forward points away from the viewer.
            bubble.rotation = Quaternion.LookRotation(bubble.position - cam.transform.position, Vector3.up);
        }
    }
}
