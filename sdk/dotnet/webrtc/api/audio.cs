/*
 *  Copyright 2019 pixiv Inc. All Rights Reserved.
 *
 *  Use of this source code is governed by a license that can be
 *  found in the LICENSE.pixiv file in the root of the source tree.
 */

using Pixiv.Rtc;
using System;
using System.Net.NetworkInformation;
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

        private IThread thread;

        private const int kAdmMaxDeviceNameSize = 128;
        private const int kAdmMaxGuidSize = 128;

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int16 webrtcAudioDeviceModulePlayoutDevices(IntPtr adm);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int16 webrtcAudioDeviceModuleRecordingDevices(IntPtr adm);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern void webrtcAudioDeviceModuleInitPlayout(IntPtr adm, IntPtr rtcthread);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern void webrtcAudioDeviceModuleInitRecording(IntPtr adm, IntPtr rtcthread);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern void webrtcAudioDeviceModuleStartPlayout(IntPtr adm, IntPtr rtcthread);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern void webrtcAudioDeviceModuleStopPlayout(IntPtr adm, IntPtr rtcthread);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern void webrtcAudioDeviceModuleStartRecording(IntPtr adm, IntPtr rtcthread);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern void webrtcAudioDeviceModuleStopRecording(IntPtr adm, IntPtr rtcthread);

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

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern void webrtcAudioDeviceModuleSetSpeakerVolume(IntPtr adm, float volume);

        public AudioDeviceModule(IntPtr ptr, IThread thread)
        {
            this.Ptr = ptr;
            this.thread = thread;
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

        public void StartPlayout()
        {
            webrtcAudioDeviceModuleStartPlayout(Ptr, thread.Ptr);
        }

        public void InitPlayout()
        {
            webrtcAudioDeviceModuleInitPlayout(Ptr, thread.Ptr);
        }

        public void InitRecording()
        {
            webrtcAudioDeviceModuleInitRecording(Ptr, thread.Ptr);
        }

        public void StopPlayout()
        {
            webrtcAudioDeviceModuleStopPlayout(Ptr, thread.Ptr);
        }

        public void StartRecording()
        {
            webrtcAudioDeviceModuleStartRecording(Ptr, thread.Ptr);
        }

        public void StopRecording()
        {
            webrtcAudioDeviceModuleStopRecording(Ptr, thread.Ptr);
        }

        public void SetSpeakerVolume(float volume)
        {
            webrtcAudioDeviceModuleSetSpeakerVolume(Ptr, volume);
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
        private static extern IntPtr webrtcCreateDefaultAudioDeviceModule(IntPtr rtcthread);

        public static AudioDeviceModule CreateDefault(IThread thread)
        {
            return new AudioDeviceModule(webrtcCreateDefaultAudioDeviceModule(thread.Ptr), thread);
        }
    }
}
