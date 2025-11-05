using System.Linq;
using UnityEngine;
using AutoWorld.Core;

namespace AutoWorld.Game
{
    public sealed class GameLoopController : MonoBehaviour
    {
        private IGameSession session;
        private double elapsedMillis;

        [SerializeField]
        private GridVisualizer gridVisualizer;

        private void Start()
        {
            if (gridVisualizer == null)
            {
                gridVisualizer = FindObjectsByType<GridVisualizer>(FindObjectsSortMode.InstanceID).First();
            }

            if (CoreRuntime.HasSession)
            {
                session = CoreRuntime.Session;
                if (gridVisualizer != null)
                {
                    gridVisualizer.SetSession(session);
                }
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
