using System.Collections.Generic;
using AutoWorld.Core;
using Datas;

namespace AutoWorld.Loading
{
    public sealed class LoadingContext
    {
        public InitConst InitConstAsset { get; set; }

        public AutoWorld.Core.Data.InitConst InitConstData { get; set; }

        public ManualTickScheduler TickScheduler { get; set; }

        public Fields FieldsAsset { get; set; }

        public FieldTransforms FieldTransformsAsset { get; set; }

        public GridMaps GridMapsAsset { get; set; }

        public Jobs JobsAsset { get; set; }

        public Tasks TasksAsset { get; set; }

        public IReadOnlyDictionary<FieldType, FieldDefinition> FieldDefinitions { get; set; }

        public ICoreEvents CoreEvents { get; set; }

        public GameSession GameSession { get; set; }

        public bool IsReady => InitConstData != null && TickScheduler != null && FieldDefinitions != null;
    }
}
