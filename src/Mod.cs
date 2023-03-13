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
	internal static BIE.Logging.ManualLogSource __logger = default!;
	internal static Dictionary<Type, LUA.Interop.IUserDataDescriptor> __typeDescriptors = new();
	internal static Dictionary<Type, LUA.DynValue> __staticDescriptors = new();
	internal THR.Tasks.Task? _collectDescriptorsTask = null;
	internal ILHook? _fieldOverloads;

	public void OnEnable() {
		try {
			_InitConfig();
			__logger = Logger;
			On.RainWorld.OnModsInit += _Init; Logger.LogMessage("Poly is running MoonSharp!\n" + LUA.Script.GetBanner());
			DynamicHooks.__Init();
			LUA.Script.WarmUp();
			LUA.Script.DefaultOptions.ScriptLoader = new AssetScriptLoader();
			//LUA.UserData.DefaultAccessMode = LUA.InteropAccessMode.Preoptimized;
			LUA.UserData.RegistrationPolicy = LUA.Interop.InteropRegistrationPolicy.Automatic;
			_fieldOverloads = new(typeof(LUA.Interop.StandardUserDataDescriptor).GetMethod("FillMemberList", BF_ALL_CONTEXTS_INSTANCE), _SupportFieldOverloads);
			//_fieldOverloads.Apply();
			//LUA.UserData.RegisterType()
			if (_forceRegisterAsmCsharp.Value) _RegisterDescriptors();
			//_collectDescriptorsTask = THR.Tasks.Task.Run(() => _RegisterDescriptors());
		}
		catch (Exception ex) {
			Logger.LogFatal(ex);
		}
	}
	private void _SupportFieldOverloads(ILContext context) {
		string GetUniqueName<TM>
			(LUA.Interop.IUserDataDescriptor self, TM member)
			where TM : RFL.MemberInfo {
			//Logger.LogDebug()
			Type inType = self.Type;
			IEnumerable<TM> fields = inType.GetMembers(BF_ALL_CONTEXTS_INSTANCE).Where(x => x is TM && x.Name == member.Name).Select(x => (x as TM)!);
			if (fields.Count() < 2) {
				return member.Name;
			}
			string res = $"{typeof(TM).Name} rename {member.Name} into {member.DeclaringType.Name}_{member.Name}";
			Logger.LogDebug(res);
			return res;
		}
		ILCursor c = new(context);
		Logger.LogDebug("ilhook 0 go");
		c.GotoNext(MoveType.Before,
			x => x.MatchCallvirt<RFL.MemberInfo>("get_Name"),
			x => x.MatchLdloc(14),
			x => x.MatchLdarg(0),
			x => x.MatchCall<LUA.Interop.StandardUserDataDescriptor>("get_AccessMode"),
			x => x.MatchCall<LUA.Interop.FieldMemberDescriptor>("TryCreateIfVisible")
			);
		Logger.LogDebug("ilhook 0 inj");
		c.Index--;
		Logger.LogDebug("ilhook 0 rm1");
		c.Emit(OpCodes.Ldarg_0);
		c.Index += 2;
		c.Prev.OpCode = OpCodes.Nop;
		c.Prev.Operand = null;
		Logger.LogDebug("ilhook 0 rm2");
		c.EmitDelegate((GetUniqueName<RFL.FieldInfo>));
		Logger.LogDebug("ilhook 0 done");
		//Logger.LogDebug(context.ToString());
		c.Index = 0;
		Logger.LogDebug("ilhook 1 go");
		c.GotoNext(MoveType.Before,
			x => x.MatchCallvirt<RFL.MemberInfo>("get_Name"),
			x => x.MatchLdloc(12),
			x => x.MatchLdarg(0),
			x => x.MatchCall<LUA.Interop.StandardUserDataDescriptor>("get_AccessMode"),
			x => x.MatchCall<LUA.Interop.PropertyMemberDescriptor>("TryCreateIfVisible")
			);
		Logger.LogDebug("ilhook 1 inj");
		c.Index--;
		Logger.LogDebug("ilhook 1 rm1");
		c.Emit(OpCodes.Ldarg_0);
		c.Index += 2;
		c.Prev.OpCode = OpCodes.Nop;
		c.Prev.Operand = null;
		Logger.LogDebug("ilhook 1 rm2");
		c.EmitDelegate((GetUniqueName<RFL.PropertyInfo>));
		Logger.LogDebug("ilhook 1 done");
		Logger.LogDebug(context.ToString());
	}
	private void _Init(On.RainWorld.orig_OnModsInit orig, RainWorld self) {
		try {
			orig(self);
			try {
				if (_collectDescriptorsTask is not null) THR.Tasks.Task.WaitAll(_collectDescriptorsTask);
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
					//LUA.DynValue crw_lua = LUA.UserData.Create(self);
					IO.FileInfo fi = new(file);
					if (fi.Extension is not ".lua") continue;
					__logger.LogMessage($"Running startup script {fi.Name}");
					LUA.Script startupscript = InitBlankScript();
					startupscript.Globals["rainworld"] = self;
					LUA.DynValue ret = startupscript.DoFile($"{startup_folder}/{fi.Name}");
					
					
					//if (ret is null || ret.Type is not LUA.DataType.Table) continue;
					//LUA.Table returns = ret.Table;
					Dictionary<DynHookEntryPoints, LUA.DynValue> presetHooks = new();
					
					foreach (DynHookEntryPoints ep in Enum.GetValues(typeof(DynHookEntryPoints))){
						if (startupscript.Globals.Get(ep.ToString()) == LUA.DynValue.Nil) continue;
						Logger.LogDebug($"{fi.Name} registering hook callback for {ep}");
						presetHooks[ep] = startupscript.Globals.Get(ep.ToString());
					}

					// foreach (LUA.TablePair pair in returns.Pairs) {
					// 	if (!Enum.TryParse<DynHookEntryPoints>(pair.Key.ToString(), out DynHookEntryPoints ep)) continue;
					// 	Logger.LogDebug($"{fi.Name} registering hook callback for {ep}");
					// 	presetHooks[ep] = pair.Value;
					// }
					if (presetHooks.Count is 0) continue;
					PersistentScriptData psd = new(fi.Name, startupscript, presetHooks);
					DynamicHooks.__pScripts.Add(psd);
				}
				catch (Exception ex) {
					__logger.LogError($"Error on startup script execution: {ex}");
				}
			}
		}
		catch (Exception ex) {
			__logger.LogFatal(ex);
		}
	}
	private void _RegisterDescriptors() {
		void registerType(Type t) {
			//List<THR.Tasks.Task> nestedTasks = new();
			lock (__typeDescriptors) {

				try { __typeDescriptors[t] = LUA.UserData.RegisterType(t); }
				catch (Exception ex) { __logger.LogError($"{t.FullName} : {ex}"); }
			}
			lock (__staticDescriptors) {
				try { __staticDescriptors[t] = LUA.UserData.CreateStatic(t); }
				catch (Exception ex) { __logger.LogError($"{t.FullName} : {ex}"); }
			}
			foreach (Type nested in t.GetNestedTypes(BF_ALL_CONTEXTS)) {
				registerType(nested);
				//nestedTasks.Add(THR.Tasks.Task.Run(() => registerType(nested)));
			}
			//THR.Tasks.Task.WaitAll(nestedTasks.ToArray());
		}
		void scanTypes(List<THR.Tasks.Task> typeTasks, IEnumerable<Type> types) {
			foreach (Type t in types) {
				if (t is null) continue;
				//typeTasks.Add(THR.Tasks.Task.Run(() => registerType(t)));
				try { registerType(t); }
				catch (Exception ex) { __logger.LogError($"{t?.FullName} : {ex}"); }
			}
		}
		List<THR.Tasks.Task> typeTasks = new();
		scanTypes(typeTasks, typeof(AboveCloudsView).Assembly.GetTypesSafe(out RFL.ReflectionTypeLoadException? err));
		scanTypes(typeTasks, typeof(Vector2).Assembly.GetTypesSafe(out RFL.ReflectionTypeLoadException? err1));
		THR.Tasks.Task.WaitAll(typeTasks.ToArray());
	}
	public static LUA.Script InitBlankScript() {
		LUA.Script script = new(LUA.CoreModules.Preset_Default);
		LUA.Table table = new(script);
		lock (__staticDescriptors) {
			foreach (KeyValuePair<Type, LUA.DynValue> kvp in __staticDescriptors) {
				table[kvp.Key.Name] = kvp.Value;
			}
		}
		script.Globals["bepin"] = script.Globals.RegisterModuleType(typeof(LuaModules.Bepin));
		script.Globals["assets"] = script.Globals.RegisterModuleType(typeof(LuaModules.Assets));
		script.Globals["hooks"] = script.Globals.RegisterModuleType(typeof(LuaModules.Hooks));
		script.Globals["statics"] = table;
		return script;
	}
}
