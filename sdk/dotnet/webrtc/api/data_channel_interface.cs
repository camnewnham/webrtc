/*
 *  Copyright 2019 pixiv Inc. All Rights Reserved.
 *
 *  Use of this source code is governed by a license that can be
 *  found in the LICENSE.pixiv file in the root of the source tree.
 */
using Pixiv.Rtc;
using System;
using System.Runtime.InteropServices;

namespace Pixiv.Webrtc
{
    public interface IDataChannelInterface
    {
        IntPtr Ptr { get; }
    }

    public interface IDisposableDataChannelInterface :
        IDataChannelInterface, Rtc.IDisposable
    {
    }

    public interface IDataChannelObserver
    {
        void OnStateChange();
        void OnMessage(bool binary, IntPtr data, int size);
        void OnBufferedAmountChange(UInt64 amount);
        void OnObserverDestroyed();
    }

    public sealed class DisposableDataChannelObserver
    {
        private delegate void OnDestructionDelegate(IntPtr context);
        private delegate void OnStateChangeDelegate(IntPtr context);
        private delegate void OnMessageDelegate(IntPtr context, bool binary, IntPtr data, int length);
        private delegate void OnBufferedAmountChangeDelegate(IntPtr context, UInt64 sent_data_size);

        private static FunctionPtrArray functions = new FunctionPtrArray(
            (OnDestructionDelegate)ObserverDestroyed,
            (OnStateChangeDelegate)StateChange,
            (OnMessageDelegate)Message,
            (OnBufferedAmountChangeDelegate)BufferedAmountChange);

        public static IntPtr FunctionsPtr { get { return functions.Ptr; } }

		[MonoPInvokeCallback(typeof(OnDestructionDelegate))]
        private static void ObserverDestroyed(IntPtr context)
        {
            (((GCHandle)context).Target as IDataChannelObserver).OnObserverDestroyed();
        }
		[MonoPInvokeCallback(typeof(OnStateChangeDelegate))]
        private static void StateChange(IntPtr context)
        {
            (((GCHandle)context).Target as IDataChannelObserver).OnStateChange();
        }
		[MonoPInvokeCallback(typeof(OnMessageDelegate))]
        private static void Message(IntPtr context, bool binary, IntPtr data, int length)
        {
            (((GCHandle)context).Target as IDataChannelObserver).OnMessage(binary, data, length);
        }
		[MonoPInvokeCallback(typeof(OnBufferedAmountChangeDelegate))]
        private static void BufferedAmountChange(IntPtr context, UInt64 sent_data_size)
        {
            (((GCHandle)context).Target as IDataChannelObserver).OnBufferedAmountChange(sent_data_size);
        }
    }
   
    public sealed class DisposableDataChannelInterface :
        DisposablePtr, IDisposableDataChannelInterface
    {
        IntPtr IDataChannelInterface.Ptr => Ptr;
        
        internal DisposableDataChannelInterface(IntPtr ptr)
        {
            Ptr = ptr;
        }

        private protected override void FreePtr()
        {
            Interop.DataChannelInterface.Release(Ptr);
        }       
        
        public IntPtr GetPtr
        {
            get
            {
                return Ptr;
            }
        }
    }

    public static class DataChannelInterfaceExtension
    {
        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr webrtcDataChannelRegisterObserverFunctions(IntPtr channel, IntPtr context, IntPtr functionsArray);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern void webrtcDataChannelUnRegisterObserver(IntPtr channel);

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr webrtcDataChannelLabel(
            IntPtr ptr
        );

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern int webrtcDataChannelStatus(
           IntPtr ptr
       );

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool webrtcDataChannelSendText(
           IntPtr ptr, string text 
       );

        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool webrtcDataChannelSendData(
           IntPtr ptr, IntPtr data, int len
       );

        public static string Label(this IDisposableDataChannelInterface channel)
        {
            return Rtc.Interop.String.MoveToString(
                webrtcDataChannelLabel(channel.Ptr));
        }

        public static int Status(this IDisposableDataChannelInterface channel)
        {
            return webrtcDataChannelStatus(channel.Ptr);
        }

        public static bool Send(this IDisposableDataChannelInterface channel, byte[] data, int offset, int length)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                return webrtcDataChannelSendData(channel.Ptr, IntPtr.Add(handle.AddrOfPinnedObject(), offset), length);
            }
            finally
            {
                handle.Free();
            }
        }

        public static bool Send(this IDisposableDataChannelInterface channel, string text)
        {
            return webrtcDataChannelSendText(channel.Ptr, text);
        }

        public static void UnRegisterObserver(this IDisposableDataChannelInterface channel)
        {
            webrtcDataChannelUnRegisterObserver(channel.Ptr);
        }

        public static IntPtr RegisterObserver(this IDisposableDataChannelInterface channel, IDataChannelObserver observer)
        {
            return webrtcDataChannelRegisterObserverFunctions(channel.Ptr, (IntPtr)GCHandle.Alloc(observer), DisposableDataChannelObserver.FunctionsPtr);
        }
    }
}

namespace Pixiv.Webrtc.Interop
{
    public static class DataChannelInterface
    {
        [DllImport(Dll.Name, CallingConvention = CallingConvention.Cdecl, EntryPoint = "webrtcDataChannelInterfaceRelease")]
        public static extern void Release(IntPtr ptr);
    }
}
