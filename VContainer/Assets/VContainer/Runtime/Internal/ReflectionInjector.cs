using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Entities;

namespace VContainer.Internal
{
    internal sealed class SystemReflectionInjector : ReflectionInjector
    {
        private SystemReflectionInjector(InjectTypeInfo injectTypeInfo) : base(injectTypeInfo) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemReflectionInjector Build(Type type)
        {
            var injectTypeInfo = TypeAnalyzer.AnalyzeWithCache(type);
            return new(injectTypeInfo);
        }

        /// <inheritdoc />
        protected override object CreateConstructedInstance(object[] parameterValues) =>
            TypeManager.ConstructSystem(injectTypeInfo.InjectConstructor.ConstructorInfo.DeclaringType);
    }

    internal class ReflectionInjector : IInjector
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReflectionInjector Build(Type type)
        {
            var injectTypeInfo = TypeAnalyzer.AnalyzeWithCache(type);
            return new(injectTypeInfo);
        }

        protected readonly InjectTypeInfo injectTypeInfo;

        protected ReflectionInjector(InjectTypeInfo injectTypeInfo) => this.injectTypeInfo = injectTypeInfo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Inject(object instance, IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters)
        {
            InjectFields(instance, resolver);
            InjectProperties(instance, resolver);
            InjectMethods(instance, resolver, parameters);
        }

        public object CreateInstance(IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters)
        {
            var parameterInfos  = injectTypeInfo.InjectConstructor.ParameterInfos;
            var parameterValues = CappedArrayPool<object>.Shared8Limit.Rent(parameterInfos.Length);
            try
            {
                for (var i = 0; i < parameterInfos.Length; i++)
                {
                    var parameterInfo = parameterInfos[i];
                    parameterValues[i] = resolver.ResolveOrParameter(
                        parameterInfo.ParameterType,
                        parameterInfo.Name,
                        parameters);
                }

                var instance = CreateConstructedInstance(parameterValues);
                Inject(instance, resolver, parameters);
                return instance;
            }
            catch (VContainerException ex)
            {
                throw new VContainerException(
                    ex.InvalidType,
                    $"Failed to resolve {injectTypeInfo.Type} : {ex.Message}");
            }
            finally
            {
                CappedArrayPool<object>.Shared8Limit.Return(parameterValues);
            }
        }

        protected virtual object CreateConstructedInstance(object[] parameterValues) =>
            injectTypeInfo.InjectConstructor.ConstructorInfo.Invoke(parameterValues);

        private void InjectFields(object obj, IObjectResolver resolver)
        {
            if (injectTypeInfo.InjectFields == null)
                return;

            foreach (var x in injectTypeInfo.InjectFields)
            {
                var fieldValue = resolver.Resolve(x.FieldType);
                x.SetValue(obj, fieldValue);
            }
        }

        private void InjectProperties(object obj, IObjectResolver resolver)
        {
            if (injectTypeInfo.InjectProperties == null)
                return;

            foreach (var x in injectTypeInfo.InjectProperties)
            {
                var propValue = resolver.Resolve(x.PropertyType);
                x.SetValue(obj, propValue);
            }
        }

        private void InjectMethods(object obj, IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters)
        {
            if (injectTypeInfo.InjectMethods == null)
                return;

            foreach (var method in injectTypeInfo.InjectMethods)
            {
                var parameterInfos  = method.ParameterInfos;
                var parameterValues = CappedArrayPool<object>.Shared8Limit.Rent(parameterInfos.Length);
                try
                {
                    for (var i = 0; i < parameterInfos.Length; i++)
                    {
                        var parameterInfo = parameterInfos[i];
                        parameterValues[i] = resolver.ResolveOrParameter(
                            parameterInfo.ParameterType,
                            parameterInfo.Name,
                            parameters);
                    }

                    method.MethodInfo.Invoke(obj, parameterValues);
                }
                catch (VContainerException ex)
                {
                    throw new VContainerException(
                        ex.InvalidType,
                        $"Failed to resolve {injectTypeInfo.Type} : {ex.Message}");
                }
                finally
                {
                    CappedArrayPool<object>.Shared8Limit.Return(parameterValues);
                }
            }
        }
    }
}