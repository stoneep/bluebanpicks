using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// 빌드된 Windows 스탠드얼론 창을 마우스로 리사이즈할 때
/// 지정한 가로:세로 비율을 유지하도록 강제하는 컴포넌트.
///
/// - 에디터 Play 모드에서는 동작하지 않으며, 실제 빌드된 exe(Windows Standalone)에서만 동작합니다.
/// - 씬에 빈 GameObject를 만들어 이 스크립트를 붙이고, DontDestroyOnLoad로 유지하는 것을 권장합니다.
/// </summary>
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

    // WM_SIZING의 wParam으로 오는 드래그 방향
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

    // 새 WndProc(델리게이트)을 심을 때 사용하는 오버로드
    [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr_Delegate(IntPtr hWnd, int nIndex, WndProcDelegate newProc);

    // 기존 WndProc(IntPtr)를 복구할 때 사용하는 오버로드
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
    private WndProcDelegate newWndProcDelegate; // GC 방지를 위해 반드시 필드로 보관
    private bool hookInstalled = false;

    // IL2CPP은 인스턴스 메서드를 가리키는 델리게이트를 네이티브로 마샬링하지 못하므로,
    // 콜백은 반드시 static이어야 하고, 인스턴스 상태는 이 static 참조를 통해 접근한다.
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
        const int maxAttempts = 180; // 약 3초 (60fps 기준)

        // 1차: Process.MainWindowHandle로 시도 (가장 흔한 경우)
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

    // EnumWindows 콜백에서 결과를 주고받기 위한 static 상태.
    // 클로저(람다) 캡처는 IL2CPP에서 네이티브로 마샬링이 불안정할 수 있어
    // static 필드 + static 콜백 방식으로 처리한다.
    private static uint s_targetPid;
    private static IntPtr s_foundWindow;

    /// <summary>
    /// MainWindowHandle이 0을 반환하는 드문 경우를 위한 폴백.
    /// 현재 프로세스 ID를 가진 보이는(Visible) 창 중 제목이 있는 첫 창을 찾는다.
    /// </summary>
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
        if (pid != s_targetPid) return true; // 계속 탐색

        if (!IsWindowVisible(wnd)) return true;

        StringBuilder sb = new StringBuilder(256);
        GetWindowText(wnd, sb, sb.Capacity);
        if (sb.Length == 0) return true; // 제목 없는 창은 스킵 (보통 숨겨진 헬퍼 창)

        s_foundWindow = wnd;
        return false; // 찾았으니 중단
    }

    // IL2CPP은 인스턴스 메서드를 네이티브 함수 포인터로 마샬링하지 못하므로
    // 반드시 static이어야 한다. [MonoPInvokeCallback]은 AOT 컴파일 시
    // 이 메서드가 네이티브 콜백 진입점임을 명시해 트리밍/최적화에서 누락되지 않게 한다.
    [MonoPInvokeCallback(typeof(WndProcDelegate))]
    private static IntPtr WndProcHookStatic(IntPtr hWndParam, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (s_instance == null)
        {
            // 인스턴스가 없으면(예: 파괴된 이후 콜백이 들어온 예외적 상황) 아무 처리도 하지 않고 넘긴다.
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
                    // 좌우로만 드래그 중 -> 높이를 비율에 맞게 조정
                    height = Mathf.RoundToInt(width / ratio);
                    rect.Bottom = rect.Top + height;
                    break;

                case WMSZ_TOP:
                case WMSZ_BOTTOM:
                    // 상하로만 드래그 중 -> 너비를 비율에 맞게 조정
                    width = Mathf.RoundToInt(height * ratio);
                    rect.Right = rect.Left + width;
                    break;

                case WMSZ_TOPLEFT:
                case WMSZ_TOPRIGHT:
                case WMSZ_BOTTOMLEFT:
                case WMSZ_BOTTOMRIGHT:
                default:
                    // 모서리 드래그 -> 너비 기준으로 높이 맞춤
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
            return new IntPtr(1); // TRUE 반환 -> 우리가 rect를 수정했음을 알림
        }

        return CallWindowProc(prevWndProc, hWndParam, msg, wParam, lParam);
    }
#endif

    void OnDestroy()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        if (hookInstalled && hWnd != IntPtr.Zero && prevWndProc != IntPtr.Zero)
        {
            // 델리게이트가 아닌 IntPtr 오버로드로 원래 WndProc 복구
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
