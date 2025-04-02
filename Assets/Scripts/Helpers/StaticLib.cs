using UnityEngine;

public static class ColorTools {
    
    public static Color HexToColor(string hex) {
        if (hex.StartsWith("#"))
            hex = hex.Substring(1);
        
        byte r, g, b, a = 255;
        r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        
        if (hex.Length >= 8) {
            a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        }
        return new Color32(r, g, b, a);
    }

    public static string ColorToHex(Color color) {
        Color32 color32 = (Color32)color;
        return color32.r.ToString("X2") + color32.g.ToString("X2") + color32.b.ToString("X2");
    }
}