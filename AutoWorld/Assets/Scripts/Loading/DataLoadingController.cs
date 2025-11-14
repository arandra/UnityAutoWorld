using System.Collections.Generic;
using AutoWorld.Core;
using AutoWorld.Game;
using AutoWorld.Loading.Steps;
using Datas;
using UnityEngine;

namespace AutoWorld.Loading
{
    public sealed class DataLoadingController : MonoBehaviour
    {
        [SerializeField] private InitConsts initConstAsset;
        [SerializeField] private Fields fieldsAsset;
        [SerializeField] private FieldTransforms fieldTransformsAsset;
        [SerializeField] private GridMaps gridMapsAsset;
        [SerializeField] private Jobs jobsAsset;
        [SerializeField] private Tasks tasksAsset;
        [SerializeField] private EventActions eventActionsAsset;
        [SerializeField] private string nextSceneName = "Game";
        [SerializeField] private DebugLog debugLog;

        private void Start()
        {
            var context = new LoadingContext
            {
                InitConstAsset = initConstAsset,
                FieldsAsset = fieldsAsset,
                FieldTransformsAsset = fieldTransformsAsset,
                GridMapsAsset = gridMapsAsset,
                JobsAsset = jobsAsset,
                TasksAsset = tasksAsset,
                EventActionsAsset = eventActionsAsset,
                DebugLog = debugLog
            };

            var steps = new List<ILoadStep>
            {
                new InitConstLoadStep(initConstAsset),
                new FieldAssetsLoadStep(fieldsAsset, fieldTransformsAsset, tasksAsset, gridMapsAsset, jobsAsset, eventActionsAsset),
                new FieldDefinitionBuildStep(),
                new DomainBootstrapStep(),
                new SceneTransitionStep(nextSceneName)
            };

            var pipeline = new DataLoadingPipeline(steps);
            pipeline.Run(context);
        }

    }
}
