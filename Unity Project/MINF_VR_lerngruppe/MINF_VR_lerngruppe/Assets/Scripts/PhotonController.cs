using System;
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;

public enum NetworkEventCodes : byte {
	ParticipantJoined = 101
}

public class PhotonController : PunBehaviour {
	public String GameVersion;
	public Transform RemoteAvatarSlot;

	public byte MaxPlayers = 2;

	public Transform TrackingSpace;
	public GameObject LocalAvatarPrefab;
	public GameObject RemoteAvatarPrefab;

	// Use this for initialization
	void Start () {
		PhotonNetwork.ConnectUsingSettings(GameVersion);
	}

	void OnEnable() {
		PhotonNetwork.OnEventCall += OnEvent;
	}

	void OnDisable() {
		PhotonNetwork.OnEventCall -= OnEvent;
	}

	private void OnEvent(byte eventcode, object content, int senderid) {
		String eventName = "UNKNOWN";

		if(Enum.IsDefined (typeof (NetworkEventCodes), eventcode)) {
			eventName = Enum.GetName(typeof (NetworkEventCodes), eventcode);
		}

		Debug.Log(
			"[PhotonController]: Received event: [" +
			"eventName = " + eventName +
			", eventCode = " + eventcode +
			", senderId = " + senderid +
			"]"
		);

		if (eventcode == (byte)NetworkEventCodes.ParticipantJoined) {
			GameObject go = null;

			Debug.Log("[PhotonController]: Executing VR avatar instantiation");

			if(PhotonNetwork.player.ID == senderid) {
				Debug.Log("[PhotonController]: Setup local avatar for sending");
				go = Instantiate(LocalAvatarPrefab, TrackingSpace);
			}
			else {
				Debug.Log("[PhotonController]: Instantiated remote avatar");
				if(RemoteAvatarPrefab) {
					go = Instantiate(RemoteAvatarPrefab, TrackingSpace);
				}
			}

			if(go != null) {
				PhotonView pView = go.GetComponent<PhotonView>();

				if (pView != null) {
					pView.viewID = (int)content;
				}
			}
		}
	}

	public override void OnConnectedToMaster() {
		Debug.Log("[PhotonController]: Connected to Master");
		PhotonNetwork.JoinRandomRoom();
	}

	public override void OnJoinedLobby() {
		Debug.Log("[PhotonController]: Lobby joined");
		PhotonNetwork.JoinRandomRoom();
	}

	public void OnPhotonRandomJoinFailed() {
		Debug.Log("[PhotonController]: Random join failed");
		PhotonNetwork.CreateRoom (
			null,
			new RoomOptions() {
				MaxPlayers = MaxPlayers
			},
			null
		);
	}

	public override void OnFailedToConnectToPhoton(DisconnectCause cause) {
		Debug.LogError("[PhotonController]: Failed to connect to Photon: " + cause);
	}

	public override void OnJoinedRoom() {
		Debug.Log("[PhotonController]: Joined room");
		int viewId = PhotonNetwork.AllocateViewID();

		PhotonNetwork.RaiseEvent(
			(byte) NetworkEventCodes.ParticipantJoined,
			viewId,
			true,
			new RaiseEventOptions() {
				CachingOption = EventCaching.AddToRoomCache,
				Receivers = ReceiverGroup.All
			}
		);
	}

	// Update is called once per frame
	void Update () {

	}
}
