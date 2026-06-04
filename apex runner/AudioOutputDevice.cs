using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace apex_runner
{
    internal class AudioOutputDevice
    {
        public string Id { get; private set; }
        public string Name { get; private set; }

        public AudioOutputDevice(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public static List<AudioOutputDevice> GetActiveRenderDevices()
        {
            List<AudioOutputDevice> devices = new List<AudioOutputDevice>();
            IntPtr enumerator = IntPtr.Zero;
            IntPtr collection = IntPtr.Zero;

            try
            {
                enumerator = CreateComInstance(ComIds.MMDeviceEnumerator, ComIds.IMMDeviceEnumerator);
                Marshal.ThrowExceptionForHR(GetDelegate<EnumAudioEndpointsDelegate>(enumerator, 3)(enumerator, 0, 1, out collection));

                uint count;
                Marshal.ThrowExceptionForHR(GetDelegate<GetCountDelegate>(collection, 3)(collection, out count));

                for (uint i = 0; i < count; i++)
                {
                    IntPtr device = IntPtr.Zero;
                    IntPtr propertyStore = IntPtr.Zero;

                    try
                    {
                        Marshal.ThrowExceptionForHR(GetDelegate<ItemDelegate>(collection, 4)(collection, i, out device));

                        string id = GetDeviceId(device);
                        propertyStore = OpenPropertyStore(device);
                        string name = GetFriendlyName(propertyStore);

                        devices.Add(new AudioOutputDevice(id, name));
                    }
                    finally
                    {
                        ReleaseIfNeeded(propertyStore);
                        ReleaseIfNeeded(device);
                    }
                }
            }
            finally
            {
                ReleaseIfNeeded(collection);
                ReleaseIfNeeded(enumerator);
            }

            return devices;
        }

        public static void SetDefault(string deviceId)
        {
            IntPtr policyConfig = IntPtr.Zero;

            try
            {
                policyConfig = CreateComInstance(ComIds.PolicyConfigClient, ComIds.IPolicyConfig);
                SetDefaultEndpointDelegate setDefaultEndpoint = GetDelegate<SetDefaultEndpointDelegate>(policyConfig, 13);

                Marshal.ThrowExceptionForHR(setDefaultEndpoint(policyConfig, deviceId, 0));
                Marshal.ThrowExceptionForHR(setDefaultEndpoint(policyConfig, deviceId, 1));
                Marshal.ThrowExceptionForHR(setDefaultEndpoint(policyConfig, deviceId, 2));
            }
            finally
            {
                ReleaseIfNeeded(policyConfig);
            }
        }

        private static string GetDeviceId(IntPtr device)
        {
            IntPtr idPtr = IntPtr.Zero;

            try
            {
                Marshal.ThrowExceptionForHR(GetDelegate<GetIdDelegate>(device, 5)(device, out idPtr));
                return Marshal.PtrToStringUni(idPtr);
            }
            finally
            {
                if (idPtr != IntPtr.Zero)
                {
                    CoTaskMemFree(idPtr);
                }
            }
        }

        private static IntPtr OpenPropertyStore(IntPtr device)
        {
            IntPtr propertyStore;
            Marshal.ThrowExceptionForHR(GetDelegate<OpenPropertyStoreDelegate>(device, 4)(device, 0, out propertyStore));
            return propertyStore;
        }

        private static string GetFriendlyName(IntPtr propertyStore)
        {
            PropertyKey friendlyNameKey = PropertyKeys.DeviceFriendlyName;
            PropVariant friendlyName;
            Marshal.ThrowExceptionForHR(GetDelegate<GetValueDelegate>(propertyStore, 5)(propertyStore, ref friendlyNameKey, out friendlyName));

            try
            {
                return friendlyName.GetString();
            }
            finally
            {
                PropVariantClear(ref friendlyName);
            }
        }

        private static IntPtr CreateComInstance(Guid clsid, Guid iid)
        {
            IntPtr instance;
            Marshal.ThrowExceptionForHR(CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out instance));
            return instance;
        }

        private static T GetDelegate<T>(IntPtr comObject, int methodIndex) where T : class
        {
            IntPtr vtable = Marshal.ReadIntPtr(comObject);
            IntPtr method = Marshal.ReadIntPtr(vtable, methodIndex * IntPtr.Size);
            return Marshal.GetDelegateForFunctionPointer(method, typeof(T)) as T;
        }

        private static void ReleaseIfNeeded(IntPtr comObject)
        {
            if (comObject != IntPtr.Zero)
            {
                Marshal.Release(comObject);
            }
        }

        [DllImport("ole32.dll")]
        private static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint context, ref Guid iid, out IntPtr instance);

        [DllImport("ole32.dll")]
        private static extern void CoTaskMemFree(IntPtr ptr);

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant pvar);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int EnumAudioEndpointsDelegate(IntPtr self, int dataFlow, int stateMask, out IntPtr devices);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetCountDelegate(IntPtr self, out uint count);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int ItemDelegate(IntPtr self, uint index, out IntPtr device);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int OpenPropertyStoreDelegate(IntPtr self, int accessMode, out IntPtr propertyStore);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetIdDelegate(IntPtr self, out IntPtr id);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetValueDelegate(IntPtr self, ref PropertyKey key, out PropVariant value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetDefaultEndpointDelegate(
            IntPtr self,
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            int role);

        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyKey
        {
            public Guid fmtid;
            public uint pid;

            public PropertyKey(Guid fmtid, uint pid)
            {
                this.fmtid = fmtid;
                this.pid = pid;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PropVariant
        {
            public ushort vt;
            public ushort wReserved1;
            public ushort wReserved2;
            public ushort wReserved3;
            public IntPtr p;
            public int p2;

            public string GetString()
            {
                const ushort VT_LPWSTR = 31;
                if (vt == VT_LPWSTR && p != IntPtr.Zero)
                {
                    return Marshal.PtrToStringUni(p);
                }

                return string.Empty;
            }
        }

        private static class PropertyKeys
        {
            public static PropertyKey DeviceFriendlyName = new PropertyKey(
                new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
                14);
        }

        private static class ComIds
        {
            public static readonly Guid MMDeviceEnumerator = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
            public static readonly Guid IMMDeviceEnumerator = new Guid("A95664D2-9614-4F35-A746-DE8DB63617E6");
            public static readonly Guid PolicyConfigClient = new Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");
            public static readonly Guid IPolicyConfig = new Guid("F8679F50-850A-41CF-9C72-430F290290C8");
        }
    }
}
