using System;
using UnityEngine.SceneManagement;

namespace AutoWorld.Loading.Steps
{
    public sealed class SceneTransitionStep : ILoadStep
    {
        private readonly string sceneName;

        public SceneTransitionStep(string sceneName)
        {
            this.sceneName = sceneName;
        }

        public string Description => "씬 전환";

        public void Run(LoadingContext context)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new InvalidOperationException("전환할 씬 이름이 비어 있습니다.");
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
