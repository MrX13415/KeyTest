using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KeyTest.Windows
{
    [StructLayout(LayoutKind.Sequential)]
    public class KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public KBDLLHOOKSTRUCTFlags flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [Flags]
    public enum KBDLLHOOKSTRUCTFlags : uint
    {
        LLKHF_EXTENDED = 0x01,
        LLKHF_INJECTED = 0x10,
        LLKHF_ALTDOWN = 0x20,
        LLKHF_UP = 0x80,
    }

    public class KeyboardHock
    {
        public const int WH_KEYBOARD_LL = 13;
        public const int HC_ACTION = 0;
        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;
        public const int WM_SYSKEYDOWN = 0x104;
        public const int WM_SYSKEYUP = 0x105;

        private static User32.HookProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public static EventHandler<KeyHockEventArgs> KeyEvent { get; set; }

        private static IntPtr SetHook(User32.HookProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return User32.SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    Kernel32.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int code = Marshal.ReadInt32(lParam);
                int type = (int)wParam;
                if (KeyEvent != null) KeyEvent(null, KeyHockEventArgs.FromVirtualKey(code, type));
            }

            return User32.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public static void Initialize()
        {
            _hookID = SetHook(_proc);
        }

    }

    public enum KeyHockEventType
    {
        None,
        KeyDown,
        KeyUp
    }

    public struct EventInfo
    {
        public KeyHockEventType EventType;
        public bool IsSysKey;

        public EventInfo(KeyHockEventType eventType, bool isSysKey)
        {
            EventType = eventType;
            IsSysKey = isSysKey;
        }
    }

    public class KeyHockEventArgs : EventArgs
    {
        public int VirtualKey { get; }
        public Key Key { get; }
        public KeyHockEventType EventType { get; }
        public bool IsSysKey { get; }

        public KeyHockEventArgs(int virtualKey, Key key, KeyHockEventType eventType, bool isSysKey)
        {
            VirtualKey = virtualKey;
            Key = key;
            EventType = eventType;
            IsSysKey = isSysKey;
        }

        public KeyHockEventArgs(int virtualKey, Key key, EventInfo info) : this(virtualKey, key, info.EventType, info.IsSysKey)
        {
        }

        public bool KeyDown => EventType == KeyHockEventType.KeyDown;
        public bool KeyUp => EventType == KeyHockEventType.KeyUp;
        public bool IsUnknow => EventType == KeyHockEventType.None;

        public static EventInfo GetEventInfo(int eventType)
        {
            switch (eventType)
            {
                case KeyboardHock.WM_SYSKEYDOWN:
                    return new EventInfo(KeyHockEventType.KeyDown, true);
                case KeyboardHock.WM_KEYDOWN:
                    return new EventInfo(KeyHockEventType.KeyDown, false);
                case KeyboardHock.WM_SYSKEYUP:
                    return new EventInfo(KeyHockEventType.KeyUp, true);
                case KeyboardHock.WM_KEYUP:
                    return new EventInfo(KeyHockEventType.KeyUp, false);
                default:
                    return new EventInfo(KeyHockEventType.None, false);
            }
        }

        public static KeyHockEventArgs FromVirtualKey(int virtualKey, int eventType)
            => new KeyHockEventArgs(virtualKey, KeyInterop.KeyFromVirtualKey(virtualKey), GetEventInfo(eventType));

    }
}
