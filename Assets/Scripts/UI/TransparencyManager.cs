using System;
using System.Runtime.InteropServices;
using UnityEngine;

#pragma warning disable CS0162 // Unreachable code detected
public class WaffleTransparency : MonoBehaviour {
    public static WaffleTransparency Instance { get; private set; }
    
    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private struct MARGINS {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    const int GWL_EXSTYLE = -20;
    const uint WS_EX_LAYERED = 0x00080000;
    const uint WS_EX_TRANSPARENT = 0x00000020;
    private IntPtr hWnd;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(this.gameObject);
        }
        else {
            Instance = this;
        }
    }
        
    private void Start() {
#if !UNITY_EDITOR
        hWnd = GetActiveWindow();
        
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);
        
        SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, 0);
#endif
        Application.runInBackground = true;
        SetClickthrough(false);
    }
    
    public void SetClickthrough(bool clickthrough) {
        if (clickthrough) {
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        } else {
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);
        }
    }
    
    public void SetAlwaysOnTop(bool value) {
#if !UNITY_EDITOR
        IntPtr windowPosition = value ? HWND_TOPMOST : HWND_NOTOPMOST;
        // SetWindowPos flags: 
        // 0x0001 = SWP_NOSIZE (don't change size)
        // 0x0002 = SWP_NOMOVE (don't change position)
        // 0x0040 = SWP_SHOWWINDOW (show the window)
        SetWindowPos(hWnd, windowPosition, 0, 0, 0, 0, 0x0001 | 0x0002);
#endif
    }
    
    void OnApplicationFocus(bool hasFocus) {
        if (hasFocus) {
            Debug.Log("Game is now in focus");
            UIManager.Instance.OnShowUI();
        }
    }
    
}