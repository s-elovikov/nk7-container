using UnityEngine;
using UnityEditor;
using System;

namespace Nk7.Container.Editor
{
    [InitializeOnLoad]
    public class ScriptOrderUtils
    {
        static ScriptOrderUtils()
        {
            Initialize();
        }

        private static void Initialize()
        {
            var allRuntimeMonoScripts = MonoImporter.GetAllRuntimeMonoScripts();

            for (int i = 0; i < allRuntimeMonoScripts.Length; ++i)
            {
                var monoScript = allRuntimeMonoScripts[i];
                var monoScriptClass = monoScript.GetClass();

                if (monoScriptClass == null)
                {
                    continue;
                }

                var customAttributes = Attribute.GetCustomAttributes(monoScriptClass, typeof(DefaultExecutionOrder));

                for (int j = 0; j < customAttributes.Length; ++j)
                {
                    var attribute = customAttributes[j];

                    int currentOrder = MonoImporter.GetExecutionOrder(monoScript);
                    int newOrder = ((DefaultExecutionOrder)attribute).order;

                    if (currentOrder == newOrder)
                    {
                        continue;
                    }

                    MonoImporter.SetExecutionOrder(monoScript, newOrder);
                }
            }
        }
    }
}