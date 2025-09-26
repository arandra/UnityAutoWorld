using System;
using System.Collections.Generic;

namespace AutoWorld.Loading
{
    public sealed class DataLoadingPipeline
    {
        private readonly List<ILoadStep> steps;

        public DataLoadingPipeline(IEnumerable<ILoadStep> steps)
        {
            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            this.steps = new List<ILoadStep>(steps);
        }

        public void Run(LoadingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (var step in steps)
            {
                step.Run(context);
            }
        }
    }
}
