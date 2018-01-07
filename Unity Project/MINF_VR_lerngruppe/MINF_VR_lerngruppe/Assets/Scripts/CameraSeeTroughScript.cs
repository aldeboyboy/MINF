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

    private Texture2D receivedTexture;

    private int _CaptureCounter = 0;

    public RawImage localBackground;
    public AspectRatioFitter localFitter;

    public RawImage remoteBackground;
    public AspectRatioFitter remoteFitter;

    public Text localDebugText;

    public Canvas localCanvas;
    public Canvas remoteCanvas;

    private bool player1remote;
    private bool player2remote;

    private int arrayLength;

	// Use this for initialization
	void Start () {
        InitCamera();

        cameraOn = true;
        player1remote = false;
        player2remote = false;
        localCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        remoteCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        TakeSnapshot();
        //text.text = canvas.renderMode.ToString();
	}
	
	// Update is called once per frame
	void Update () {

        if(GameObject.Find("RemoteAvatar(Clone)")){
            Debug.Log("playerID = " + PhotonNetwork.player.ID);

            if(PhotonNetwork.player.ID == 1) {
                SendPlayer1Props();
                localDebugText.text = player2remote.ToString();
                GetPlayer2Props();
            }
            else if(PhotonNetwork.player.ID == 2) {
                SendPlayer2Props();
                localDebugText.text = player1remote.ToString();
                GetPlayer1Props();
            }

        }
              
        if(!camAvailable)
        {
            return;
        }

        float ratio = (float)backCam.width / (float)backCam.height;
        localFitter.aspectRatio = ratio;
        remoteFitter.aspectRatio = ratio;

        float scaleY = backCam.videoVerticallyMirrored ? -1f : 1f;
        localBackground.rectTransform.localScale = new Vector3(1f, scaleY, 1f);
        remoteBackground.rectTransform.localScale = new Vector3(1f, scaleY, 1f);

        int orient = -backCam.videoRotationAngle;
        localBackground.rectTransform.localEulerAngles = new Vector3(0, 0, orient);
        remoteBackground.rectTransform.localEulerAngles = new Vector3(0, 0, orient);

        if (!cameraOn && (OVRInput.Get(OVRInput.Button.Any) || Input.GetKeyDown("space")))
        {
            //text.text = canvas.renderMode.ToString();
            cameraOn = true;
            localCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            remoteCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            TakeSnapshot();
            if (PhotonNetwork.player.ID == 1)
            {
                player1remote = false;
            }
            else if (PhotonNetwork.player.ID == 2)
            {
                player2remote = false;
            }
            // Debug.Log("Switched to localCanvas");
        }
        else if (cameraOn && (OVRInput.Get(OVRInput.Button.Any) || Input.GetKeyDown("space")))
        {
            //text.text = canvas.renderMode.ToString();
            cameraOn = false;
            localCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            remoteCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            if (PhotonNetwork.player.ID == 1)
            {
                player1remote = true;
            }
            else if (PhotonNetwork.player.ID == 2)
            {
                player2remote = true;
            }
            // Debug.Log("Switched to remoteCanvas");
        }

        localDebugText.text = arrayLength.ToString();
	}

    void InitCamera()
    {
        defaultBackground = localBackground.texture;
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
        localBackground.texture = backCam;

        camAvailable = true;
    }

    private void SendPlayer1Props()
    {
        if (PhotonNetwork.connected)
        {
            hashtable["player1Remote"] = player1remote;
            if (player2remote) {
                hashtable["player1Texture"] = EncodePNG();
            }

            PhotonNetwork.room.SetCustomProperties(hashtable);
        }

    }

    private void GetPlayer2Props()
    {
        if (PhotonNetwork.connected)
        {
            player2remote = (bool)PhotonNetwork.room.CustomProperties["player2Remote"];

            byte[] player2Texture;
            player2Texture = null;

            if(player1remote) {
                player2Texture = (byte[])PhotonNetwork.room.CustomProperties["player2Texture"];
                DecodePNG(player2Texture);
            }

            /*if (player2Texture == null)
            {
                localDebugText.text = "null";
            }
            else
            {
                localDebugText.text = "not null";
            }*/
        }
    }

    private void SendPlayer2Props()
    {
        if (PhotonNetwork.connected)
        {
            hashtable["player2Remote"] = player2remote;
            if (player1remote)
            {
                hashtable["player2Texture"] = EncodePNG();
            }

            PhotonNetwork.room.SetCustomProperties(hashtable);
        }
    }

    private void GetPlayer1Props()
    {
        if (PhotonNetwork.connected)
        {
            player1remote = (bool)PhotonNetwork.room.CustomProperties["player1Remote"];

            byte[] player1Texture;
            player1Texture = null;

            if (player2remote)
            {
                player1Texture = (byte[])PhotonNetwork.room.CustomProperties["player1Texture"];
                DecodePNG(player1Texture);
            }

            /*if(player1Texture == null){
                localDebugText.text = "null";
            } else{
                localDebugText.text = "not null";
            }*/
        }
    }

    private byte[] EncodePNG(){
        Texture2D texture2d = new Texture2D(backCam.width, backCam.height);
        //texture2d.Resize(texture2d.width/16, texture2d.height/16, texture2d.format, false);
        texture2d.Apply();
        texture2d.SetPixels(backCam.GetPixels());

        return texture2d.EncodeToJPG(20);
    }

    private void DecodePNG(byte[] receivedByte)
    {
        arrayLength = receivedByte.Length;
        receivedTexture = null;
        receivedTexture = new Texture2D(backCam.width, backCam.height);
        receivedTexture.LoadImage(receivedByte);
        remoteBackground.texture = receivedTexture;
    }

    private void TakeSnapshot(){
        Texture2D snap = new Texture2D(backCam.width, backCam.height);
        snap.Apply();
        snap.SetPixels(backCam.GetPixels());

        System.IO.File.WriteAllBytes(Application.persistentDataPath + "/snap" + _CaptureCounter.ToString() + ".png", snap.EncodeToPNG());
        ++_CaptureCounter;
    }
}
