using UnityEngine;
using System;

namespace GameTime
{
    /// <summary>
    /// Singleton manager untuk game time/day system.
    /// Track current day (Senin-Minggu) dan time (0-23 hours).
    /// </summary>
    public class GameTimeManager : MonoBehaviour
    {
        public static GameTimeManager Instance { get; private set; }

        [Header("Current Time")]
        [SerializeField] private DayOfWeek currentDay = DayOfWeek.Monday;
        [SerializeField] [Range(0, 23)] private int currentHour = 8;
        [SerializeField] [Range(0, 59)] private int currentMinute = 0;

        [Header("Time Settings")]
        [Tooltip("Berapa detik real-time = 1 jam in-game")]
        [Range(1f, 600f)]
        public float secondsPerHour = 60f;

        [Tooltip("Auto advance time atau manual")]
        public bool autoAdvanceTime = true;

        // Events - subscribe untuk listen time changes
        public event Action<DayOfWeek, int> OnHourChanged;
        public event Action<DayOfWeek> OnDayChanged;
        public event Action<int> OnMinuteChanged;

        private float timeAccumulator = 0f;

        void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            if (autoAdvanceTime)
            {
                AdvanceTime(Time.deltaTime);
            }
        }

        /// <summary>
        /// Advance time by deltaTime
        /// </summary>
        private void AdvanceTime(float deltaTime)
        {
            timeAccumulator += deltaTime;

            // Calculate berapa banyak minutes berlalu
            float minutesPerSecond = 60f / secondsPerHour;
            int minutesToAdd = Mathf.FloorToInt(timeAccumulator * minutesPerSecond);

            if (minutesToAdd > 0)
            {
                timeAccumulator -= minutesToAdd / minutesPerSecond;
                AddMinutes(minutesToAdd);
            }
        }

        /// <summary>
        /// Add minutes ke current time
        /// </summary>
        public void AddMinutes(int minutes)
        {
            int oldHour = currentHour;
            DayOfWeek oldDay = currentDay;

            currentMinute += minutes;

            // Handle minute overflow
            while (currentMinute >= 60)
            {
                currentMinute -= 60;
                currentHour++;
            }

            // Handle hour overflow
            while (currentHour >= 24)
            {
                currentHour -= 24;
                AdvanceDay();
            }

            // Trigger events
            if (currentMinute != oldHour) // Minute changed
            {
                OnMinuteChanged?.Invoke(currentMinute);
            }

            if (currentHour != oldHour) // Hour changed
            {
                OnHourChanged?.Invoke(currentDay, currentHour);
            }

            if (currentDay != oldDay) // Day changed
            {
                OnDayChanged?.Invoke(currentDay);
            }
        }

        /// <summary>
        /// Advance to next day
        /// </summary>
        private void AdvanceDay()
        {
            // Cycle through days
            int dayInt = (int)currentDay;
            dayInt++;
            
            if (dayInt > (int)DayOfWeek.Saturday)
            {
                dayInt = (int)DayOfWeek.Sunday;
            }

            currentDay = (DayOfWeek)dayInt;
        }

        /// <summary>
        /// Set current time
        /// </summary>
        public void SetTime(DayOfWeek day, int hour, int minute = 0)
        {
            DayOfWeek oldDay = currentDay;
            int oldHour = currentHour;

            currentDay = day;
            currentHour = Mathf.Clamp(hour, 0, 23);
            currentMinute = Mathf.Clamp(minute, 0, 59);

            // Trigger events jika berubah
            if (currentDay != oldDay)
            {
                OnDayChanged?.Invoke(currentDay);
            }

            if (currentHour != oldHour || currentDay != oldDay)
            {
                OnHourChanged?.Invoke(currentDay, currentHour);
            }

            OnMinuteChanged?.Invoke(currentMinute);
        }

        /// <summary>
        /// Get current day
        /// </summary>
        public DayOfWeek GetCurrentDay()
        {
            return currentDay;
        }

        /// <summary>
        /// Get current hour
        /// </summary>
        public int GetCurrentHour()
        {
            return currentHour;
        }

        /// <summary>
        /// Get current minute
        /// </summary>
        public int GetCurrentMinute()
        {
            return currentMinute;
        }

        /// <summary>
        /// Get formatted time string
        /// </summary>
        public string GetTimeString()
        {
            return $"{currentDay} {currentHour:D2}:{currentMinute:D2}";
        }

#if UNITY_EDITOR
        [ContextMenu("Add 1 Hour")]
        public void DebugAddOneHour()
        {
            AddMinutes(60);
            Debug.Log($"Time: {GetTimeString()}");
        }

        [ContextMenu("Add 1 Day")]
        public void DebugAddOneDay()
        {
            AddMinutes(24 * 60);
            Debug.Log($"Time: {GetTimeString()}");
        }

        [ContextMenu("Set to Monday 08:00")]
        public void DebugSetMondayMorning()
        {
            SetTime(DayOfWeek.Monday, 8, 0);
            Debug.Log($"Time: {GetTimeString()}");
        }
#endif
    }
}
