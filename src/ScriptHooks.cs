namespace Poly;

internal static partial class ScriptHooks {
	internal static HashSet<PersistentScriptData> __pScripts = new();
	internal static void __AddPersScript(PersistentScriptData pscript) => __pScripts.Add(pscript);
	internal static void __ClearScripts() => __pScripts.Clear();
	internal static void __Init() {
		On.Room.Loaded += __RoomLoad;
		On.AbstractWorldEntity.Update += __AbstractObjectUpdate;
	}
	private static void __AbstractObjectUpdate(On.AbstractWorldEntity.orig_Update orig, AbstractWorldEntity self, int time) {
		orig(self, time);
		const DynHookEntryPoints site = DynHookEntryPoints.RoomLoaded;
		object[] args = new object[] { self, time };
		__InvokeLua(site, args);
	}

	private static void __RoomLoad(On.Room.orig_Loaded orig, Room self) {
		orig(self);
		const DynHookEntryPoints site = DynHookEntryPoints.RoomLoaded;
		object[] args = new[] { self };
		__InvokeLua(site, args);
	}

	private static void __InvokeLua(DynHookEntryPoints site, object[] args) {
		foreach (PersistentScriptData pd in __pScripts) {
			try {
				if (!pd.presetHooks.TryGetValue(site, out LUA.DynValue fun)) continue;
				if (fun.Type is not LUA.DataType.Function or LUA.DataType.ClrFunction) continue;
				fun.Function.Call(args);
			}
			catch (Exception ex) {
				__logger.LogError(__ErrorMessage(site, $"invoking Lua callback of {pd.name}", ex));
			}
		}
	}

	private static string __ErrorMessage(DynHookEntryPoints site, string? comment, Exception? error)
		=> string.Format("DynHooks: Error on {0}, comment: \"{1}\", error: {2}", site, comment, error);

}
