namespace Poly;

using static MoonSharp.Interpreter.ModuleRegister;

[BIE.BepInPlugin("rwmodding.coreorg.poly", "Poly", "0.1")]
public class Mod : BIE.BaseUnityPlugin {
	internal static BIE.Logging.ManualLogSource __logger = default!;
	internal static THR.SemaphoreSlim __luaLock = new(0, 1);
	public int x;

	public void OnEnable() {
		On.RainWorld.OnModsInit += Init;
	}
	public void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self) {
		orig(self);
		try {
			__logger = Logger;
			Logger.LogMessage("Poly is running MoonSharp!\n" + LUA.Script.GetBanner());
			LUA.Script.WarmUp();
			LUA.Script.DefaultOptions.ScriptLoader = new AssetScriptLoader();
			//LUA.ModuleRegister.RegisterCoreModules(script.Globals, MoonSharp.Interpreter.CoreModules).
			string fp = AssetManager.ResolveFilePath("startup.lua");
			if (!IO.File.Exists(fp)) { Logger.LogError("startup file does not exist"); return; }
			LUA.Script script = new(LUA.CoreModules.Preset_Default);
			script.Globals["bepin"] = script.Globals.RegisterModuleType(typeof(LuaModules.Bepin));
			script.Globals["assets"] = script.Globals.RegisterModuleType(typeof(LuaModules.Assets));
			script.DoFile(fp);
		}
		catch (Exception ex) {
			Logger.LogFatal(ex);
		}
	}
}
