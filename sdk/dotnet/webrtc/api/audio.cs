/*
 *  Copyright 2019 pixiv Inc. All Rights Reserved.
 *
 *  Use of this source code is governed by a license that can be
 *  found in the LICENSE.pixiv file in the root of the source tree.
 */

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Pixiv.Webrtc
{
    public interface IAudioDeviceModule
    {
        IntPtr Ptr { get; }
    }

    public interface IAudioMixer
    {
        System.IntPtr Ptr { get; }
    }

    public class AudioDeviceModule : IAudioDeviceModule
    {
        public IntPtr Ptr { get; private set; }

        private const int kAdmMaxDeviceNameSize = 128;
        private const int kAdmMaxGuidSize = 128;

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int16 webrtcAudioDeviceModulePlayoutDevices(IntPtr adm);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int16 webrtcAudioDeviceModuleRecordingDevices(IntPtr adm);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 webrtcAudioDeviceModuleSetPlayoutDevice(IntPtr adm, UInt16 index);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 webrtcAudioDeviceModuleSetRecordingDevice(IntPtr adm, UInt16 index);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 webrtcAudioDeviceModulePlayoutDeviceName(IntPtr adm, 
            UInt16 index, 
            StringBuilder name,
            StringBuilder guid);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 webrtcAudioDeviceModuleRecordingDeviceName(IntPtr adm, 
            UInt16 index,
            StringBuilder name,
            StringBuilder guid);

        public AudioDeviceModule(IntPtr ptr)
        {
            this.Ptr = ptr;
        }

        public Int32 PlayoutDevices()
        {
            return webrtcAudioDeviceModulePlayoutDevices(Ptr);
        }

        public Int32 RecordingDevices()
        {
            return webrtcAudioDeviceModuleRecordingDevices(Ptr);
        }

        public Int32 SetPlayoutDevice(UInt16 index)
        {
            return webrtcAudioDeviceModuleSetPlayoutDevice(Ptr, index);
        }

        public Int32 SetRecordingDevice(UInt16 index)
        {
            return webrtcAudioDeviceModuleSetRecordingDevice(Ptr, index);
        }

        public struct AudioDeviceName
        {
            public UInt16 index;
            public string name;
            public string guid;

            public override string ToString()
            {
                return string.Format("{0}:{1} {2}", index, name, guid);
            }
        }

        public AudioDeviceName GetPlayoutDeviceName(UInt16 index)
        {
            var name = new StringBuilder(kAdmMaxDeviceNameSize);
            var guid = new StringBuilder(kAdmMaxGuidSize);

            webrtcAudioDeviceModulePlayoutDeviceName(Ptr, index, name, guid);
            return new AudioDeviceName()
            {
                index = index,
                guid = guid.ToString(),
                name = name.ToString()
            };
        }

        public AudioDeviceName GetRecordingDeviceName(UInt16 index)
        {
            var name = new StringBuilder(kAdmMaxDeviceNameSize);
            var guid = new StringBuilder(kAdmMaxGuidSize);

            webrtcAudioDeviceModuleRecordingDeviceName(Ptr, index, name, guid);
            return new AudioDeviceName()
            {
                index = index,
                guid = guid.ToString(),
                name = name.ToString()
            };
        }

    }

    public static class AudioDeviceModuleFactory
    {
        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr webrtcCreateDefaultAudioDeviceModule();

        public static AudioDeviceModule CreateDefault()
        {
            return new AudioDeviceModule(webrtcCreateDefaultAudioDeviceModule());
        }
    }
}
