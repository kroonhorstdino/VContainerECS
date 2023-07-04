using System;
using System.Collections.Concurrent;
using System.Reflection;
#if VCONTAINER_ECS_INTEGRATION_1_0
using Unity.Entities;
using UnityEngine;
#endif

namespace VContainer.Internal
{
    internal static class InjectorCache
    {
        private static readonly ConcurrentDictionary<Type, IInjector> Injectors = new();

        public static IInjector GetOrBuild(Type type)
        {
            return Injectors.GetOrAdd(
                type,
                key =>
                {
                    // SourceGenerator
                    var generatedType = key.Assembly.GetType($"{key.FullName}GeneratedInjector", false);
                    if (generatedType != null)
                        return (IInjector) Activator.CreateInstance(generatedType);

                    // IL weaving (Deprecated)
                    var getter = key.GetMethod("__GetGeneratedInjector", BindingFlags.Static | BindingFlags.Public);
                    if (getter != null)
                        return (IInjector) getter.Invoke(null, null);

                #if VCONTAINER_ECS_INTEGRATION_1_0
                    if (type.IsSubclassOf(typeof(ComponentSystemBase)))
                    {
                        Debug.Log($"Test {type}");
                        return SystemReflectionInjector.Build(key);
                    }
                #endif
                    return ReflectionInjector.Build(key);
                });
        }
    }
}