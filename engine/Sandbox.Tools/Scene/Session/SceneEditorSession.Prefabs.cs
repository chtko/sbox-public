namespace Editor;

public partial class SceneEditorSession
{
	/// <summary>Reverts the prefab overrides on each object as one undo step.</summary>
	public void RevertPrefabInstanceChanges( IEnumerable<GameObject> objects )
	{
		var targets = objects.Where( x => x.IsValid() && x.IsPrefabInstance ).ToArray();
		if ( targets.Length == 0 ) return;

		using var sceneScope = Scene.Push();
		using var undo = UndoScope( "Revert Changes" )
			.WithGameObjectChanges( targets, GameObjectUndoFlags.All ).Push();

		foreach ( var target in targets )
			EditorUtility.Prefabs.RevertGameObjectInstanceChanges( target );
	}

	/// <summary>
	/// Saves the objects as a prefab at <paramref name="path"/> and links them to it as one undo step.
	/// Several objects are grouped under a new root first. Returns the linked root, or null on failure.
	/// </summary>
	public GameObject ConvertToPrefab( IEnumerable<GameObject> objects, string path ) => ConvertToPrefab( objects, path, false );

	internal GameObject ConvertToPrefab( IEnumerable<GameObject> objects, string path, bool skipDiskWrite )
	{
		var selection = objects.Where( x => x.IsValid() ).ToArray();
		if ( selection.Length == 0 ) return null;

		var first = selection[0];

		using var sceneScope = Scene.Push();
		var undo = UndoScope( "Convert to Prefab" )
			.WithGameObjectChanges( selection, GameObjectUndoFlags.All )
			.WithGameObjectCreations();

		// Converting a prefab instance replaces it with a clone
		if ( selection.Length == 1 && first.IsPrefabInstance )
			undo = undo.WithGameObjectDestructions( first );

		using ( undo.Push() )
		{
			var root = first;
			if ( selection.Length > 1 )
			{
				root = new GameObject();
				root.WorldTransform = first.WorldTransform;
				foreach ( var gameObject in selection )
					gameObject.SetParent( root, true );
			}

			root.Name = System.IO.Path.GetFileNameWithoutExtension( path );
			return EditorUtility.Prefabs.ConvertGameObjectToPrefab( root, path, skipDiskWrite );
		}
	}
}
