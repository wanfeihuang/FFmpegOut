using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FFmpegOut;
using UnityEngine.UI;
public class WebCamTest : MonoBehaviour
{
    [SerializeField]
    private WebCameraCapture _capture;
    [SerializeField]
    private RawImage _rawImg;

    [SerializeField]
    private Button _btnStart;
    [SerializeField]
    private Button _btnstop;


    private void Awake()
    {
        _btnStart.onClick.AddListener(()=> 
        {
            _rawImg.texture = _capture.webTex;
            _capture.StartCapture("test.mp4",5f);
        });

        _btnstop.onClick.AddListener(()=> 
        {
            _capture.StopCapture();
        });
    }

}
