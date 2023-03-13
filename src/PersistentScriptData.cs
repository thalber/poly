using MoonSharp.Interpreter;

namespace Poly;

public class PersistentScriptData {
	public string name;
	public LUA.Script script;
	public Dictionary<DynHookEntryPoints, LUA.DynValue> presetHooks = new();

	public PersistentScriptData(string name, Script script, Dictionary<DynHookEntryPoints, DynValue> presetHooks) {
		this.name = name;
		this.script = script;
		this.presetHooks = presetHooks;
	}
}
