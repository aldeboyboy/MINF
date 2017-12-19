using System;
using System.Collections;
using System.Collections.Generic;
using Photon;
using UnityEngine;
using UnityEngine.UI;

/**
 * Custom Photon Event Codes
 * Custom Events start from 101 since [1..100] is reserved by Photon
 */
public enum NetworkEventCodes : byte {
	ParticipantJoined = 101
}

public class PhotonController : PunBehaviour {
	public string GameVersion;
	public Transform RemoteAvatarSlot;

    public Text text;

	public byte MaxPlayers = 2;

    public Transform TrackingSpace;
	public GameObject LocalAvatarPrefab;
	public GameObject RemoteAvatarPrefab;

	void Start () {
		// Players are grouped by game version, if two clients have another version than the other,
		// the won't be able to see each other!
		PhotonNetwork.ConnectUsingSettings (GameVersion);
	}

	void OnEnable () {
		PhotonNetwork.OnEventCall += OnEvent;
	}

	void OnDisable () {
		PhotonNetwork.OnEventCall -= OnEvent;
	}

	private void OnEvent (byte eventcode, object content, int senderid) {
		string eventName = "UNKNOWN";

		if (Enum.IsDefined (typeof (NetworkEventCodes), eventcode)) {
			eventName = Enum.GetName (typeof (NetworkEventCodes), eventcode);
		}
		
		Debug.Log (
			"[PhotonController]: Recieved event: [" +
			"eventName=" + eventName + 
			", eventCode=" + eventcode + 
			", senderId=" + senderid + 
			"]"
		);

		if (eventcode == (byte) NetworkEventCodes.ParticipantJoined) {
			GameObject go = null;
            
			text.text = "[PhotonController]: Executing VR avatar instantiation";

			if (PhotonNetwork.player.ID == senderid) {
                text.text = "[PhotonController]: Setup local avatar for sending";
				go = Instantiate (LocalAvatarPrefab, TrackingSpace);
			} else {
                text.text = "[PhotonController]: Instantiated remote avatar";
				if (RemoteAvatarPrefab) {
					go = Instantiate (RemoteAvatarPrefab, RemoteAvatarSlot);
				}
			}

			if (go != null) {
				PhotonView pView = go.GetComponent<PhotonView> ();

				if (pView != null) {
					pView.viewID = (int) content;
				}
			}
		}
	}
	
	public override void OnConnectedToMaster () {
        text.text = "[PhotonController]: Connected to master";
		PhotonNetwork.JoinRandomRoom ();
	}

	public override void OnJoinedLobby () {
        text.text = "[PhotonController]: Lobby joined";
		PhotonNetwork.JoinRandomRoom ();
	}
	
	public void OnPhotonRandomJoinFailed () {
        text.text = "[PhotonController]: Random join failed";
		PhotonNetwork.CreateRoom (
			null,
			new RoomOptions () {
				MaxPlayers = MaxPlayers
			},
			null
		);
	}
	
	public override void OnFailedToConnectToPhoton (DisconnectCause cause) {
        text.text = "[PhotonController]: Failed to connect to Photon: " + cause;
	}
	
	public override void OnJoinedRoom () {
        text.text = "[PhotonController]: Joined room";
		int viewId = PhotonNetwork.AllocateViewID ();

		PhotonNetwork.RaiseEvent (
			(byte) NetworkEventCodes.ParticipantJoined,
			viewId,
			true,
			new RaiseEventOptions () {
				CachingOption = EventCaching.AddToRoomCache,
				Receivers = ReceiverGroup.All
			}
		);
	}
}
