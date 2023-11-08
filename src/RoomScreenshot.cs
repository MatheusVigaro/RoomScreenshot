using System.IO;
using Object = UnityEngine.Object;

namespace RoomScreenshot;

public static class RoomScreenshot
{
    private static bool KeyWasPressed;
    private static int OriginalCameraPos;
    private static int NextScreenshotPos = -1;
    private static bool TakingScreenshot;
    private static int ScreenshotDelay;
    private static readonly List<Texture2D> Screenshots = new();
    
    public static void Apply()
    {
        On.RainWorldGame.Update += RainWorldGame_Update;
        On.RoomCamera.GetCameraBestIndex += RoomCamera_GetCameraBestIndex;
    }

    private static void RoomCamera_GetCameraBestIndex(On.RoomCamera.orig_GetCameraBestIndex orig, RoomCamera self)
    {
        if (!TakingScreenshot)
        {
            orig(self);
        }
    }

    private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);

        var camera = self.cameras?.FirstOrDefault();
        if (camera == null) return;

        try
        {
            if (Input.GetKey(RoomScreenshotOptions.Instance.KeyCode.Value) && !TakingScreenshot)
            {
                if (!KeyWasPressed)
                {
                    ClearScreenshots();
                    
                    OriginalCameraPos = camera.currentCameraPosition;
                    NextScreenshotPos = 0;
                    ScreenshotDelay = RoomScreenshotOptions.Instance.FramesDelay.Value;
                    camera.MoveCamera(NextScreenshotPos);
                    TakingScreenshot = true;
                    HUDVisibility(camera, false);
                }

                KeyWasPressed = true;
            }
            else
            {
                KeyWasPressed = false;
            }

            if (TakingScreenshot && !camera.applyPosChangeWhenTextureIsLoaded)
            {
                if (ScreenshotDelay <= 0)
                {
                    ScreenshotDelay = RoomScreenshotOptions.Instance.FramesDelay.Value;
                    Screenshots.Add(ScreenCapture.CaptureScreenshotAsTexture());

                    if (camera.room.cameraPositions.Length > NextScreenshotPos + 1)
                    {
                        camera.MoveCamera(++NextScreenshotPos);
                    }
                    else
                    {
                        SaveScreenshots(camera);
                        ClearScreenshots();
                        TakingScreenshot = false;
                        camera.MoveCamera(OriginalCameraPos);
                        HUDVisibility(camera, true);
                    }
                }
                else
                {
                    if (ScreenshotDelay == RoomScreenshotOptions.Instance.FramesDelay.Value)
                    {
                        Futile.stage.Redraw(true, true);
                    }

                    ScreenshotDelay--;
                }
            }
        }
        catch
        {
            TakingScreenshot = false;
            HUDVisibility(camera, true);
            ClearScreenshots();
            throw;
        }
    }

    private static void HUDVisibility(RoomCamera camera, bool visible)
    {
        if (RoomScreenshotOptions.Instance.HideHUD.Value)
        {
            var containers = camera.hud?.fContainers;
            if (containers != null)
            {
                foreach (var container in containers)
                {
                    container.isVisible = visible;
                }
            }
        }

        foreach (var sLeaser in camera.spriteLeasers)
        {
            if (sLeaser.drawableObject is DeathFallGraphic)
            {
                foreach (var sprite in sLeaser.sprites)
                {
                    sprite.isVisible = visible;
                }
            }
        }
    }

    private static void ClearScreenshots()
    {
        foreach (var screenshot in Screenshots)
        {
            Object.Destroy(screenshot);
        }
        Screenshots.Clear();
    }

    private static void SaveScreenshots(RoomCamera camera)
    {
        if (camera.room.cameraPositions.Length == 0 || Screenshots.Count == 0) return;
        
        var roomName = camera.room.roomSettings.name;
        var directory = Path.Combine(Custom.RootFolderDirectory(), "RoomScreenshots");
        Directory.CreateDirectory(directory);

        var positions = new Vector2[camera.room.cameraPositions.Length];
        camera.room.cameraPositions.CopyTo(positions, 0);

        var bottomLeft = new Vector2Int(int.MaxValue, int.MaxValue);
        var topRight = new Vector2Int(int.MinValue, int.MinValue);

        for (var i = 0; i < positions.Length && i < Screenshots.Count; i++)
        {
            var position = positions[i];

            if (position.x < bottomLeft.x)
            {
                bottomLeft.x = (int)position.x;
            }
            if (position.x > topRight.x)
            {
                topRight.x = (int)position.x;
            }
            if (position.y < bottomLeft.y)
            {
                bottomLeft.y = (int)position.y;
            }
            if (position.y > topRight.y)
            {
                topRight.y = (int)position.y;
            }
        }

        var screenshotWidth = Screenshots[0].width;
        var screenshotHeight = Screenshots[0].height;

        topRight.x += screenshotWidth;
        topRight.y += screenshotHeight;

        var xOffset = 0;
        var yOffset = 0;

        if (bottomLeft.x < 0)
        {
            xOffset = Math.Abs(bottomLeft.x);
            topRight.x += xOffset;
            bottomLeft.x = 0;
        }

        if (bottomLeft.y < 0)
        {
            yOffset = Math.Abs(bottomLeft.y);
            topRight.y += yOffset;
            bottomLeft.y = 0;
        }

        var pixels = new Color32[topRight.x * topRight.y];

        for (var i = 0; i < positions.Length && i < Screenshots.Count; i++)
        {
            var position = positions[i];
            var screenshot = Screenshots[i];
            var screenPixels = screenshot.GetPixels32();

            for (var y = 0; y < screenshotHeight; y++)
            {
                for (var x = 0; x < screenshotWidth; x++)
                {
                    var pixelIndex = (int)((y + yOffset + position.y) * topRight.x + x + xOffset+ position.x);
                    pixels[pixelIndex] = screenPixels[y * screenshotWidth + x];
                }
            }
        }

        var texture = new Texture2D(topRight.x, topRight.y, TextureFormat.ARGB32, false);
        texture.SetPixels32(pixels);
        texture.Apply();

        var file = Path.Combine(directory, $"{roomName}_{DateTime.Now:yyyy'-'MM'-'dd'_'HH'-'mm'-'ss}.png");
        File.WriteAllBytes(file, texture.EncodeToPNG());

        var screensDirectory = Path.Combine(directory, "Screens");
        Directory.CreateDirectory(screensDirectory);

        for (var i = 0; i < Screenshots.Count; i++)
        {
            var screenTexture = Screenshots[i];
            var screenFile = Path.Combine(screensDirectory, $"{roomName}_{i}_{DateTime.Now:yyyy'-'MM'-'dd'_'HH'-'mm'-'ss}.png");
            File.WriteAllBytes(screenFile, screenTexture.EncodeToPNG());
        }
    }
}