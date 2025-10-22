#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor;
using System;

namespace Nk7.Container.Editor
{
    [InitializeOnLoad]
    internal static class DefineUtils
    {
        private const string DEFINE = "NK7_CONTAINER";
        private const char SEPARATOR = ';';

        static DefineUtils()
        {
            try
            {
                TryAddDefine(NamedBuildTarget.Android);
                TryAddDefine(NamedBuildTarget.iOS);
                TryAddDefine(NamedBuildTarget.NintendoSwitch);
                TryAddDefine(NamedBuildTarget.NintendoSwitch2);
                TryAddDefine(NamedBuildTarget.PS4);
                TryAddDefine(NamedBuildTarget.PS5);
                TryAddDefine(NamedBuildTarget.Standalone);
                TryAddDefine(NamedBuildTarget.EmbeddedLinux);
                TryAddDefine(NamedBuildTarget.LinuxHeadlessSimulation);
                TryAddDefine(NamedBuildTarget.QNX);
                TryAddDefine(NamedBuildTarget.Server);
                TryAddDefine(NamedBuildTarget.tvOS);
                TryAddDefine(NamedBuildTarget.VisionOS);
                TryAddDefine(NamedBuildTarget.WebGL);
                TryAddDefine(NamedBuildTarget.WindowsStoreApps);
                TryAddDefine(NamedBuildTarget.XboxOne);
            }
            catch (Exception ex)
            {
                LogsUtils.LogError($"Failed to set scripting define symbols - {DEFINE}.\n{ex}");
            }
        }

        private static bool TryAddDefine(NamedBuildTarget buildTarget)
        {
            string[] definesArray = PlayerSettings.GetScriptingDefineSymbols(buildTarget)
                                        .Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < definesArray.Length; ++i)
            {
                var define = definesArray[i].Trim();

                if (define.Equals(DEFINE))
                {
                    return false;
                }
            }

            string definesString = string.Join(SEPARATOR, definesArray) + SEPARATOR + DEFINE;
            string resultDefinesString = definesString.Length == 0
                ? DEFINE
                : definesString + SEPARATOR + DEFINE;

            PlayerSettings.SetScriptingDefineSymbols(buildTarget, resultDefinesString);

            return true;
        }
    }
}
#endif
