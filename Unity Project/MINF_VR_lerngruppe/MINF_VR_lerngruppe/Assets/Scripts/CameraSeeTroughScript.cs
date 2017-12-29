﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;

public class CameraSeeTroughScript : PunBehaviour {

    private bool camAvailable;
    private WebCamTexture backCam;
    private Texture defaultBackground;
    private bool cameraOn;

    private ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();

    private RawImage remotePlane;

    public RawImage background;
    public AspectRatioFitter fit;

    public Canvas localCanvas;
    public Canvas remoteCanvas;

	// Use this for initialization
	void Start () {
        InitCamera();

        cameraOn = true;
        localCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        remoteCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        //text.text = canvas.renderMode.ToString();
	}
	
	// Update is called once per frame
	void Update () {

        if(GameObject.Find("RemoteAvatar(Clone)")){
            remotePlane = GameObject.Find("remoteBg").GetComponent<RawImage>();

            Debug.Log("playerID = " + PhotonNetwork.player.ID);

            if(PhotonNetwork.player.ID == 1){
                SetTexturePlayer1();
                ShowTexturePlayer1();
            }else if(PhotonNetwork.player.ID == 2){
                SetTexturePlayer2();
                ShowTexturePlayer2();
            }

        }
              
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

        if (!cameraOn && (OVRInput.Get(OVRInput.Button.Any) || Input.GetKeyDown("space")))
        {
            //text.text = canvas.renderMode.ToString();
            cameraOn = true;
            localCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            remoteCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log("Switched to localCanvas");
        }
        else if (cameraOn && (OVRInput.Get(OVRInput.Button.Any) || Input.GetKeyDown("space")))
        {
            //text.text = canvas.renderMode.ToString();
            cameraOn = false;
            localCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            remoteCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            Debug.Log("Switched to remoteCanvas");
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

    private void SetTexturePlayer1()
    {
        if (PhotonNetwork.connected)
        {
            hashtable["player1Texture"] = backCam;

            PhotonNetwork.room.SetCustomProperties(hashtable);
            Debug.Log("Player 1 texture sent");
        }

    }

    private void ShowTexturePlayer1()
    {
        if (PhotonNetwork.connected)
        {
            WebCamTexture player2Texture = (WebCamTexture)PhotonNetwork.room.CustomProperties["player2Texture"];
            remotePlane.texture = player2Texture;
            Debug.Log("Player 2 texture received");
        }
    }

    private void SetTexturePlayer2()
    {
        if (PhotonNetwork.connected)
        {
            hashtable["player2Texture"] = backCam;

            PhotonNetwork.room.SetCustomProperties(hashtable);
            Debug.Log("Player 2 texture sent");
        }

    }

    private void ShowTexturePlayer2()
    {
        if (PhotonNetwork.connected)
        {
            WebCamTexture player1Texture = (WebCamTexture)PhotonNetwork.room.CustomProperties["player1Texture"];
            remotePlane.texture = player1Texture;
            Debug.Log("Player 1 texture received");
        }
    }
}
