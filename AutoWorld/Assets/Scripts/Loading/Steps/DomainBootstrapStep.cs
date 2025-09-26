using System;
using AutoWorld.Core;
using AutoWorld.Core.Services;

namespace AutoWorld.Loading.Steps
{
    public sealed class DomainBootstrapStep : ILoadStep
    {
        private sealed class NoOpCoreEvents : ICoreEvents
        {
            public void OnFoodConsumed(int citizenId) { }

            public void OnFoodShortage(int citizenId) { }

            public void OnSoldierLevelUp(int citizenId, int level) { }
        }

        public string Description => "도메인 부트스트랩";

        public void Run(LoadingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.IsReady)
            {
                throw new InvalidOperationException("필수 초기화 항목이 준비되지 않았습니다.");
            }

            var coreEvents = context.CoreEvents ?? new NoOpCoreEvents();
            context.GameSession = CoreBootstrapper.CreateGameSession(context.InitConstData, context.TickScheduler, context.FieldDefinitions, coreEvents);
            CoreRuntime.SetSession(context.GameSession);
            context.TickScheduler.SetTickDuration(context.InitConstData.MillisecondPerTick);
        }
    }
}
