/*
 *  Copyright 2019 pixiv Inc. All Rights Reserved.
 *
 *  Use of this source code is governed by a license that can be
 *  found in the LICENSE.pixiv file in the root of the source tree.
 */

#ifndef SDK_C_MODULES_AUDIO_DEVICE_H_
#define SDK_C_MODULES_AUDIO_DEVICE_H_

#include "sdk/c/interop.h"

#ifdef __cplusplus
#include "modules/audio_device/include/audio_device.h"
#endif

RTC_C_CLASS(webrtc::AudioDeviceModule, WebrtcAudioDeviceModule)

extern "C" {

RTC_EXPORT WebrtcAudioDeviceModule* webrtcCreateDefaultAudioDeviceModule();

RTC_EXPORT uint16_t
webrtcAudioDeviceModulePlayoutDevices(WebrtcAudioDeviceModule* adm);

RTC_EXPORT uint16_t
webrtcAudioDeviceModuleRecordingDevices(WebrtcAudioDeviceModule* adm);

RTC_EXPORT int32_t
webrtcAudioDeviceModuleSetPlayoutDevice(WebrtcAudioDeviceModule* adm,
                                        uint16_t index);

RTC_EXPORT int32_t
webrtcAudioDeviceModuleSetRecordingDevice(WebrtcAudioDeviceModule* adm,
                                          uint16_t index);

RTC_EXPORT int32_t
webrtcAudioDeviceModulePlayoutDeviceName(WebrtcAudioDeviceModule* adm,
                                         uint16_t index,
                                         char* name,
                                         char* guid);
RTC_EXPORT int32_t
webrtcAudioDeviceModuleRecordingDeviceName(WebrtcAudioDeviceModule* adm,
                                           uint16_t index,
                                           char* name,
                                           char* guid);
}
#endif