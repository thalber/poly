namespace Poly.LuaModules;
//tbd
[LUA.MoonSharpModule]
public static class Hooks {
	[LUA.MoonSharpModuleMethod]
	public static LUA.DynValue sample_method(LUA.ScriptExecutionContext context, LUA.CallbackArguments args) {
		return LUA.DynValue.Nil;
	}
	[LUA.MoonSharpModuleMethod]
	public static LUA.DynValue register_hook(LUA.ScriptExecutionContext context, LUA.CallbackArguments args) {
		return LUA.DynValue.Nil;
	}

}
