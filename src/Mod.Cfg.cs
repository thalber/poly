namespace Poly;

public partial class Mod{
	private BIE.Configuration.ConfigEntry<bool> _forceRegisterAsmCsharp = default!;
	private BIE.Configuration.ConfigEntry<bool> _threadedRegistration = default!;
	private void _InitConfig(){
		_forceRegisterAsmCsharp = Config.Bind("main", "force_register_asmcsharp", true);
		_threadedRegistration = Config.Bind("main", "multithread_registration", true);
	}
}
