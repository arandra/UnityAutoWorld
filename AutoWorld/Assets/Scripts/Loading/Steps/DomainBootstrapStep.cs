using System;
using AutoWorld.Core;
using AutoWorld.Core.Services;

namespace AutoWorld.Loading.Steps
{
    public sealed class DomainBootstrapStep : ILoadStep
    {
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

            context.GameSession = CoreBootstrapper.CreateGameSession(
                context.InitConstData,
                context.TickScheduler,
                context.FieldDefinitions,
                context.GridMapLookup,
                context.JobCosts);
            CoreRuntime.SetSession(context.GameSession);
            context.TickScheduler.SetTickDuration(context.InitConstData.MillisecondPerTick);
        }
    }
}
