/*
 *  Copyright 2019 pixiv Inc. All Rights Reserved.
 *
 *  Use of this source code is governed by a license that can be
 *  found in the LICENSE.pixiv file in the root of the source tree.
 */

#include "api/media_stream_interface.h"
#include "rtc_base/ref_counted_object.h"
#include "sdk/c/api/media_stream_interface.h"

#include <atomic>

namespace webrtc {

class DelegatingAudioSourceInterface : public AudioSourceInterface {
 public:
  DelegatingAudioSourceInterface(
      void* context,
      const struct AudioSourceInterfaceFunctions* functions) {
    context_ = context;
    functions_ = functions;
  }

  ~DelegatingAudioSourceInterface() { functions_->on_destruction(context_); }

  void RegisterObserver(ObserverInterface* observer) {
    functions_->register_observer(context_, rtc::ToC(observer));
  }

  void UnregisterObserver(ObserverInterface* observer) {
    functions_->unregister_observer(context_, rtc::ToC(observer));
  }

  void AddSink(AudioTrackSinkInterface* sink) {
    functions_->add_sink(context_, rtc::ToC(sink));
  }

  void RemoveSink(AudioTrackSinkInterface* sink) {
    functions_->remove_sink(context_, rtc::ToC(sink));
  }

  SourceState state() const {
    return static_cast<SourceState>(functions_->state(context_));
  }

  bool remote() const { return functions_->remote(context_); }

 private:
  void* context_;
  const struct AudioSourceInterfaceFunctions* functions_;
};

class RingBufferAudioTrackSink : public AudioTrackSinkInterface {
 public:
  RingBufferAudioTrackSink(void* context, int size) {
    length = size;
    buffer = new uint8_t[size];
    read.store(0);
    write.store(0);
    watermark.store(length);
    dropped.store(0);
  }

  ~RingBufferAudioTrackSink() override { delete buffer; }

 private:
  size_t length;
  uint8_t* buffer;

  // signed so we can perform a single atomic subtraction and determine both
  // magnitude and order.
  std::atomic<int64_t> read;
  std::atomic<int64_t> write;
  std::atomic<int64_t> watermark;

  std::atomic<size_t> dropped;

  std::atomic<int> bits_per_sample;
  std::atomic<int> sample_rate;
  std::atomic<size_t> number_of_channels;

  struct AudioInfo {
    int bits_per_sample;
    int number_of_channels;
    int sample_rate;
  } audioInfo;

  void OnData(const void* audio_data,
              int bits_per_sample,
              int sample_rate,
              size_t number_of_channels,
              size_t number_of_frames) override {
    // check if the audio configuration has changed, and if so reset the buffer.

    bool needsreset = false;

    if (this->bits_per_sample != bits_per_sample) {
      this->bits_per_sample = bits_per_sample;
      needsreset = true;
    }

    if (this->number_of_channels != number_of_channels) {
      this->number_of_channels = number_of_channels;
      needsreset = true;
    }

    if (this->sample_rate != sample_rate) {
      this->sample_rate = sample_rate;
      needsreset = true;
    }

    if (needsreset) {
      read.store(0);
      write.store(0);
      watermark.store(length);
    }

    size_t sample_size = bits_per_sample / 8;
    size_t number_of_samples = number_of_frames * number_of_channels;
    int64_t write_len = number_of_samples * sample_size;

    int64_t remaining = length - write.load();
    if (write_len <= remaining) {  // we can write into the end of the buffer...

      //... provided read is keeping up
      int64_t capacity = write.load() - read.load();
      if (capacity < 0) {  // write follows read
        if (abs(capacity) < write_len) {
          dropped++;
          return;  // there is no space in the buffer
        }
      }

      memcpy(buffer + write.load(), audio_data, (size_t)write_len);
      write.fetch_add(write_len);      

    } else {  // no space at the end so wrap around

      watermark.store(write.load());

      if (read.load() > write_len) {
        memcpy(buffer, audio_data, write_len);
        write.store(write_len);
      } else {
        dropped++;
      }
    }
  }

