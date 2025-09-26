using System;

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
                throw new InvalidOperationException("InitConst 로딩이 완료되지 않았습니다.");
            }

            if (context.FieldDefinitions == null)
            {
                throw new InvalidOperationException("필드 정의가 준비되지 않았습니다.");
            }

            context.TickScheduler.SetTickDuration(context.InitConstData.MillisecondPerTick);
        }
    }
}
