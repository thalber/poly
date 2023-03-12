namespace Poly;

public partial class Mod{
	private BIE.Configuration.ConfigEntry<bool> _forceRegisterAsmCsharp = default!;
	private void _InitConfig(){
		_forceRegisterAsmCsharp = Config.Bind("main", "force_register_asmcsharp", false);
	}
}
