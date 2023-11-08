using System.Security;
using System.Security.Permissions;
using BepInEx;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace RoomScreenshot;

[BepInPlugin(MOD_ID, "Room Screenshot", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    public const string MOD_ID = "vigaro.roomscreenshot";

    public bool IsInit;

    private void OnEnable()
    {
        try
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }
    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        
        MachineConnector.SetRegisteredOI(MOD_ID, RoomScreenshotOptions.Instance);

        try
        {
            if (IsInit) return;
            IsInit = true;

            RoomScreenshot.Apply();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}