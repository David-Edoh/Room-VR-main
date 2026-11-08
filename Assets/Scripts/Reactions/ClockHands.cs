using System;
using UnityEngine;

namespace ColorRoomVR
{
    public class ClockHands : MonoBehaviour
    {
        // Private serialized fields
        [SerializeField] private PaintableObject target;
        [SerializeField] private Transform hour;
        [SerializeField] private Transform minute;
        [SerializeField] private Transform second;
        [SerializeField] private float syncDuration = 1.5f;

        private bool isRunning;
        private float syncElapsed;
        private DateTime syncEndTime;
        private float hourStart;
        private float minuteStart;
        private float secondStart;
        private float hourDelta;
        private float minuteDelta;
        private float secondDelta;

        private void OnEnable()
        {
            target.OnPainted.AddListener(OnPainted);
        }

        private void OnDisable()
        {
            target.OnPainted.RemoveListener(OnPainted);
        }

        private void OnPainted()
        {
            if (isRunning) return;

            // The hands land on the first whole second after the sync ends, so the ticking picks up without a jump.
            syncEndTime = TruncateToSecond(DateTime.Now.AddSeconds(syncDuration)).AddSeconds(1);
            GetClockAngles(syncEndTime, out float hourEnd, out float minuteEnd, out float secondEnd);

            hourStart = GetClockwiseAngle(hour);
            minuteStart = GetClockwiseAngle(minute);
            secondStart = GetClockwiseAngle(second);

            // Always move forward (clockwise) to the target angle.
            hourDelta = Mathf.Repeat(hourEnd - hourStart, 360f);
            minuteDelta = Mathf.Repeat(minuteEnd - minuteStart, 360f);
            secondDelta = Mathf.Repeat(secondEnd - secondStart, 360f);

            syncElapsed = 0f;
            isRunning = true;
        }

        private void Update()
        {
            if (!isRunning) return;

            if (syncElapsed < syncDuration)
            {
                syncElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(syncElapsed / syncDuration));
                SetClockwiseAngle(hour, hourStart + hourDelta * t);
                SetClockwiseAngle(minute, minuteStart + minuteDelta * t);
                SetClockwiseAngle(second, secondStart + secondDelta * t);
                return;
            }

            // Tick once per second; the hands stay on the sync landing time until the real clock reaches it.
            DateTime now = TruncateToSecond(DateTime.Now);
            if (now < syncEndTime) now = syncEndTime;
            GetClockAngles(now, out float hourAngle, out float minuteAngle, out float secondAngle);
            SetClockwiseAngle(hour, hourAngle);
            SetClockwiseAngle(minute, minuteAngle);
            SetClockwiseAngle(second, secondAngle);
        }

        private static DateTime TruncateToSecond(DateTime time)
        {
            return new DateTime(time.Ticks - time.Ticks % TimeSpan.TicksPerSecond, time.Kind);
        }

        private static void GetClockAngles(DateTime time, out float hourAngle, out float minuteAngle, out float secondAngle)
        {
            float minutes = time.Minute + time.Second / 60f;
            float hours = time.Hour % 12 + minutes / 60f;
            hourAngle = hours * 30f;
            minuteAngle = minutes * 6f;
            secondAngle = time.Second * 6f;
        }

        // Hands point up at 0 and rotate around local Z; the clock is seen from +Z, so clockwise is negative Z.
        private static float GetClockwiseAngle(Transform hand)
        {
            return hand.localEulerAngles.z;
        }

        private static void SetClockwiseAngle(Transform hand, float angle)
        {
            hand.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
