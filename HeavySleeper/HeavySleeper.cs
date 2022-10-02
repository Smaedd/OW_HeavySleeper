using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using HarmonyLib;

namespace HeavySleeper
{
    public class HeavySleeper : ModBehaviour
    {
        private float _minLoopDuration;
        private float _maxLoopDuration;
        public bool _lightSleeperMode;

        public const float DEFAULT_LOOP_DURATION = 1320f;

        public static HeavySleeper Instance;

        private void Awake()
        {
            Instance = this;
        }

        public override void Configure(IModConfig config)
        {
            _minLoopDuration = config.GetSettingsValue<float>("Minimum Time Left (seconds)");
            _maxLoopDuration = config.GetSettingsValue<float>("Maximum Time Left (seconds)");

            _lightSleeperMode = config.GetSettingsValue<bool>("Light Sleeper Mode (Wake up at 0 seconds)");
        }
        private void Start()
        {
            Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly());

            ModHelper.Events.Player.OnPlayerAwake += (playerBody) =>
            {
                float timeLeft = Random.Range(_minLoopDuration, _maxLoopDuration);

                if (_lightSleeperMode)
                {
                    // We are waking up at second 0, so we set duration rather than offset
                    // (Offset becomes 0 in the SetSecondsRemaining call.)
                    TimeLoop._loopDuration = timeLeft;
                }

                TimeLoop.SetSecondsRemaining(timeLeft);

                ModHelper.Console.WriteLine($"Set time left to: {timeLeft}", MessageType.Debug);
            };
        }
    }
}