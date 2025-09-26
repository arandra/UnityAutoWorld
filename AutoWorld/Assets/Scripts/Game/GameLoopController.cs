using UnityEngine;
using AutoWorld.Core;

namespace AutoWorld.Game
{
    public sealed class GameLoopController : MonoBehaviour
    {
        private GameSession session;
        private double elapsedMillis;

        private void Start()
        {
            if (CoreRuntime.HasSession)
            {
                session = CoreRuntime.Session;
            }
        }

        private void Update()
        {
            if (session == null)
            {
                return;
            }

            elapsedMillis += Time.deltaTime * 1000d;
            var duration = session.Scheduler.TickDurationMillis;
            while (elapsedMillis >= duration)
            {
                elapsedMillis -= duration;
                session.AdvanceTick();
            }
        }
    }
}
