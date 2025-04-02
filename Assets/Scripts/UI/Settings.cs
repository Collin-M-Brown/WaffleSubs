using System;
using System.Buffers.Text;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class colors {
    private static readonly string onWarning = "FBFF00";
    private static readonly string onBright = "13C400";
    private static readonly string onDim = "085100";
    private static readonly string offBright = "E30000";
    private static readonly string offDim = "7E0000";
    
    public static Color HexToColor(string hex) {
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber); 
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber); 
        return new Color32(r, g, b, 255);
    }
    
    public static Color OnWarning() {
        return HexToColor(onWarning);
    }
    
    public static Color OnBright() {
        return HexToColor(onBright);
    }

    public static Color OnDim() {
        return HexToColor(onDim);
    }

    public static Color OffBright() {
        return HexToColor(offBright);
    }

    public static Color OffDim() {
        return HexToColor(offDim);
    }
}