/*
 *  Copyright 2020 developerinabox. All Rights Reserved.
 *
 *  Use of this source code is governed by a license that can be
 *  found in the LICENSE.pixiv file in the root of the source tree.
 */

#ifndef SDK_C_API_DATA_CHANNEL_INTERFACE_H_
#define SDK_C_API_DATA_CHANNEL_INTERFACE_H_

#include "sdk/c/interop.h"

#ifdef __cplusplus
#include "api/data_channel_interface.h"

extern "C" {
#endif
RTC_C_CLASS(webrtc::DataChannelInterface, WebrtcDataChannelInterface)
RTC_C_CLASS(webrtc::DataChannelObserver, WebrtcDataChannelObserver)

struct WebrtcDataChannelObserverFunctions {
  void (*on_destruction)(void* context);
  void (*on_state_change)(void* context);
  void (*on_message)(void* context, bool binary, const uint8_t* data, int len);
  void (*on_buffered_amount_change)(void* context, uint64_t sent_data_size);
};

RTC_EXPORT void webrtcDataChannelInterfaceRelease(
    const WebrtcDataChannelInterface* channel);

RTC_EXPORT RtcString* webrtcDataChannelLabel(
    const WebrtcDataChannelInterface* channel);

RTC_EXPORT int webrtcDataChannelStatus(
    const WebrtcDataChannelInterface* channel);

RTC_EXPORT bool webrtcDataChannelSendText(
    WebrtcDataChannelInterface* channel,
    const char* text);

RTC_EXPORT bool webrtcDataChannelSendData(
    WebrtcDataChannelInterface* channel,
    const char* data,
    size_t len);

RTC_EXPORT WebrtcDataChannelObserver* webrtcDataChannelRegisterObserverFunctions(
    WebrtcDataChannelInterface* channel,
    void* context,
    const struct WebrtcDataChannelObserverFunctions* functions);

RTC_EXPORT void webrtcDataChannelUnRegisterObserver(
    WebrtcDataChannelInterface* channel);

#ifdef __cplusplus
}
#endif

#endif
