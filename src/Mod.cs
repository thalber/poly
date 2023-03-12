using MonoMod.Cil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;

#pragma warning disable CS0618
[assembly: System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum)]
#pragma warning restore CS0618

namespace Poly;

using static MoonSharp.Interpreter.ModuleRegister;

[BIE.BepInPlugin("rwmodding.coreorg.poly", "Poly", "0.1")]
public partial class Mod : BIE.BaseUnityPlugin {

	//todo: register all types in assemblycsharp for interop
	internal static BIE.Logging.ManualLogSource __logger = default!;
	internal RFL.Assembly _asmcshaprAsm = typeof(AboveCloudsView).Assembly;
	internal RFL.Assembly _unitycoreAsm = typeof(Vector2).Assembly;
	internal Dictionary<Type, LUA.Interop.IUserDataDescriptor> _typeDescriptors = new();
	internal Dictionary<Type, LUA.DynValue> _staticDescriptors = new();
	internal THR.Tasks.Task? _collectDescriptorsTask = null;
	internal ILHook? _fieldOverloads;

	public void OnEnable() {
		try {

			On.RainWorld.OnModsInit += Init; Logger.LogMessage("Poly is running MoonSharp!\n" + LUA.Script.GetBanner());
			LUA.Script.WarmUp();
			LUA.Script.DefaultOptions.ScriptLoader = new AssetScriptLoader();
			LUA.UserData.DefaultAccessMode = LUA.InteropAccessMode.Preoptimized;
			LUA.UserData.RegistrationPolicy = LUA.Interop.InteropRegistrationPolicy.Automatic;
			_fieldOverloads = new(typeof(LUA.Interop.StandardUserDataDescriptor).GetMethod("FillMemberList", BF_ALL_CONTEXTS_INSTANCE), SupportFieldOverloads);
			_fieldOverloads.Apply();
			//LUA.UserData.RegisterType()
			_collectDescriptorsTask = THR.Tasks.Task.Run(() => RegisterDescriptors());
		}
		catch (Exception ex) {
			Logger.LogFatal(ex);
		}

	}
	public void SupportFieldOverloads(ILContext context) {
		string GetUniqueName<TM>
			(LUA.Interop.IUserDataDescriptor self, TM member)
			where TM : RFL.MemberInfo {
			//Logger.LogDebug()
			Type inType = self.Type;
			IEnumerable<TM> fields = inType.GetMembers(BF_ALL_CONTEXTS_INSTANCE).Where(x => x is TM && x.Name == member.Name).Select(x => (x as TM)!);
			if (fields.Count() < 2) {
				return member.Name;
			}
			string res = $"{member.DeclaringType.Name}_{member.Name}";
			Logger.LogDebug(res);
			return res;
		}
		ILCursor c = new(context);
		Logger.LogDebug("ilhook go");
		c.GotoNext(MoveType.Before,
			x => x.MatchCallvirt<RFL.MemberInfo>("get_Name"),
			x => x.MatchLdloc(14),
			x => x.MatchLdarg(0),
			x => x.MatchCall<LUA.Interop.StandardUserDataDescriptor>("get_AccessMode"),
			x => x.MatchCall<LUA.Interop.FieldMemberDescriptor>("TryCreateIfVisible")
			);
		Logger.LogDebug("ilhook inj");
		c.Index--;
		Logger.LogDebug("ilhook rm1");
		c.Emit(OpCodes.Ldarg_0);
		c.Index += 2;
		c.Prev.OpCode = OpCodes.Nop;
		c.Prev.Operand = null;
		Logger.LogDebug("ilhook rm2");
		c.EmitDelegate((GetUniqueName<RFL.FieldInfo>));
		Logger.LogDebug("ilhook done");
		Logger.LogDebug(context.ToString());
	}
	public void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self) {
		try {
			orig(self);
			__logger = Logger;
			try {
				THR.Tasks.Task.WaitAll(_collectDescriptorsTask);
			}
			catch (Exception ex) {
				__logger.LogError(ex);
			}
			finally {
				_collectDescriptorsTask = null;
			}
			const string startup_folder = "lua-startup";
			foreach (string file in AssetManager.ListDirectory(startup_folder, false, false)) {
				try {
					LUA.DynValue crw_lua = LUA.UserData.Create(self);
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
	public void RegisterDescriptors() {
		void registerType(Type t) {
			List<THR.Tasks.Task> nestedTasks = new();
			lock (_typeDescriptors) {
				_typeDescriptors[t] = LUA.UserData.RegisterType(t);
			}
			lock (_staticDescriptors) {
				_staticDescriptors[t] = LUA.UserData.CreateStatic(t);
			}
			foreach (Type nested in t.GetNestedTypes(BF_ALL_CONTEXTS)) {
				nestedTasks.Add(THR.Tasks.Task.Run(() => registerType(nested)));
			}
			THR.Tasks.Task.WaitAll(nestedTasks.ToArray());
		}
		void scanTypes(List<THR.Tasks.Task> typeTasks, IEnumerable<Type> types) {
			foreach (Type t in types) {
				typeTasks.Add(THR.Tasks.Task.Run(() => registerType(t)));
			}
		}
		List<THR.Tasks.Task> typeTasks = new();
		scanTypes(typeTasks, typeof(AboveCloudsView).Assembly.GetTypesSafe(out RFL.ReflectionTypeLoadException? err));
		scanTypes(typeTasks, typeof(Vector2).Assembly.GetTypesSafe(out RFL.ReflectionTypeLoadException? err1));
		THR.Tasks.Task.WaitAll(typeTasks.ToArray());
	}
	public static LUA.Script InitBlankScript() {
		LUA.Script script = new(LUA.CoreModules.Preset_Default);
		script.Globals["bepin"] = script.Globals.RegisterModuleType(typeof(LuaModules.Bepin));
		script.Globals["assets"] = script.Globals.RegisterModuleType(typeof(LuaModules.Assets));
		return script;
	}
}
