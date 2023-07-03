using System.Linq;
using UnityEngine;

namespace Yohash.FABRIK
{
  public static class FabrikEditorValues
  {
    private static Color background = new Color(0.3f, 0.3f, 0.3f);

    private static Texture2D windowBackground {
      get {
        var texture = new Texture2D(1, 1);
        var pixels = Enumerable.Repeat(background, 1).ToArray();
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
      }
    }
    private static GUIStyleState windowStyleState = new GUIStyleState {
      textColor = Color.white,
      background = windowBackground
    };
    public static GUIStyle windowStyle = new GUIStyle() {
      fontSize = 14,
      fontStyle = FontStyle.Bold,
      normal = windowStyleState,
      stretchWidth = true,
      padding = new RectOffset(8, 8, 6, 6),
      margin = new RectOffset(8, 8, 8, 0)
    };
  }
}
