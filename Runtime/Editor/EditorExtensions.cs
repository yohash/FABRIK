using UnityEngine;

namespace Yohash.FABRIK
{
  public static class EditorExtensions
  {
    public static GUIStyle LeftAlign(this GUIStyle style)
    {
      var newStyle = new GUIStyle(style);
      newStyle.alignment = TextAnchor.MiddleLeft;
      return newStyle;
    }
    public static GUIStyle RightAlign(this GUIStyle style)
    {
      var newStyle = new GUIStyle(style);
      newStyle.alignment = TextAnchor.MiddleRight;
      return newStyle;
    }
    public static GUIStyle Bold(this GUIStyle style)
    {
      var newStyle = new GUIStyle(style);
      newStyle.fontStyle = FontStyle.Bold;
      return newStyle;
    }
  }
}