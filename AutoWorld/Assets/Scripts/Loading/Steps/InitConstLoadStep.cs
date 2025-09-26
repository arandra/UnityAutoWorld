using System;
using AutoWorld.Core;
using Datas;

namespace AutoWorld.Loading.Steps
{
    public sealed class InitConstLoadStep : ILoadStep
    {
        private readonly InitConst asset;

        public InitConstLoadStep(InitConst asset)
        {
            this.asset = asset;
        }

        public string Description => "InitConst 로딩";

        public void Run(LoadingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (asset == null)
            {
                throw new InvalidOperationException("InitConst 자산이 설정되지 않았습니다.");
            }

            if (asset.Data == null)
            {
                throw new InvalidOperationException("InitConst 데이터가 비어 있습니다.");
            }

            if (asset.Data.MillisecondPerTick < 1)
            {
                throw new InvalidOperationException("MillisecondPerTick 값은 1 이상이어야 합니다.");
            }

            context.InitConstData = asset.Data;
            context.TickScheduler = new ManualTickScheduler(asset.Data.MillisecondPerTick);
        }
    }
}
