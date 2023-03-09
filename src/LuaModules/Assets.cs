using MoonSharp.Interpreter;

namespace Poly.LuaModules;

[LUA.MoonSharpModule]
public static class Assets {
	private static TXT.StringBuilder __sharedSB = new();

	[LUA.MoonSharpModuleMethod]
	public static LUA.DynValue resolve_filepath(LUA.ScriptExecutionContext context, LUA.CallbackArguments args) {

		if (args.Count < 1) return LUA.DynValue.NewNil();
		try {
			var res = AssetManager.ResolveFilePath(args[0].ToString());
			return LUA.DynValue.NewString(res);
		}
		catch (Exception ex) {
			Bepin.log_error(context, new(new List<DynValue>() { LUA.DynValue.NewString(ex.Message) }, false));
			return LUA.DynValue.NewNil();
		}
	}
	[LUA.MoonSharpModuleMethod]
	public static LUA.DynValue list_directory(LUA.ScriptExecutionContext context, LUA.CallbackArguments args) {
		if (args.Count < 1) return new[] { LUA.DynValue.NewNil(), "No files found".ToStrDyn() }.ToTupleDyn();
		try {
			var res = AssetManager.ListDirectory(args[0].ToString());
			return res.Select(x => LUA.DynValue.NewString(x)).ToArray().ToTableDyn(context.OwnerScript);
		}
		catch (Exception ex) {
			return TupleDyn(LUA.DynValue.Nil, $"Exception: {ex.Message}".ToStrDyn());
		}
	}
	[LUA.MoonSharpModuleMethod]
	public static LUA.DynValue asset_as_string(LUA.ScriptExecutionContext context, LUA.CallbackArguments args) {
		string fp = "";
		try {
			fp = AssetManager.ResolveFilePath(args[1].ToString());
			if (!IO.File.Exists(fp)) return new[] { LUA.DynValue.NewNil(), LUA.DynValue.NewString("file does not exist") }.ToTupleDyn();
			return LUA.DynValue.NewString(IO.File.ReadAllText(fp));
		}
		catch (Exception ex) {
			return new[] { LUA.DynValue.NewNil(), $"Exception: {ex.ToString()}\n(input: {fp})".ToStrDyn() }.ToTupleDyn();
		}
	}
	[LUA.MoonSharpModuleMethod]
	public static LUA.DynValue asset_as_lines(LUA.ScriptExecutionContext context, LUA.CallbackArguments args) {
		string fp = "";
		try {
			fp = AssetManager.ResolveFilePath(args[1].ToString());
			if (!IO.File.Exists(fp)) return new[] { LUA.DynValue.NewNil(), LUA.DynValue.NewString("File does not exist") }.ToTableDyn(context.OwnerScript);
			return TableDyn(context.OwnerScript, IO.File.ReadAllLines(fp).Select(x => x.ToStrDyn()).ToArray());
		}
		catch (Exception ex) {
			return new[] { LUA.DynValue.NewNil(), $"Exception: {ex.ToString()}\n(input: {fp}".ToStrDyn() }.ToTupleDyn();
		}
	}
}
