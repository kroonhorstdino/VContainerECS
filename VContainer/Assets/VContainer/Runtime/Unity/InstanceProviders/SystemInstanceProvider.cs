#if VCONTAINER_ECS_INTEGRATION
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace VContainer.Unity
{
    public sealed class SystemInstanceProvider : IInstanceProvider
    {
        private readonly Type                            systemType;
        private readonly IInjector                       injector;
        private readonly IReadOnlyList<IInjectParameter> customParameters;
        private readonly string                          worldName;
        private readonly Type                            systemGroupType;

        private World               world;
        private ComponentSystemBase instance;

        public SystemInstanceProvider(
            Type                            systemType,
            string                          worldName,
            Type                            systemGroupType,
            IInjector                       injector,
            IReadOnlyList<IInjectParameter> customParameters)
        {
            this.systemType       = systemType;
            this.worldName        = worldName;
            this.systemGroupType  = systemGroupType;
            this.injector         = injector;
            this.customParameters = customParameters;
        }

        public object SpawnInstance(IObjectResolver resolver)
        {
            if (world is null)
                world = GetWorld(resolver);

            TypeManager.Initialize();

            if (instance is null)
            {
            #if VCONTAINER_ECS_INTEGRATION_1_0

                instance = world.CreateSystemManaged(systemType);
                resolver.Inject(instance);
            #else
                instance = (ComponentSystemBase) injector.CreateInstance(resolver, customParameters);
                world.AddSystem(instance);
            #endif

                if (systemGroupType != null)
                {
                #if VCONTAINER_ECS_INTEGRATION_1_0
                    var systemGroup = (ComponentSystemGroup) world.GetOrCreateSystemManaged(systemGroupType);
                #else
                    var systemGroup = (ComponentSystemGroup)world.GetOrCreateSystem(systemGroupType);
                #endif
                    systemGroup.AddSystemToUpdateList(instance);
                }

                return instance;
            }
        #if VCONTAINER_ECS_INTEGRATION_1_0
            return world.GetExistingSystemManaged(systemType);
        #else
            return world.GetExistingSystem(systemType);
        #endif
        }

        private World GetWorld(IObjectResolver resolver)
        {
            if (worldName is null && World.DefaultGameObjectInjectionWorld != null)
                return World.DefaultGameObjectInjectionWorld;

            var worlds = resolver.Resolve<IEnumerable<World>>();
            foreach (var world in worlds)
            {
                if (world.Name == worldName)
                    return world;
            }

            throw new VContainerException(systemType, $"World `{worldName}` is not registered");
        }
    }
}
#endif