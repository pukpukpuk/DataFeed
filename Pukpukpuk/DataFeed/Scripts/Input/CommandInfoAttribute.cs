using System;
using JetBrains.Annotations;

namespace Pukpukpuk.DataFeed.Input
{
    public class CommandInfoAttribute : Attribute
    {
        /// <summary>
        /// The name by which this command is to be referred to
        /// </summary>
        public readonly string Alias;
        /// <summary>
        /// Should the command only work when the game is running
        /// </summary>
        public readonly bool IsOnlyForGame;

        public CommandInfoAttribute(string alias, bool isOnlyForGame = true)
        {
            Alias = alias;
            IsOnlyForGame = isOnlyForGame;
        }
    }
}