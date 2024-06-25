using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Input
{
    [MeansImplicitUse]
    public abstract class Command
    {
        /// <summary>
        /// The name by which this command is to be referred to
        /// </summary>
        public abstract string GetAlias();

        /// <summary>
        /// Should the command only work when the game is running. Default - true
        /// </summary>
        public virtual bool IsOnlyForGame()
        {
            return true;
        }

        /// <summary>
        /// Command execution
        /// </summary>
        /// <param name="args">Arguments for command</param>
        /// <param name="isError">Whether the command completed its work incorrectly</param>
        /// <returns>Result of command execution</returns>
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

        /// <summary>
        /// Completions of command
        /// </summary>
        /// <param name="lastArgument">Last argument</param>
        /// <param name="lastArgumentIndex">Index of last argument</param>
        /// <param name="args">All command arguments</param>
        /// <returns>List of suitable completions</returns>
        public abstract List<string> GetCompletions(string lastArgument, int lastArgumentIndex, List<string> args);
    }
}