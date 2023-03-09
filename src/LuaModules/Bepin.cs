namespace Poly.LuaModules;

[LUA.MoonSharpModule]
public static class Bepin {
	private static TXT.StringBuilder __sharedSB = new();
	[LUA.MoonSharpModuleMethod]
	public static LUA.DynValue log_message(LUA.ScriptExecutionContext context, LUA.CallbackArguments args) {
		__logArgs(LOG.LogLevel.Message, args);
		return LUA.DynValue.NewNil();
	}
	[LUA.MoonSharpModuleMethod]
	public static LUA.DynValue log_error(LUA.ScriptExecutionContext context, LUA.CallbackArguments args) {
		__logArgs(LOG.LogLevel.Error, args);
		return LUA.DynValue.NewNil();
	}
	[LUA.MoonSharpModuleMethod]
	public static LUA.DynValue log_debug(LUA.ScriptExecutionContext context, LUA.CallbackArguments args) {
		__logArgs(LOG.LogLevel.Debug, args);
		return LUA.DynValue.NewNil();
	}
	private static void __logArgs(LOG.LogLevel sev, LUA.CallbackArguments args) {
		lock (__sharedSB) {
			__sharedSB.Clear();
			for (int i = 0; i < args.Count; i++) {
				__sharedSB.Append(args[i]);
				__sharedSB.Append(", ");
			}
			if (args.Count != 0) __sharedSB.Length -= 2;
			__logger.Log(sev, __sharedSB.ToString());
		}
	}
}
