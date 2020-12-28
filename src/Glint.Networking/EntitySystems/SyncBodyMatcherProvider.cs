using System;
using System.Collections.Generic;
using System.Reflection;
using Glint.Networking.Components;
using Nez;

namespace Glint.Networking.EntitySystems {
    /// <summary>
    /// utility class for creating matchers for SyncBody components
    /// </summary>
    public static class SyncBodyMatcherProvider {
        /// <summary>
        ///     automatically create matcher for all SyncBody subclasses
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static Matcher createMatcher(Assembly assembly) {
            var syncBodyTypes = new List<Type>();
            foreach (var type in assembly.DefinedTypes) {
                if (type.IsSubclassOf(typeof(SyncBody))) {
                    syncBodyTypes.Add(type);
                }
            }
            return new Matcher().One(syncBodyTypes.ToArray());
        }
    }
}