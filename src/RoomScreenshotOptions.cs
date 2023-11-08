using Menu.Remix.MixedUI;
using RewiredConsts;

namespace RoomScreenshot;

public class RoomScreenshotOptions : OptionInterface
{
    public readonly Configurable<bool> HideHUD;
    public readonly Configurable<int> FramesDelay;
    public readonly Configurable<KeyCode> KeyCode;

    public static readonly RoomScreenshotOptions Instance = new RoomScreenshotOptions();

    public RoomScreenshotOptions()
    {
        HideHUD = config.Bind("HideHUD", true);
        FramesDelay = config.Bind("FramesDelay", 5, new ConfigAcceptableRange<int>(3, 20));
        KeyCode = config.Bind("KeyCode", UnityEngine.KeyCode.Backspace);
    }

    private UIelement[] elements;

    public override void Initialize()
    {
        var opTab = new OpTab(this, "Settings");
        Tabs = new[] { opTab };

        elements = new UIelement[] {
            new OpCheckBox(HideHUD, 10, 540),
            new OpLabel(45f, 540f, "Hide HUD when taking the screenshot"),
            new OpSlider(FramesDelay, new Vector2(10, 480), 50, true),
            new OpLabel(45f, 500f, "How many frames to wait for the camera to load before taking screenshot (increase this if you get wrong screenshots)"),
            new OpKeyBinder(KeyCode, new Vector2(10, 420), new Vector2(80, 30)),
            new OpLabel(100f, 420f, "Keybind")
        };

        opTab.AddItems(elements);
    }
}