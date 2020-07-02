// FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/KlakNDI

using UnityEngine;
using System.Collections;
using System;

namespace FFmpegOut
{
    [AddComponentMenu("FFmpegOut/WebCamera Capture")]
    public sealed class WebCameraCapture : MonoBehaviour
    {
        #region Public properties

        [SerializeField] int _width = 1920;

        public int width
        {
            get { return _width; }
            set { _width = value; }
        }

        [SerializeField] int _height = 1080;

        public int height
        {
            get { return _height; }
            set { _height = value; }
        }

        [SerializeField] FFmpegPreset _preset;

        public FFmpegPreset preset
        {
            get { return _preset; }
            set { _preset = value; }
        }

        [SerializeField] float _frameRate = 30;

        public float frameRate
        {
            get { return _frameRate; }
            set { _frameRate = value; }
        }

        public WebCamTexture webTex
        {
            get { return _webCamTex; }
        }
        #endregion

        #region Private members

        FFmpegSession _session;
        WebCamTexture _webCamTex;
        RenderTextureFormat GetTargetFormat(Camera camera)
        {
            return camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        }

        int GetAntiAliasingLevel(Camera camera)
        {
            return camera.allowMSAA ? QualitySettings.antiAliasing : 1;
        }

        WebCamTexture GetWebCameraTexture(int index = 0)
        {
            WebCamTexture camTexture = null;
            for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++)
            {
                var device = WebCamTexture.devices[cameraIndex];
                if (index == 0)
                {
                    camTexture = new WebCamTexture(device.name, _width, _height,60);
                    break;
                }
            }
            return camTexture;
        }

        IEnumerator WaitForSeconds(float seconds, Action onFinished)
        {
            yield return new WaitForSeconds(seconds);
            if (onFinished != null)
            {
                onFinished.Invoke();
            }
        }
        #endregion

        #region Time-keeping variables

        int _frameCount;
        float _startTime;
        int _frameDropCount;

        float FrameTime
        {
            get { return _startTime + (_frameCount - 0.5f) / _frameRate; }
        }

        void WarnFrameDrop()
        {
            if (++_frameDropCount != 10) return;

            Debug.LogWarning(
                "Significant frame droppping was detected. This may introduce " +
                "time instability into output video. Decreasing the recording " +
                "frame rate is recommended."
            );
        }

        #endregion

        #region MonoBehaviour implementation

        void OnValidate()
        {
            _width = Mathf.Max(8, _width);
            _height = Mathf.Max(8, _height);
        }

        void OnDisable()
        {
            StopCapture();
            if (_webCamTex != null)
            {
                _webCamTex.Stop();
            }
        }
        IEnumerator Start()
        {
            _webCamTex = GetWebCameraTexture();
            _webCamTex.Play();

            // Sync with FFmpeg pipe thread at the end of every frame.
            for (var eof = new WaitForEndOfFrame(); ;)
            {
                yield return eof;
                _session?.CompletePushFrames();
            }
        }

        void Update()
        {
            if (_webCamTex == null||!_webCamTex.isPlaying || _session==null)
                return;

            var gap = Time.time - FrameTime;
            var delta = 1 / _frameRate;

            if (gap < 0)
            {
                // Update without frame data.
                _session.PushFrame(null);
            }
            else if (gap < delta)
            {
                // Single-frame behind from the current time:
                // Push the current frame to FFmpeg.
                _session.PushFrame(_webCamTex as Texture);
                _frameCount++;
            }
            else if (gap < delta * 2)
            {
                // Two-frame behind from the current time:
                // Push the current frame twice to FFmpeg. Actually this is not
                // an efficient way to catch up. We should think about
                // implementing frame duplication in a more proper way. #fixme
                _session.PushFrame(_webCamTex as Texture);
                _session.PushFrame(_webCamTex as Texture);
                _frameCount += 2;
            }
            else
            {
                // Show a warning message about the situation.
                WarnFrameDrop();

                // Push the current frame to FFmpeg.
                _session.PushFrame(_webCamTex as Texture);

                // Compensate the time delay.
                _frameCount += Mathf.FloorToInt(gap * _frameRate);
            }
        }

        #endregion

        public void StartCapture(string path,float length)
        {
            Debug.Log("StartCapture!!");
            if (length > 0)
            {
                length = Mathf.Max(1, length);
                StartCoroutine(WaitForSeconds(length,()=> 
                {
                    StopCapture();
                }));
            }
            else
            {
                //用户手动调用结束接口进行结束
            }

            _session = FFmpegSession.Create(
                path,
                _webCamTex.width,
                _webCamTex.height,
                _frameRate, preset
                );

            _startTime = Time.time;
            _frameCount = 0;
            _frameDropCount = 0;
        }

        public void StopCapture()
        {
            Debug.Log("StopCapture!!");
            StopAllCoroutines();
            if (_session != null)
            {
                _session.Close();
                _session.Dispose();
                _session = null;
            }
        }
    }
}
