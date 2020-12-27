﻿using ModernFlyouts.Core.Display;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using static ModernFlyouts.Core.Interop.NativeMethods;

namespace ModernFlyouts.Core.Interop
{
    internal sealed class WindowsTaskbar
    {
        #region Enums

        public enum AppBarEdge : uint
        {
            Left = 0,
            Top = 1,
            Right = 2,
            Bottom = 3
        }

        public enum AppBarMessage : uint
        {
            GetState = 0x00000004,
            GetTaskbarPos = 0x00000005
        }

        [Flags]
        public enum AppBarState
        {
            ABS_AUTOHIDE = 1
        }

        public enum Position
        {
            Left = 0,
            Top = 1,
            Right = 2,
            Bottom = 3
        }

        #endregion

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public AppBarEdge uEdge;
            public RECT rect;
            public int lParam;
        }

        public struct State : IEquatable<State>
        {
            public Position Location;
            public Rect Bounds;
            public Screen ContainingScreen;
            public bool IsAutoHideEnabled;
            public bool IsRightToLeftLayout;

            public bool Equals(State other)
            {
                return Location == other.Location
                    && Bounds == other.Bounds
                    && ContainingScreen == other.ContainingScreen
                    && IsAutoHideEnabled == other.IsAutoHideEnabled
                    && IsRightToLeftLayout == other.IsRightToLeftLayout;
            }

            public override bool Equals(object obj)
            {
                return (obj is State other) && Equals(other);
            }

            public override int GetHashCode()
            {
                return Location.GetHashCode() ^ Bounds.GetHashCode()
                    ^ ContainingScreen.GetHashCode()
                    ^ IsAutoHideEnabled.GetHashCode()
                    ^ IsRightToLeftLayout.GetHashCode();
            }

            public static bool operator ==(State left, State right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(State left, State right)
            {
                return !(left == right);
            }
        }

        #endregion

        [DllImport("shell32.dll")]
        public static extern UIntPtr SHAppBarMessage(AppBarMessage dwMessage, ref APPBARDATA pData);

        public static State Current
        {
            get
            {
                var hWnd = GetHwnd();
                var state = new State
                {
                    ContainingScreen = Screen.FromHWnd(hWnd),
                };
                var appBarData = new APPBARDATA
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA)),
                    hWnd = hWnd
                };

                if (SHAppBarMessage(AppBarMessage.GetTaskbarPos, ref appBarData) != UIntPtr.Zero)
                {
                    state.Bounds = appBarData.rect.ToRect();
                    state.Location = (Position)appBarData.uEdge;
                }
                else
                {
                    GetWindowRect(hWnd, out var bounds);
                    state.Bounds = bounds.ToRect();

                    if (state.ContainingScreen != null)
                    {
                        var screen = state.ContainingScreen;
                        if (state.Bounds.Bottom == screen.Bounds.Bottom && state.Bounds.Top == screen.Bounds.Top)
                        {
                            state.Location = (state.Bounds.Left == screen.Bounds.Left) ? Position.Left : Position.Right;
                        }
                        if (state.Bounds.Right == screen.Bounds.Right && state.Bounds.Left == screen.Bounds.Left)
                        {
                            state.Location = (state.Bounds.Top == screen.Bounds.Top) ? Position.Top : Position.Bottom;
                        }
                    }
                }

                var appBarState = (AppBarState)SHAppBarMessage(AppBarMessage.GetState, ref appBarData);
                state.IsAutoHideEnabled = appBarState.HasFlag(AppBarState.ABS_AUTOHIDE);
                state.IsRightToLeftLayout = IsRightToLeftLayout(hWnd);

                return state;
            }
        }

        private static IntPtr GetHwnd() => FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);

        private static bool IsRightToLeftLayout(IntPtr hWnd)
        {
            var exStyles = (uint)GetWindowLongPtr(hWnd, (int)GetWindowLongFields.GWL_EXSTYLE);
            return (exStyles & (uint)ExtendedWindowStyles.WS_EX_LAYOUTRTL) == (uint)ExtendedWindowStyles.WS_EX_LAYOUTRTL;
        }
    }
}
