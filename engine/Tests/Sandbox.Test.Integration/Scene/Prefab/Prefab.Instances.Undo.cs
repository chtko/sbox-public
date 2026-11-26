using Editor;
using System.Linq;

namespace SceneTests.Prefab;

public partial class InstancesTest
{
	[TestMethod]
	[DataRow( false )]
	[DataRow( true )]
	public void ConvertPrefabInstanceToPrefabUndoRestoresOriginal( bool nested )
	{
		const string convertedPath = "___undo_convert_target.prefab";
		using var inner = Helpers.RegisterPrefabFromJson( "__nestedPrefab.prefab", _basicPrefabSource );
		using var outer = Helpers.RegisterPrefabFromJson( "___undo_convert_outer.prefab", _outerPrefabWithNestedPrefabSource );
		var scene = new Scene();
		using var scope = scene.Push();
		var instance = GameObject.GetPrefab( nested ? "___undo_convert_outer.prefab" : "__nestedPrefab.prefab" ).Clone();
		var original = nested ? instance.Children[0] : instance;
		var originalId = original.Id;
		var originalName = original.Name;
		var session = new GameEditorSession( null, scene );
		try
		{
			var converted = session.ConvertToPrefab( [original], convertedPath, true );
			Assert.IsNotNull( converted );
			Assert.AreNotEqual( originalId, converted.Id );
			var convertedId = converted.Id;
			for ( var i = 0; i < 2; i++ )
			{
				AssertConverted();
				Assert.IsTrue( session.UndoSystem.Undo() );
				var restored = scene.Directory.FindByGuid( originalId );
				Assert.IsTrue( restored.IsValid() );
				Assert.IsFalse( restored.IsDestroyed );
				Assert.AreEqual( originalName, restored.Name );
				Assert.AreEqual( "__nestedPrefab.prefab", restored.PrefabInstanceSource, true );
				Assert.AreEqual( nested, restored.IsNestedPrefabInstanceRoot );
				Assert.IsNull( scene.Directory.FindByGuid( convertedId ) );
				Assert.IsTrue( session.UndoSystem.Redo() );
			}
			AssertConverted();

			void AssertConverted()
			{
				var current = scene.Directory.FindByGuid( convertedId );
				Assert.IsTrue( current.IsValid() );
				Assert.IsTrue( current.IsPrefabInstanceRoot );
				Assert.AreEqual( convertedPath, current.PrefabInstanceSource );
				var stale = scene.Directory.FindByGuid( originalId );
				Assert.IsTrue( stale is null || stale.IsDestroyed );
			}
		}
		finally
		{
			session.Destroy();
			if ( ResourceLibrary.Get<PrefabFile>( convertedPath ) is { } file )
				Game.Resources.Unregister( file );
		}
	}
}
