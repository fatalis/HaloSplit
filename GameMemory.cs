using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LiveSplit.HaloSplit
{
    class GameMemory
    {
        public delegate void MapChangedEventHandler(object sender, string map);
        public event MapChangedEventHandler OnMapChanged; 
        public event EventHandler OnGainControl;
        public event EventHandler OnLostControl;
        public event EventHandler OnReset;

        private Task _thread;
        private CancellationTokenSource _cancelSource;

        private DeepPointer _currentMapPtr;
        private DeepPointer _playerPosPtr;
        private DeepPointer _playerFrozenPtr;
        private DeepPointer _difficultyPtr;

        public GameMemory()
        {
            _difficultyPtr = new DeepPointer(0x290354);
        }

        public void StartReading()
        {
            if (_thread != null && _thread.Status == TaskStatus.Running)
                throw new InvalidOperationException();

            _cancelSource = new CancellationTokenSource();
            _thread = Task.Factory.StartNew(MemoryReadThread);
        }

        public void Stop()
        {
            if (_cancelSource == null || _thread == null)
                throw new InvalidOperationException();

            if (_thread.Status != TaskStatus.Running)
                return;

            _cancelSource.Cancel();
            _thread.Wait();
        }

        Process GetGameProcess()
        {
            Process gameProcess = Process.GetProcesses()
                .FirstOrDefault(p => p.ProcessName.ToLower() == "halo" && !p.HasExited);

            if (gameProcess != null)
            {
                if (gameProcess.MainModule.FileVersionInfo.FileVersion == "01.00.00.0564")
                {
                    _currentMapPtr = new DeepPointer(0x30B4B9);
                    _playerFrozenPtr = new DeepPointer(0x46B838, 0x11);
                    _playerPosPtr = new DeepPointer(0x21A508, 0x77c);
                    return gameProcess;
                }
                else if (gameProcess.MainModule.FileVersionInfo.FileVersion == "01.00.01.0580")
                {
                    _currentMapPtr = new DeepPointer(0x30B6C1);
                    _playerFrozenPtr = new DeepPointer(0x46BA58, 0x11);
                    _playerPosPtr = new DeepPointer(0x21A278, 0x77c);
                    return gameProcess;
                }
            }

            return null;
        }

        void MemoryReadThread()
        {
            while (!_cancelSource.IsCancellationRequested)
            {
                try
                {
                    Process gameProcess;
                    while ((gameProcess = this.GetGameProcess()) == null)
                    {
                        Thread.Sleep(250);
                        if (_cancelSource.IsCancellationRequested)
                            return;
                    }

                    string prevCurrentMap = String.Empty;
                    bool prevPlayerFrozen = false;
                    while (!gameProcess.HasExited)
                    {
                        string currentMap;
                        _currentMapPtr.Deref(gameProcess, out currentMap, 255);
                        if (currentMap != prevCurrentMap)
                        {
                            if (this.OnMapChanged != null)
                                this.OnMapChanged(this, currentMap);
                        }

                        bool playerFrozen;
                        _playerFrozenPtr.Deref(gameProcess, out playerFrozen);
                        if (playerFrozen != prevPlayerFrozen)
                        {
                            if (!playerFrozen && currentMap == @"levels\a10\a10")
                            {
                                Vector3f pos;
                                _playerPosPtr.Deref(gameProcess, out pos);

                                const int DIFFICULTY_LEGENDARY = 3;
                                int difficulty;
                                _difficultyPtr.Deref(gameProcess, out difficulty);

                                // a bunch of hacks, but it works
                                var chamberPos = new Vector3f(-0.0989f, 0.436f, 0);
                                float dist = pos.DistanceXY(chamberPos);
                                if ((difficulty == DIFFICULTY_LEGENDARY && dist < 0.001)
                                    || (difficulty != DIFFICULTY_LEGENDARY && dist > 1.0f && dist < 55.4f))
                                {
                                    if (this.OnGainControl != null)
                                        this.OnGainControl(this, EventArgs.Empty);
                                }
                                else if (dist > 55.3f && dist < 55.5f)
                                {
                                    if (this.OnReset != null)
                                        this.OnReset(this, EventArgs.Empty);
                                }
                            }
                            else if (playerFrozen && currentMap == @"levels\d40\d40")
                            {
                                if (this.OnLostControl != null)
                                    this.OnLostControl(this, EventArgs.Empty);
                            }
                        }

                        prevCurrentMap = currentMap;
                        prevPlayerFrozen = playerFrozen;

                        Thread.Sleep(15);

                        if (_cancelSource.IsCancellationRequested)
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                    Thread.Sleep(1000);
                }
            }
        }
    }

    class DeepPointer
    {
        private List<int> _offsets;
        private int _base;

        public DeepPointer(int base_, params int[] offsets)
        {
            _base = base_;
            _offsets = new List<int>();
            _offsets.Add(0); // deref base first
            _offsets.AddRange(offsets);
        }

        public bool Deref<T>(Process process, out T value) where T: struct
        {
            int offset = _offsets[_offsets.Count - 1];
            IntPtr ptr;
            if (!this.DerefOffsets(process, out ptr)
                || !ReadProcessValue(process, ptr + offset, out value))
            {
                value = default(T);
                return false;
            }

            return true;
        }

        public bool Deref(Process process, out Vector3f value)
        {
            int offset = _offsets[_offsets.Count - 1];
            IntPtr ptr;
            float x, y, z;
            if (!this.DerefOffsets(process, out ptr)
                || !ReadProcessValue(process, ptr + offset + 0, out x)
                || !ReadProcessValue(process, ptr + offset + 4, out y)
                || !ReadProcessValue(process, ptr + offset + 8, out z))
            {
                value = new Vector3f();
                return false;
            }

            value = new Vector3f(x, y, z);
            return true;
        }

        public bool Deref(Process process, out string str, int max)
        {
            var sb = new StringBuilder(max);

            IntPtr ptr;
            if (!this.DerefOffsets(process, out ptr)
                || !ReadProcessASCIIString(process, ptr, sb))
            {
                str = String.Empty;
                return false;
            }

            str = sb.ToString();
            return true;
        }

        bool DerefOffsets(Process process, out IntPtr ptr)
        {
            ptr = process.MainModule.BaseAddress + _base;
            for (int i = 0; i < _offsets.Count - 1; i++)
            {
                if (!ReadProcessPtr32(process, ptr + _offsets[i], out ptr)
                    || ptr == IntPtr.Zero)
                {
                    return false;
                }
            }

            return true;
        }

        static bool ReadProcessValue<T>(Process process, IntPtr addr, out T val) where T : struct
        {
            Type type = typeof(T);

            var bytes = new byte[Marshal.SizeOf(type)];

            int read;
            val = default(T);
            if (!SafeNativeMethods.ReadProcessMemory(process.Handle, addr, bytes, bytes.Length, out read) || read != bytes.Length)
                return false;

            if (type == typeof(int))
            {
                val = (T)(object)BitConverter.ToInt32(bytes, 0);
            }
            else if (type == typeof(uint))
            {
                val = (T)(object)BitConverter.ToUInt32(bytes, 0);
            }
            else if (type == typeof(float))
            {
                val = (T)(object)BitConverter.ToSingle(bytes, 0);
            }
            else if (type == typeof(byte))
            {
                val = (T)(object)bytes[0];
            }
            else if (type == typeof(bool))
            {
                val = (T)(object)BitConverter.ToBoolean(bytes, 0);
            }
            else
            {
                throw new Exception("Type not supported.");
            }

            return true;
        }

        static bool ReadProcessPtr32(Process process, IntPtr addr, out IntPtr val)
        {
            byte[] bytes = new byte[4];
            int read;
            val = IntPtr.Zero;
            if (!SafeNativeMethods.ReadProcessMemory(process.Handle, addr, bytes, bytes.Length, out read) || read != bytes.Length)
                return false;
            val = (IntPtr)BitConverter.ToInt32(bytes, 0);
            return true;
        }

        static bool ReadProcessASCIIString(Process process, IntPtr addr, StringBuilder sb)
        {
            byte[] bytes = new byte[sb.Capacity];
            int read;
            if (!SafeNativeMethods.ReadProcessMemory(process.Handle, addr, bytes, bytes.Length, out read) || read != bytes.Length)
                return false;
            sb.Append(Encoding.ASCII.GetString(bytes));

            for (int i = 0; i < sb.Length; i++)
            {
                if (sb[i] == '\0')
                {
                    sb.Remove(i, sb.Length - i);
                    break;
                }
            }

            return true;
        }
    }

    public class Vector3f
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public int IX { get { return (int)this.X; } }
        public int IY { get { return (int)this.Y; } }
        public int IZ { get { return (int)this.Z; } }

        public Vector3f() { }

        public Vector3f(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public float Distance(Vector3f other)
        {
            float result = (this.X - other.X) * (this.X - other.X) +
                (this.Y - other.Y) * (this.Y - other.Y) +
                (this.Z - other.Z) * (this.Z - other.Z);
            return (float)Math.Sqrt(result);
        }

        public float DistanceXY(Vector3f other)
        {
            float result = (this.X - other.X) * (this.X - other.X) +
                (this.Y - other.Y) * (this.Y - other.Y);
            return (float)Math.Sqrt(result);
        }

        public override string ToString()
        {
            return this.X + " " + this.Y + " " + this.Z;
        }
    }
}
