using System.Collections.Generic;
using AutoWorld.Loading.Steps;
using Datas;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AutoWorld.Loading
{
    public sealed class DataLoadingController : MonoBehaviour
    {
        [SerializeField] private InitConst initConstAsset;
        [SerializeField] private Fields fieldsAsset;
        [SerializeField] private FieldTransforms fieldTransformsAsset;
        [SerializeField] private Tasks tasksAsset;
        [SerializeField] private string nextSceneName;

        private void Start()
        {
            var steps = new List<ILoadStep>
            {
                new InitConstLoadStep(initConstAsset),
                new FieldAssetsLoadStep(fieldsAsset, fieldTransformsAsset, tasksAsset),
                new FieldDefinitionBuildStep(),
                new DomainBootstrapStep(),
                new SceneTransitionStep(nextSceneName)
            };

            var pipeline = new DataLoadingPipeline(steps);
            var context = new LoadingContext();

            pipeline.Run(context);
        }
    }
}
