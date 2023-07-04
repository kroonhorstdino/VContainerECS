#if VCONTAINER_ECS_INTEGRATION
using System;
using Unity.Entities;
using VContainer.Internal;

namespace VContainer.Unity
{
    public sealed class SystemRegistrationBuilder : RegistrationBuilder
    {
        private readonly string worldName;
        private          Type   systemGroupType;

        internal SystemRegistrationBuilder(Type implementationType, string worldName) : base(
            implementationType,
            default)
        {
            this.worldName = worldName;
            InterfaceTypes = new()
            {
                typeof(ComponentSystemBase),
                implementationType
            };
        }

        public override Registration Build()
        {
            var injector = InjectorCache.GetOrBuild(ImplementationType);
            var provider = new SystemInstanceProvider(
                ImplementationType,
                worldName,
                systemGroupType,
                injector,
                Parameters);
            return new(ImplementationType, Lifetime, InterfaceTypes, provider);
        }

        public SystemRegistrationBuilder IntoGroup<T>() where T : ComponentSystemGroup
        {
            systemGroupType = typeof(T);
            return this;
        }
    }
}
#endif