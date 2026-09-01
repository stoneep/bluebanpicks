using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class AspectRatioWindowLock : MonoBehaviour
{
    [Header("고정할 비율 (예: 16:9 -> 16, 9)")]
    public int aspectWidth = 16;
    public int aspectHeight = 9;

    [Header("디버그")]
    [Tooltip("켜두면 훅 설치 과정과 리사이즈 이벤트를 Player.log에 상세히 남깁니다.")]
    public bool verboseLogging = true;

    private const int GWL_WNDPROC = -4;
    private const int WM_SIZING = 0x0214;
    
    private const int WMSZ_LEFT = 1;
    private const int WMSZ_RIGHT = 2;
    private const int WMSZ_TOP = 3;
    private const int WMSZ_TOPLEFT = 4;
    private const int WMSZ_TOPRIGHT = 5;
    private const int WMSZ_BOTTOM = 6;
    private const int WMSZ_BOTTOMLEFT = 7;
    private const int WMSZ_BOTTOMRIGHT = 8;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr_Delegate(IntPtr hWnd, int nIndex, WndProcDelegate newProc);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr_Ptr(IntPtr hWnd, int nIndex, IntPtr newProc);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CallWindowProc(IntPtr prevProc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    private IntPtr hWnd = IntPtr.Zero;
    private IntPtr prevWndProc = IntPtr.Zero;
    private WndProcDelegate newWndProcDelegate;
    private bool hookInstalled = false;
    
    private static AspectRatioWindowLock s_instance;

    void Start()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        Debug.Log($"[AspectRatioWindowLock] 초기화 시작. IntPtr.Size={IntPtr.Size} (4=32비트, 8=64비트 빌드)");
        s_instance = this;
        StartCoroutine(InstallHookWhenReady());
#else
        Debug.Log("[AspectRatioWindowLock] 에디터이거나 Windows Standalone 빌드가 아니라서 비활성화됩니다.");
#endif
    }

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
    private IEnumerator InstallHookWhenReady()
    {
        int attempts = 0;
        const int maxAttempts = 180;
        
        while (hWnd == IntPtr.Zero && attempts < maxAttempts)
        {
            hWnd = Process.GetCurrentProcess().MainWindowHandle;
            if (hWnd == IntPtr.Zero)
            {
                attempts++;
                yield return null;
            }
        }

        if (hWnd == IntPtr.Zero)
        {
            Debug.LogWarning("[AspectRatioWindowLock] MainWindowHandle 획득 실패. EnumWindows 폴백 시도.");
            hWnd = FindWindowByProcessId();
        }

        if (hWnd == IntPtr.Zero)
        {
            Debug.LogError("[AspectRatioWindowLock] 창 핸들을 끝내 가져오지 못했습니다. 훅을 설치할 수 없습니다.");
            yield break;
        }

        newWndProcDelegate = WndProcHookStatic;
        prevWndProc = SetWindowLongPtr_Delegate(hWnd, GWL_WNDPROC, newWndProcDelegate);

        if (prevWndProc == IntPtr.Zero)
        {
            int err = Marshal.GetLastWin32Error();
            Debug.LogError($"[AspectRatioWindowLock] SetWindowLongPtr 실패 (Win32 에러 코드: {err}). 훅이 걸리지 않았습니다.");
        }
        else
        {
            hookInstalled = true;
            Debug.Log($"[AspectRatioWindowLock] 훅 설치 성공. hWnd={hWnd}, 비율={aspectWidth}:{aspectHeight}");
        }
    }
    
    private static uint s_targetPid;
    private static IntPtr s_foundWindow;
    
    private IntPtr FindWindowByProcessId()
    {
        s_targetPid = (uint)Process.GetCurrentProcess().Id;
        s_foundWindow = IntPtr.Zero;

        EnumWindows(EnumWindowsCallback, IntPtr.Zero);

        return s_foundWindow;
    }

    [MonoPInvokeCallback(typeof(EnumWindowsProc))]
    private static bool EnumWindowsCallback(IntPtr wnd, IntPtr param)
    {
        GetWindowThreadProcessId(wnd, out uint pid);
        if (pid != s_targetPid) return true;

        if (!IsWindowVisible(wnd)) return true;

        StringBuilder sb = new StringBuilder(256);
        GetWindowText(wnd, sb, sb.Capacity);
        if (sb.Length == 0) return true;

        s_foundWindow = wnd;
        return false;
    }
    
    [MonoPInvokeCallback(typeof(WndProcDelegate))]
    private static IntPtr WndProcHookStatic(IntPtr hWndParam, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (s_instance == null)
        {
            return IntPtr.Zero;
        }

        return s_instance.HandleWndProc(hWndParam, msg, wParam, lParam);
    }

    private IntPtr HandleWndProc(IntPtr hWndParam, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_SIZING)
        {
            RECT rect = Marshal.PtrToStructure<RECT>(lParam);
            int side = wParam.ToInt32();

            float ratio = (float)aspectWidth / aspectHeight;
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            switch (side)
            {
                case WMSZ_LEFT:
                case WMSZ_RIGHT:
                    height = Mathf.RoundToInt(width / ratio);
                    rect.Bottom = rect.Top + height;
                    break;

                case WMSZ_TOP:
                case WMSZ_BOTTOM:
                    width = Mathf.RoundToInt(height * ratio);
                    rect.Right = rect.Left + width;
                    break;

                case WMSZ_TOPLEFT:
                case WMSZ_TOPRIGHT:
                case WMSZ_BOTTOMLEFT:
                case WMSZ_BOTTOMRIGHT:
                default:
                    height = Mathf.RoundToInt(width / ratio);
                    if (side == WMSZ_TOPLEFT || side == WMSZ_TOPRIGHT)
                        rect.Top = rect.Bottom - height;
                    else
                        rect.Bottom = rect.Top + height;
                    break;
            }

            if (verboseLogging)
            {
                Debug.Log($"[AspectRatioWindowLock] WM_SIZING side={side} -> {width}x{height}");
            }

            Marshal.StructureToPtr(rect, lParam, true);
            return new IntPtr(1);
        }

        return CallWindowProc(prevWndProc, hWndParam, msg, wParam, lParam);
    }
#endif

    void OnDestroy()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        if (hookInstalled && hWnd != IntPtr.Zero && prevWndProc != IntPtr.Zero)
        {
            SetWindowLongPtr_Ptr(hWnd, GWL_WNDPROC, prevWndProc);
            Debug.Log("[AspectRatioWindowLock] 원래 WndProc로 복구 완료.");
        }

        if (s_instance == this)
        {
            s_instance = null;
        }
#endif
    }
}
