using UnityEditor;
using UnityEngine;

namespace Nk7.Container.Editor
{
    public static class ContainerMenu
    {
        [MenuItem("Tools/Nk7/Container/CompositionRoot", false, 10)]
		public static void CreateCompositionRoot(MenuCommand menuCommand)
		{
			var go = new GameObject("CompositionRoot");

			go.AddComponent<CompositionRoot>();

			GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            
			Selection.activeObject = go;
		}

		[MenuItem("Tools/Nk7/Container/SubContainer", false, 10)]
		public static void CreateSceneRoot(MenuCommand menuCommand)
		{
			var go = new GameObject("SubContainer");

			go.AddComponent<SubContainer>();

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

			Selection.activeObject = go;
		}
    }
}