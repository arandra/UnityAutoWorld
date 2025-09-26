using System;

namespace AutoWorld.Loading
{
    public interface ILoadStep
    {
        string Description { get; }

        void Run(LoadingContext context);
    }
}
