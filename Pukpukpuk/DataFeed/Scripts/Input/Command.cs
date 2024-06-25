using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Input
{
    [MeansImplicitUse]
    public abstract class Command
    {
        public abstract string GetAlias();

        public virtual bool IsOnlyForGame()
        {
            return true;
        }

        public string Execute(string[] args, out bool isError)
        {
            if (!Application.isPlaying && IsOnlyForGame())
            {
                isError = true;
                return "This command can only be used during game is running!";
            }

            return Execute_hided(args, out isError);
        }

        protected abstract string Execute_hided(string[] args, out bool isError);

        public abstract List<string> GetCompletions(string lastArgument, int lastArgumentIndex, List<string> args);
    }
}