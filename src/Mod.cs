namespace Poly;

using static MoonSharp.Interpreter.ModuleRegister;

[BIE.BepInPlugin("rwmodding.coreorg.poly", "Poly", "0.1")]
public class Mod : BIE.BaseUnityPlugin {
	//todo: register all types in assemblycsharp for interop
	internal static BIE.Logging.ManualLogSource __logger = default!;
	internal static RFL.Assembly __asmcshaprAsm = typeof(AboveCloudsView).Assembly;
	internal static RFL.Assembly __unitycoreAsm = typeof(Vector2).Assembly;
	internal static Dictionary<Type, LUA.Interop.IUserDataDescriptor> __asmCsharpDescriptors = new();
	internal static Dictionary<Type, LUA.Interop.IUserDataDescriptor> __unityEngineCoreDescriptors = new();
	internal static THR.Tasks.Task? __collectDescriptorsTask = null;
	//internal static THR.SemaphoreSlim __descriptorCollectionLock = new(0, 1);

	public void OnEnable() {
		On.RainWorld.OnModsInit += Init; Logger.LogMessage("Poly is running MoonSharp!\n" + LUA.Script.GetBanner());
		LUA.Script.WarmUp();
		LUA.Script.DefaultOptions.ScriptLoader = new AssetScriptLoader();
		LUA.UserData.DefaultAccessMode = LUA.InteropAccessMode.Preoptimized;
		__collectDescriptorsTask = RegisterDescriptors();
	}
	public async THR.Tasks.Task RegisterDescriptors() {
		async THR.Tasks.Task registerType(Type t) {
			List<THR.Tasks.Task> nestedTasks = new();
			Dictionary<Type, LUA.Interop.IUserDataDescriptor>? selectedDict = t.Assembly switch {
				_ when t.Assembly == __asmcshaprAsm => __asmCsharpDescriptors,
				_ when t.Assembly == __unitycoreAsm => __unityEngineCoreDescriptors,
				_ => null
			};
			if (selectedDict is null) return;
			lock (selectedDict) {
				if (selectedDict.ContainsKey(t)) return;
				selectedDict[t] = LUA.UserData.RegisterType(t);
			}
			foreach (Type nested in t.GetNestedTypes(BF_ALL_CONTEXTS)) {
				nestedTasks.Add(registerType(nested));
			}
			THR.Tasks.Task.WaitAll(nestedTasks.ToArray());
		}
		List<THR.Tasks.Task> typeTasks = new();
		foreach (Type t in typeof(AboveCloudsView).Assembly.GetTypesSafe(out RFL.ReflectionTypeLoadException? err)) {
			typeTasks.Add(registerType(t));
		}
		THR.Tasks.Task.WaitAll(typeTasks.ToArray());
	}

	public void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self) {
		try {
			orig(self);
			__logger = Logger;
			try {
				THR.Tasks.Task.WaitAll(__collectDescriptorsTask);
			}
			catch (AggregateException ex){
				__logger.LogError(ex);
			} 
			const string startup_folder = "lua-startup";
			foreach (string file in AssetManager.ListDirectory(startup_folder, false, false)) {
				try {
					LUA.DynValue crw_lua = LUA.UserData.Create(self, __asmCsharpDescriptors[typeof(RainWorld)]);
					IO.FileInfo fi = new(file);
					if (fi.Extension is not ".lua") continue;
					__logger.LogMessage($"Running startup script {fi.Name}");
					LUA.Script? startupscript = InitBlankScript();
					startupscript.Globals["rainworld"] = crw_lua;
					startupscript?.DoFile($"{startup_folder}/{fi.Name}");
				}
				catch (Exception ex) {
					__logger.LogError($"Error on script execution: {ex}");
				}
			}
		}
		catch (Exception ex) {
			__logger.LogFatal(ex);
		}
	}
	public static LUA.Script InitBlankScript() {
		LUA.Script script = new(LUA.CoreModules.Preset_Default);
		script.Globals["bepin"] = script.Globals.RegisterModuleType(typeof(LuaModules.Bepin));
		script.Globals["assets"] = script.Globals.RegisterModuleType(typeof(LuaModules.Assets));
		return script;
	}
	public static LUA.Script? InitScriptSource(string source) {
		LUA.Script res = InitBlankScript();
		try {
			res.LoadString(source);
			return res;
		}
		catch (Exception ex) {
			__logger.LogError($"Could not load script from source due to error: {ex}");
			return null;
		}
	}
	public static LUA.Script? InitScriptAssetpath(string asset) {
		LUA.Script res = InitBlankScript();
		try {
			res.LoadFile(asset);

			//res.Call()
			return res;
		}
		catch (Exception ex) {
			__logger.LogError($"Could not load script from file due to error: {ex}");
			return null;
		}
	}
}
