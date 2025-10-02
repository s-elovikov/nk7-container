using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System;

namespace Nk7.Container
{
    public static class Activator
    {
        private const string PARAMETERS = "parameters";
        private const string INSTANCE = "instance";
        private const string VALUE = "value";
        private const string ARGS = "args";

        internal delegate object ObjectActivator(params object[] args);

        private static readonly Dictionary<ConstructorInfo, ObjectActivator> _activatorsCache;
        private static readonly Dictionary<Type, Func<object>> _defaultConstructorsCache;
        private static readonly Dictionary<FieldInfo, Action<object, object>> _fieldsSettersCache;
        private static readonly Dictionary<PropertyInfo, Action<object, object>> _propertiesSettersCache;
        private static readonly Dictionary<MethodInfo, Func<object, object[], object>> _methodsInvokersCache;

        static Activator()
        {
            _activatorsCache = new Dictionary<ConstructorInfo, ObjectActivator>(128);
            _defaultConstructorsCache = new Dictionary<Type, Func<object>>(128);
            _fieldsSettersCache = new Dictionary<FieldInfo, Action<object, object>>(128);
            _methodsInvokersCache = new Dictionary<MethodInfo, Func<object, object[], object>>(128);
            _propertiesSettersCache = new Dictionary<PropertyInfo, Action<object, object>>(128);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ObjectActivator GetActivator(ConstructorInfo ctor)
        {
            if (_activatorsCache.TryGetValue(ctor, out var activator))
            {
                return activator;
            }

            var parametersInfo = ctor.GetParameters();

            // create a single parameter of type object[]
            var parameter = Expression.Parameter(typeof(object[]), ARGS);
            var argsExpressions = new Expression[parametersInfo.Length];

            // pick each arg from the params array 
            // and create a typed expression of them
            for (var i = 0; i < parametersInfo.Length; ++i)
            {
                var index = Expression.Constant(i);
                var parameterType = parametersInfo[i].ParameterType;

                var parameterAccessorExpression = Expression.ArrayIndex(parameter, index);
                var parameterCastExpression = Expression.Convert(parameterAccessorExpression, parameterType);

                argsExpressions[i] = parameterCastExpression;
            }

            // make a NewExpression that calls the
            // ctor with the args we just created
            var newExpression = Expression.New(ctor, argsExpressions);

            // create a lambda with the NewExpression
            // as body and our param object[] as arg
            var lambda = Expression.Lambda<ObjectActivator>(newExpression, parameter);
            var compiled = lambda.Compile();

            _activatorsCache[ctor] = compiled;

            return compiled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Func<object> GetDefaultConstructor(Type type)
        {
            if (_defaultConstructorsCache.TryGetValue(type, out var constructor))
            {
                return constructor;
            }

            return CreateDefaultConstructor(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Action<object, object> GetFieldSetter(FieldInfo field)
        {
            if (_fieldsSettersCache.TryGetValue(field, out var setter))
            {
                return setter;
            }

            return CreateFieldSetter(field);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Action<object, object> GetPropertySetter(PropertyInfo property)
        {
            if (_propertiesSettersCache.TryGetValue(property, out var setter))
            {
                return setter;
            }

            return CreatePropertySetter(property);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Func<object, object[], object> GetMethodInvoker(MethodInfo method)
        {
            if (_methodsInvokersCache.TryGetValue(method, out var invoker))
            {
                return invoker;
            }

            return CreateMethodInvoker(method);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<object> CreateDefaultConstructor(Type type)
        {
            var newExpression = Expression.New(type);
            var lambda = Expression.Lambda<Func<object>>(newExpression);
            var compiled = lambda.Compile();

            _defaultConstructorsCache[type] = compiled;

            return compiled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Action<object, object> CreateFieldSetter(FieldInfo field)
        {
            var instanceParameter = Expression.Parameter(typeof(object), INSTANCE);
            var valueParameter = Expression.Parameter(typeof(object), VALUE);

            var instanceCast = Expression.Convert(instanceParameter, field.DeclaringType);
            var valueCast = Expression.Convert(valueParameter, field.FieldType);

            var fieldAccess = Expression.Field(instanceCast, field);
            var assignExpression = Expression.Assign(fieldAccess, valueCast);
            var compiled = Expression.Lambda<Action<object, object>>(assignExpression, instanceParameter, valueParameter).Compile();

            _fieldsSettersCache[field] = compiled;

            return compiled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Action<object, object> CreatePropertySetter(PropertyInfo property)
        {
            var instanceParameter = Expression.Parameter(typeof(object), INSTANCE);
            var valueParameter = Expression.Parameter(typeof(object), VALUE);

            var instanceCast = Expression.Convert(instanceParameter, property.DeclaringType);
            var valueCast = Expression.Convert(valueParameter, property.PropertyType);

            var setterCall = Expression.Call(instanceCast, property.GetSetMethod(true), valueCast);
            var compiled = Expression.Lambda<Action<object, object>>(setterCall, instanceParameter, valueParameter).Compile();

            _propertiesSettersCache[property] = compiled;

            return compiled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<object, object[], object> CreateMethodInvoker(MethodInfo method)
        {
            var instanceParameter = Expression.Parameter(typeof(object), INSTANCE);
            var parametersParameter = Expression.Parameter(typeof(object[]), PARAMETERS);

            var instanceCast = Expression.Convert(instanceParameter, method.DeclaringType);

            var parameterExpressions = new List<Expression>();
            var paramInfos = method.GetParameters();

            for (int i = 0; i < paramInfos.Length; i++)
            {
                var paramInfo = paramInfos[i];
                var paramAccessor = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                var paramCast = Expression.Convert(paramAccessor, paramInfo.ParameterType);

                parameterExpressions.Add(paramCast);
            }

            var methodCall = Expression.Call(instanceCast, method, parameterExpressions);

            // If method returns void, wrap the call in a block and return null
            Expression body;
            if (method.ReturnType == typeof(void))
            {
                body = Expression.Block(methodCall, Expression.Constant(null));
            }
            else
            {
                body = Expression.Convert(methodCall, typeof(object));
            }

            var compiled = Expression.Lambda<Func<object, object[], object>>(body, instanceParameter, parametersParameter).Compile();

            // Save to cache
            _methodsInvokersCache[method] = compiled;

            return compiled;
        }
    }
}