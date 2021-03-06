﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Drexel.Terminal.Sink;
using Drexel.Terminal.Sink.Win32;
using Drexel.Terminal.Source;
using Drexel.Terminal.Source.Win32;
using Microsoft.Win32.SafeHandles;

namespace Drexel.Terminal.Win32
{
    public sealed class TerminalInstance : ITerminal, IDisposable
    {
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> ActiveSemaphores =
            new ConcurrentDictionary<int, SemaphoreSlim>(1, 1);

        private readonly Action releaseCallback;
        private readonly object isDisposedLock;
        private bool isDisposed;

        private TerminalInstance(Action releaseCallback)
        {
            this.releaseCallback = releaseCallback;

            this.Source = new TerminalSource();
            this.Sink = new TerminalSink();

            this.isDisposedLock = new object();
            this.isDisposed = false;
        }

        public TerminalSource Source { get; }

        public TerminalSink Sink { get; }

        public string Title
        {
            get => Console.Title;
            set => Console.Title = value;
        }

        public ushort Height
        {
            get
            {
                checked
                {
                    return (ushort)Console.BufferHeight;
                }
            }
            set
            {
                Console.WindowHeight = value;
                Console.BufferHeight = value;
            }
        }

        public ushort Width
        {
            get
            {
                checked
                {
                    return (ushort)Console.BufferWidth;
                }
            }
            set
            {
                Console.WindowWidth = value;
                Console.BufferWidth = value;
            }
        }

        ITerminalSource IReadOnlyTerminal.Source => this.Source;

        ITerminalSink ITerminal.Sink => this.Sink;

        string IReadOnlyTerminal.Title => this.Title;

        ushort IReadOnlyTerminal.Height => this.Height;

        ushort IReadOnlyTerminal.Width => this.Width;

        public static async Task<TerminalInstance> GetInstanceAsync(CancellationToken cancellationToken)
        {
            SemaphoreSlim activeSemaphore = ActiveSemaphores.GetOrAdd(
                Process.GetCurrentProcess().Id,
                pid => new SemaphoreSlim(1, 1));
            
            await activeSemaphore.WaitAsync(cancellationToken);
            return new TerminalInstance(() => activeSemaphore.Release());
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFileHandle CreateFileW(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] uint fileAccess,
            [MarshalAs(UnmanagedType.U4)] uint fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] int flags,
            IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern SafeFileHandle GetStdHandle(int nStdHandle);

        public void Dispose()
        {
            bool shouldDispose = false;
            lock (this.isDisposedLock)
            {
                if (!this.isDisposed)
                {
                    this.isDisposed = true;
                    shouldDispose = true;
                }
            }

            if (shouldDispose)
            {
                this.Source.Dispose();
                this.Sink.Dispose();
                this.releaseCallback.Invoke();
            }
        }

        public void SetCodePage(ConsoleCodePage codePage)
        {
            this.Source.CodePage = codePage;
            this.Sink.CodePage = codePage;
        }

        public void DisableResize()
        {
            this.Sink.DisableResize();
        }
    }
}
