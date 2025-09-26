using System;
using Datas;

namespace AutoWorld.Loading.Steps
{
    public sealed class FieldAssetsLoadStep : ILoadStep
    {
        private readonly Fields fields;
        private readonly FieldTransforms transforms;
        private readonly Tasks tasks;

        public FieldAssetsLoadStep(Fields fields, FieldTransforms transforms, Tasks tasks)
        {
            this.fields = fields;
            this.transforms = transforms;
            this.tasks = tasks;
        }

        public string Description => "필드 관련 데이터 로딩";

        public void Run(LoadingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (fields == null)
            {
                throw new InvalidOperationException("Fields 자산이 설정되지 않았습니다.");
            }

            if (transforms == null)
            {
                throw new InvalidOperationException("FieldTransforms 자산이 설정되지 않았습니다.");
            }

            if (tasks == null)
            {
                throw new InvalidOperationException("Tasks 자산이 설정되지 않았습니다.");
            }

            context.FieldsAsset = fields;
            context.FieldTransformsAsset = transforms;
            context.TasksAsset = tasks;
        }
    }
}