 public:
  int available() {
    int64_t available = write.load() - read.load();
    if (available >= 0) {
      return available;
    } else {
      return (watermark.load() - read.load()) + write.load();
    }
  }

  const void* data() { return buffer + read.load(); }

  const void* info() {
    this->audioInfo.bits_per_sample = this->bits_per_sample;
    this->audioInfo.number_of_channels = this->number_of_channels;
    this->audioInfo.sample_rate = this->sample_rate;
    return &audioInfo;
  }

  void advance(int amount) {
    read.fetch_add(amount);
    if (read.load() >= watermark.load()) {
      read.store(0);
    }
  }
};

}  // namespace webrtc

extern "C" WebrtcMediaSourceInterface*
webrtcAudioSourceInterfaceToWebrtcMediaSourceInterface(
    WebrtcAudioSourceInterface* source) {
  return rtc::ToC(
      static_cast<webrtc::MediaSourceInterface*>(rtc::ToCplusplus(source)));
}

extern "C" void webrtcAudioTrackInterfaceAddSink(
    WebrtcAudioTrackInterface* track,
    WebrtcAudioTrackSinkInterface* sink) {
  rtc::ToCplusplus(track)->AddSink(rtc::ToCplusplus(sink));
}

extern "C" WebrtcMediaStreamTrackInterface*
webrtcAudioTrackInterfaceToWebrtcMediaStreamTrackInterface(
    WebrtcAudioTrackInterface* track) {
  return rtc::ToC(
      static_cast<webrtc::MediaStreamTrackInterface*>(rtc::ToCplusplus(track)));
}

extern "C" void webrtcAudioTrackSinkInterfaceOnData(
    WebrtcAudioTrackSinkInterface* sink,
    const void* audio_data,
    int bits_per_sample,
    int sample_rate,
    size_t number_of_channels,
    size_t number_of_frames) {
  rtc::ToCplusplus(sink)->OnData(audio_data, bits_per_sample, sample_rate,
                                 number_of_channels, number_of_frames);
}

extern "C" void webrtcMediaSourceInterfaceRelease(
    const WebrtcMediaSourceInterface* source) {
  rtc::ToCplusplus(source)->Release();
}

extern "C" WebrtcVideoTrackSourceInterface*
webrtcMediaSourceInterfaceToWebrtcVideoTrackSourceInterface(
    WebrtcMediaSourceInterface* source) {
  return rtc::ToC(static_cast<webrtc::VideoTrackSourceInterface*>(
      rtc::ToCplusplus(source)));
}

extern "C" void webrtcMediaStreamInterfaceRelease(
    const WebrtcMediaStreamInterface* stream) {
  rtc::ToCplusplus(stream)->Release();
}

extern "C" RtcString* webrtcMediaStreamTrackInterfaceId(
    const WebrtcMediaStreamTrackInterface* track) {
  return rtc::ToC(new auto(rtc::ToCplusplus(track)->id()));
}

extern "C" void webrtcMediaStreamTrackInterfaceRelease(
    const WebrtcMediaStreamTrackInterface* track) {
  rtc::ToCplusplus(track)->Release();
}

extern "C" const char* webrtcMediaStreamTrackInterfaceKAudioKind() {
  return webrtc::MediaStreamTrackInterface::kAudioKind;
}

extern "C" const char* webrtcMediaStreamTrackInterfaceKind(
    const WebrtcMediaStreamTrackInterface* track) {
  auto kind = rtc::ToCplusplus(track)->kind();

  if (kind == webrtc::MediaStreamTrackInterface::kAudioKind) {
    return webrtc::MediaStreamTrackInterface::kAudioKind;
  }

  if (kind == webrtc::MediaStreamTrackInterface::kVideoKind) {
    return webrtc::MediaStreamTrackInterface::kVideoKind;
  }

  RTC_NOTREACHED();

  return nullptr;
}

extern "C" const char* webrtcMediaStreamTrackInterfaceKVideoKind() {
  return webrtc::MediaStreamTrackInterface::kVideoKind;
}

