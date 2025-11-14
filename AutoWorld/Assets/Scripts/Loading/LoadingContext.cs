using System.Collections.Generic;
using AutoWorld.Core;
using AutoWorld.Core.Data;
using Datas;

namespace AutoWorld.Loading
{
    public sealed class LoadingContext
    {
        public Datas.InitConsts InitConstAsset { get; set; }

        public AutoWorld.Core.Data.InitConst InitConstData { get; set; }

        public ManualTickScheduler TickScheduler { get; set; }

        public Fields FieldsAsset { get; set; }

        public FieldTransforms FieldTransformsAsset { get; set; }

        public GridMaps GridMapsAsset { get; set; }

        public Jobs JobsAsset { get; set; }

        public Tasks TasksAsset { get; set; }

        public EventActions EventActionsAsset { get; set; }

        public IReadOnlyDictionary<FieldType, FieldDefinition> FieldDefinitions { get; set; }

        public IReadOnlyDictionary<int, AutoWorld.Core.Data.GridMap> GridMapLookup { get; set; }

        public IReadOnlyDictionary<JobType, IReadOnlyList<ResourceAmount>> JobCosts { get; set; }

        public IReadOnlyList<EventAction> EventActions { get; set; }

        public IGameSession GameSession { get; set; }
        public IDebugLog DebugLog { get; set; }

        public bool IsReady => InitConstData != null && FieldDefinitions != null;
    }
}
