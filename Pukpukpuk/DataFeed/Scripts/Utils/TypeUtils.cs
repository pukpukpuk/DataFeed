using System;
using System.Collections.Generic;
using System.Linq;

namespace Pukpukpuk.DataFeed.Utils
{
    public static class TypeUtils
    {
        public static IEnumerable<Type> GetAllSubclasses<T>()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(
                    assembly => assembly
                        .GetTypes()
                        .Where(type => type.IsSubclassOf(typeof(T)))
                ).ToList();
        }
    }
}