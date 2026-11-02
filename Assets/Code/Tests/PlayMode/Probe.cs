using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Tests
{
    /// <summary>
    /// Reflection access to game types. They live in Assembly-CSharp, which assembly-definition
    /// test assemblies cannot reference, so tests read and call them by name.
    /// </summary>
    internal static class Probe
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                             BindingFlags.DeclaredOnly;

        public static T Get<T>(object target, string member)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(member, Members);
                if (field != null)
                    return (T)field.GetValue(target);

                var property = type.GetProperty(member, Members);
                if (property != null)
                    return (T)property.GetValue(target);
            }

            throw new MissingMemberException(target.GetType().Name, member);
        }

        public static object Call(object target, string method, params object[] args)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var info = type.GetMethod(method, Members);
                if (info != null)
                    return info.Invoke(target, args);
            }

            throw new MissingMethodException(target.GetType().Name, method);
        }

        public static Component[] FindComponents(string typeName)
        {
            return Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(component => component && component.GetType().Name == typeName)
                .ToArray();
        }

        public static Component FindComponent(string typeName)
        {
            return FindComponents(typeName).FirstOrDefault();
        }
    }
}