extern "C" WebrtcAudioTrackInterface*
webrtcMediaStreamTrackInterfaceToWebrtcAudioTrackInterface(
    WebrtcMediaStreamTrackInterface* track) {
  return rtc::ToC(
      static_cast<webrtc::AudioTrackInterface*>(rtc::ToCplusplus(track)));
}

extern "C" WebrtcVideoTrackInterface*
webrtcMediaStreamTrackInterfaceToWebrtcVideoTrackInterface(
    WebrtcMediaStreamTrackInterface* track) {
  return rtc::ToC(
      static_cast<webrtc::VideoTrackInterface*>(rtc::ToCplusplus(track)));
}

extern "C" WebrtcAudioSourceInterface* webrtcNewAudioSourceInterface(
    void* context,
    const struct AudioSourceInterfaceFunctions* functions) {
  auto source =
      new rtc::RefCountedObject<webrtc::DelegatingAudioSourceInterface>(
          context, functions);

  source->AddRef();

  return rtc::ToC(static_cast<webrtc::AudioSourceInterface*>(source));
}

RTC_EXPORT WebrtcAudioTrackSinkInterface* webrtcNewRingBufferAudioTrackSink(
    void* context,
    int buffersize) {
  auto sink = new webrtc::RingBufferAudioTrackSink(context, buffersize);
  return rtc::ToC(reinterpret_cast<webrtc::AudioTrackSinkInterface*>(sink));
}

RTC_EXPORT void webrtcDeleteRingBufferAudioTrackSink(
    WebrtcAudioTrackSinkInterface* sink) {
  delete reinterpret_cast<webrtc::RingBufferAudioTrackSink*>(rtc::ToCplusplus(sink));
}

RTC_EXPORT int webrtcRingBufferAudioTrackSinkAvailale(WebrtcAudioTrackSinkInterface* sink) {
  return reinterpret_cast<webrtc::RingBufferAudioTrackSink*>(
             rtc::ToCplusplus(sink))
      ->available();
}

RTC_EXPORT const void* webrtcRingBufferAudioTrackSinkData(
    WebrtcAudioTrackSinkInterface* sink) {
  return reinterpret_cast<webrtc::RingBufferAudioTrackSink*>(
             rtc::ToCplusplus(sink))
      ->data();
}

RTC_EXPORT void webrtcRingBufferAudioTrackSinkAdvance(
    WebrtcAudioTrackSinkInterface* sink,
    int amount) {
  return reinterpret_cast<webrtc::RingBufferAudioTrackSink*>(
             rtc::ToCplusplus(sink))
      ->advance(amount);
}

RTC_EXPORT const void* webrtcRingBufferAudioTrackSinkInfo(
    WebrtcAudioTrackSinkInterface* sink) {
  return reinterpret_cast<webrtc::RingBufferAudioTrackSink*>(
             rtc::ToCplusplus(sink))
      ->info();
}

extern "C" void webrtcVideoTrackInterfaceAddOrUpdateSink(
    WebrtcVideoTrackInterface* track,
    RtcVideoSinkInterface* sink,
    const struct RtcVideoSinkWants* cwants) {
  rtc::VideoSinkWants wants;

  wants.rotation_applied = cwants->rotation_applied;
  wants.black_frames = cwants->black_frames;
  wants.max_pixel_count = cwants->max_pixel_count;
  wants.max_framerate_fps = cwants->max_framerate_fps;

  if (cwants->has_target_pixel_count) {
    wants.target_pixel_count = cwants->target_pixel_count;
  }

  return rtc::ToCplusplus(track)->AddOrUpdateSink(rtc::ToCplusplus(sink),
                                                  wants);
}

extern "C" WebrtcMediaStreamTrackInterface*
webrtcVideoTrackInterfaceToWebrtcMediaStreamTrackInterface(
    WebrtcVideoTrackInterface* track) {
  return rtc::ToC(
      static_cast<webrtc::MediaStreamTrackInterface*>(rtc::ToCplusplus(track)));
}
