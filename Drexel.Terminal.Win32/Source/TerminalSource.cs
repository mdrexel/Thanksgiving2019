﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Drexel.Terminal.Win32;
using Microsoft.Win32.SafeHandles;

namespace Drexel.Terminal.Source.Win32
{
    public sealed class TerminalSource : ITerminalSource
    {
        private const int STD_INPUT_HANDLE = -10;
        private const int INFINITE = -1;

        private readonly Box<bool> eventThreadRunning;
        private readonly Thread eventThread;
        private readonly SafeFileHandle inputHandle;
        private readonly SafeFileHandle inputStreamHandle;
        private readonly ConsoleCtrlHandlerDelegate consoleControlHandler;

        private bool mouseEnabled;
        private ConsoleMouseEventInfo lastMouseEvent;

        internal TerminalSource()
        {
            this.inputHandle = TerminalInstance.CreateFileW(
                "CONIN$",
                0x80000000,
                1,
                IntPtr.Zero,
                FileMode.Open,
                0,
                IntPtr.Zero);

            if (this.inputHandle.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            this.inputStreamHandle = TerminalInstance.GetStdHandle(STD_INPUT_HANDLE);
            if (this.inputStreamHandle.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            lastMouseEvent = default;

            this.consoleControlHandler =
                (consoleControlEventType) =>
                {
                    if (consoleControlEventType == ConsoleControlEventType.CtrlC
                        || consoleControlEventType == ConsoleControlEventType.CtrlBreak)
                    {
                        ExitRequestedEventArgs args = new ExitRequestedEventArgs();
                        this.OnExitRequested?.Invoke(this, args);
                        if (args.Allow)
                        {
                            this.OnExitAccepted?.Invoke(this, new ExitAcceptedEventArgs());
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                };

            SetConsoleCtrlHandler(
                this.consoleControlHandler,
                true);

            this.OnLeftMouse += (obj, e) => this.LeftMouseDown = e.ButtonDown;
            this.OnRightMouse += (obj, e) => this.RightMouseDown = e.ButtonDown;

            this.eventThreadRunning = new Box<bool>(true);
            this.eventThread =
                new Thread(
                    () =>
                    {
                        bool GetRunningState()
                        {
                            lock (this.eventThreadRunning)
                            {
                                return this.eventThreadRunning.Value;
                            }
                        }

                        while (GetRunningState())
                        {
                            try
                            {
                                while (this.ListenForEvents(out ConsoleInputEventInfo[] events))
                                {
                                    foreach (ConsoleInputEventInfo @event in events)
                                    {
                                        try
                                        {
                                            this.DispatchEventProcessing(@event);
                                        }
                                        catch
                                        {
                                            // Processing an event shouldn't emit an exception, but just in case...
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // This shouldn't ever happen, but just in case, re-listen for events
                            }
                        }
                    });
            this.eventThread.Start();
        }

        public bool MouseEnabled
        {
            get => this.mouseEnabled;
            set
            {
                if (this.mouseEnabled == value)
                {
                    return;
                }

                this.mouseEnabled = value;

                // Toggle quick-edit mode (the ability to highlight regions of the console with the mouse)
                GetConsoleMode(this.inputStreamHandle.DangerousGetHandle(), out uint consoleMode);
                SetConsoleMode(
                    this.inputStreamHandle.DangerousGetHandle(),
                    (uint)(((ConsoleInputModes)consoleMode) ^ ConsoleInputModes.ENABLE_QUICK_EDIT_MODE));
            }
        }

        public ConsoleCodePage CodePage
        {
            get => (ConsoleCodePage)GetConsoleCP();
            set => SetConsoleCP((uint)value);
        }

        public bool LeftMouseDown { get; private set; }

        public bool RightMouseDown { get; private set; }

        public event EventHandler<ExitRequestedEventArgs>? OnExitRequested;

        public event EventHandler<MouseClickEventArgs>? OnLeftMouse;

        public event EventHandler<MouseClickEventArgs>? OnRightMouse;

        public event EventHandler<MouseMoveEventArgs>? OnMouseMove;

        public event EventHandler<MouseWheelEventArgs>? OnMouseWheel;

        public event EventHandler<TerminalKeyInfo>? OnKeyPressed;

        public event EventHandler<TerminalKeyInfo>? OnKeyReleased;

        public event EventHandler<ExitAcceptedEventArgs>? OnExitAccepted;

        private delegate bool ConsoleCtrlHandlerDelegate(ConsoleControlEventType CtrlType);

        public async Task DelayUntilExitAccepted(CancellationToken cancellationToken)
        {
            CancellationTokenSource cts;
            lock (this.eventThreadRunning)
            {
                if (!this.eventThreadRunning.Value)
                {
                    return;
                }
                else
                {
                    cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    this.OnExitAccepted +=
                        (obj, e) =>
                        {
                            cts.Cancel();
                        };
                }
            }

            try
            {
                await Task.Delay(-1, cts.Token);
            }
            catch (TaskCanceledException)
            {
                // An exception being thrown is expected.
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetConsoleCP();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCP(uint wCodePageID);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(
            ConsoleCtrlHandlerDelegate HandlerRoutine,
            bool Add);

        [DllImport("kernel32.dll")]
        private static extern uint WaitForSingleObject(
            IntPtr hHandle,
            int dwMilliseconds);

        [DllImport("kernel32.dll")]
        private static extern uint WaitForSingleObject(
            SafeFileHandle hHandle,
            int dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CancelIoEx(SafeFileHandle handle, IntPtr lpOverlapped);

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(
            IntPtr hConsoleHandle,
            out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(
            IntPtr hConsoleHandle,
            uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadConsoleInput(
            SafeFileHandle hConsoleInput,
            [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ConsoleInputEventInfo[] lpBuffer,
            int nLength,
            out int lpNumberOfEventsRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetNumberOfConsoleInputEvents(
            SafeFileHandle hConsoleInput,
            out int lpcNumberOfEvents);

        internal void Dispose()
        {
            lock (this.eventThreadRunning)
            {
                this.eventThreadRunning.Value = false;
            }

            SetConsoleCtrlHandler(
                this.consoleControlHandler,
                false);

            CancelIoEx(this.inputHandle, IntPtr.Zero);

            this.inputHandle.Dispose();
            this.inputStreamHandle.Dispose();

            this.eventThread.Join();
        }

        private bool ListenForEvents(out ConsoleInputEventInfo[] events)
        {
            bool @continue = true;
            int unreadEventCount;
            try
            {
                // Wait until the console notifies us that at least one event has been received
                WaitForSingleObject(this.inputHandle, INFINITE);

                // Find out the number of console events waiting for us
                GetNumberOfConsoleInputEvents(this.inputHandle, out unreadEventCount);
            }
            catch (ObjectDisposedException)
            {
                // This gets thrown if we were listening for events when the terminal was disposed. This is fine,
                // because the event listener thread will exit after processing the last batch of events.
                @continue = false;
                unreadEventCount = 0;
            }

            if (unreadEventCount == 0)
            {
                events = Array.Empty<ConsoleInputEventInfo>();
            }
            else
            {
                // Allocate an array to read events into, and then read them
                events = new ConsoleInputEventInfo[unreadEventCount];
                ReadConsoleInput(
                    this.inputHandle,
                    events,
                    unreadEventCount,
                    out _);
            }

            return @continue;
        }

        private void DispatchEventProcessing(ConsoleInputEventInfo @event)
        {
            switch (@event.EventType)
            {
                case ConsoleInputEventType.None:
                    // This shouldn't ever happen, but just in case...
                    break;
                case ConsoleInputEventType.FocusEvent:
                    break;
                case ConsoleInputEventType.KeyEvent:
                    this.ProcessKeyEvent(@event.KeyEvent);
                    break;
                case ConsoleInputEventType.MenuEvent:
                    break;
                case ConsoleInputEventType.MouseEvent:
                    if (this.mouseEnabled)
                    {
                        this.ProcessMouseEvent(@event.MouseEvent);
                    }

                    break;
                case ConsoleInputEventType.WindowBufferSizeEvent:
                    break;
                default:
                    throw new NotImplementedException(
                        "Unrecognized console input event type.");
            }
        }

        private void ProcessKeyEvent(ConsoleKeyEventInfo keyEvent)
        {
            TerminalKeyInfo keyInfo = new TerminalKeyInfo(
                keyEvent.UnicodeChar,
                (TerminalKey)keyEvent.VirtualKeyCode,
                keyEvent.ControlKeyState.HasFlag(ConsoleControlKeyState.ShiftPressed),
                keyEvent.ControlKeyState.HasFlag(ConsoleControlKeyState.LeftAltPressed)
                    || keyEvent.ControlKeyState.HasFlag(ConsoleControlKeyState.RightAltPressed),
                keyEvent.ControlKeyState.HasFlag(ConsoleControlKeyState.LeftCtrlPressed)
                    || keyEvent.ControlKeyState.HasFlag(ConsoleControlKeyState.RightCtrlPressed));
            if (keyEvent.KeyDown)
            {
                this.OnKeyPressed?.Invoke(this, keyInfo);
            }
            else
            {
                this.OnKeyReleased?.Invoke(this, keyInfo);
            }
        }

        private void ProcessMouseEvent(ConsoleMouseEventInfo mouseEvent)
        {
            if (mouseEvent.MousePosition != this.lastMouseEvent.MousePosition)
            {
                MouseMoveEventArgs args =
                    new MouseMoveEventArgs(
                        this.lastMouseEvent.MousePosition,
                        mouseEvent.MousePosition);
                this.OnMouseMove?.Invoke(this, args);
            }

            ConsoleMouseButtonState delta = mouseEvent.ButtonState ^ lastMouseEvent.ButtonState;

            if (delta.HasFlag(ConsoleMouseButtonState.FromLeft1stButtonPressed))
            {
                this.OnLeftMouse?.Invoke(
                    this,
                    new MouseClickEventArgs(
                        mouseEvent.MousePosition,
                        mouseEvent.ButtonState.HasFlag(ConsoleMouseButtonState.FromLeft1stButtonPressed)));
            }

            if (delta.HasFlag(ConsoleMouseButtonState.RightMostButtonPressed))
            {
                this.OnRightMouse?.Invoke(
                    this,
                    new MouseClickEventArgs(
                        mouseEvent.MousePosition,
                        mouseEvent.ButtonState.HasFlag(ConsoleMouseButtonState.RightMostButtonPressed)));
            }

            if (mouseEvent.EventFlags.HasFlag(ConsoleMouseEventType.MouseWheeled))
            {
                this.OnMouseWheel?.Invoke(
                    this,
                    new MouseWheelEventArgs(
                        mouseEvent.MousePosition,
                        mouseEvent.ButtonState.HasFlag(ConsoleMouseButtonState.ScrollDown)
                            ? MouseWheelDirection.Down
                            : MouseWheelDirection.Up));
            }

            this.lastMouseEvent = mouseEvent;
        }

        private sealed class Box<T>
        {
            public Box(T value)
            {
                this.Value = value;
            }

            public T Value { get; set; }
        }
    }
}