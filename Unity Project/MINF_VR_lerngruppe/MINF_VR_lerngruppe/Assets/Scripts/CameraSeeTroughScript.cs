﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraSeeTroughScript : MonoBehaviour {

    private bool camAvailable;
    private WebCamTexture backCam;
    private Texture defaultBackground;
    private bool cameraOn;

    public Text text;
    public RawImage background;
    public AspectRatioFitter fit;
    public Canvas canvas;

	// Use this for initialization
	void Start () {
        InitCamera();

        cameraOn = false;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        text.text = canvas.renderMode.ToString();
	}
	
	// Update is called once per frame
	void Update () {

        //Camera.main.fieldOfView = 180.0f;
              
        if(!camAvailable)
        {
            return;
        }

        float ratio = (float)backCam.width / (float)backCam.height;
        fit.aspectRatio = ratio;

        float scaleY = backCam.videoVerticallyMirrored ? -1f : 1f;
        background.rectTransform.localScale = new Vector3(1f, scaleY, 1f);

        int orient = -backCam.videoRotationAngle;
        background.rectTransform.localEulerAngles = new Vector3(0, 0, orient);

        if (!cameraOn && OVRInput.Get(OVRInput.Button.Any))
        {
            text.text = canvas.renderMode.ToString();
            cameraOn = true;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        }
        else if (cameraOn && OVRInput.Get(OVRInput.Button.Any))
        {
            text.text = canvas.renderMode.ToString();
            cameraOn = false;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
	}

    void InitCamera()
    {
        defaultBackground = background.texture;
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            //text.text = "No Camera detected";
            camAvailable = false;
            return;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing)
            {
                backCam = new WebCamTexture(devices[i].name, Screen.width, Screen.height);
            }
        }

        if (backCam == null)
        {
            //text.text =  "Unable to find back camera";
            return;
        }

        backCam.Play();
        background.texture = backCam;

        camAvailable = true;
    }
}
