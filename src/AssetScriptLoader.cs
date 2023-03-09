using MoonSharp.Interpreter;

namespace Poly;

public class AssetScriptLoader : LUA.Loaders.ScriptLoaderBase {
	/// <inheritdoc/>
	public override object LoadFile(string file, Table globalContext) {
		return IO.File.ReadAllText(AssetManager.ResolveFilePath(file));
	}
	/// <inheritdoc/>
	public override bool ScriptFileExists(string name) {
		return IO.File.Exists(AssetManager.ResolveFilePath(name));
	}
}
