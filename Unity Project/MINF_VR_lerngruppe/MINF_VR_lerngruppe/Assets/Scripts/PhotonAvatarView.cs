using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent (typeof(PhotonView))]
public class PhotonAvatarView : MonoBehaviour {
	private PhotonView _pv;
	private OvrAvatar _ovrAvatar;
	private OvrAvatarRemoteDriver _ovrAvatarRemoteDriver;
	
	private List<byte[]> _packetData = new List<byte[]> (); // outgoing buffer

	private int _localSequence = 0;                         // local sequence number

	private bool _isMine = false;                           // was this object controlled by me in the last Update cycle?

	void Awake () {
		_pv = GetComponent<PhotonView> ();
		_ovrAvatar = GetComponent<OvrAvatar> ();
		_ovrAvatarRemoteDriver = GetComponent<OvrAvatarRemoteDriver> ();
	}

	void OnEnable () {
		_isMine = _pv.isMine;
		if (_pv.isMine) {
			SetPacketRecording (true);
		}
	}
	
	void OnDisable () {
		_isMine = _pv.isMine;
		if (_pv.isMine) {
			SetPacketRecording (false);
		}
	}
	
	void Update () {
		// PhotonView might switch owner at runtime, this is used to catch that
		if (_isMine != _pv.isMine) {
			SetPacketRecording (_pv.isMine);
			_isMine = _pv.isMine;
		}
	}

	/**
	 * Toggles the recording of the local avatar
	 */
	void SetPacketRecording (bool recording) {
		_ovrAvatar.RecordPackets = recording;
		if (recording) {
			_ovrAvatar.PacketRecorded += OnLocalAvatarPacketRecorded;
		} else {
			_ovrAvatar.PacketRecorded -= OnLocalAvatarPacketRecorded;
		}
	}

	/**
	 * Callback to process recorded avatar movements
	 */
	public void OnLocalAvatarPacketRecorded (object sender, OvrAvatar.PacketEventArgs args) {
		using (MemoryStream outputStream = new MemoryStream ()) {
			BinaryWriter writer = new BinaryWriter (outputStream);

			// Bring the recorded data in a sendable binary format
			var size = Oculus.Avatar.CAPI.ovrAvatarPacket_GetSize (args.Packet.ovrNativePacket);
			byte[] data = new byte[size];
			Oculus.Avatar.CAPI.ovrAvatarPacket_Write (args.Packet.ovrNativePacket, size, data);

			// Use BinaryWriter to generate byte sequence
			writer.Write (_localSequence++);           // local sequence number
			writer.Write (size);                       // size of the following data in bytes
			writer.Write (data);                       // the recorded avatar data

			_packetData.Add (outputStream.ToArray ()); // add the package to the queue
		}
	}
	
	/**
	 * Read the recorded avatar data from a remote avatar and queue it for playback
	 */
	private void DeserializeAndQueuePacketData (byte[] data) {
		using (MemoryStream inputStream = new MemoryStream (data)) {
			BinaryReader reader = new BinaryReader (inputStream);
			int remoteSequence = reader.ReadInt32 (); // retrieve the remote sequence number

			int size = reader.ReadInt32 ();           // retrieve the size for the rest of the data
			byte[] sdkData = reader.ReadBytes (size); // retrieve the binary avatar data
			
			// Use the AvatarSDK to import the remote avatar data
			System.IntPtr packet = Oculus.Avatar.CAPI.ovrAvatarPacket_Read ((System.UInt32) data.Length, sdkData);
			
			// Queue data for playback
			_ovrAvatarRemoteDriver.QueuePacket (remoteSequence, new OvrAvatarPacket {ovrNativePacket = packet});
		}
	}
	
	/**
	 * Called when Photon feels the need to send or recieve some data
	 * (there is some heuristic at work here and it's not guaranteed to be called each frame)
	 */
	public void OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info) {
		if (stream.isWriting) {
			if (_packetData.Count == 0) {
				return;
			}

			stream.SendNext (_packetData.Count);

			foreach (byte[] b in _packetData) {
				stream.SendNext (b);
			}

			_packetData.Clear ();
		} else {
			int num = (int) stream.ReceiveNext ();
			
			for (int counter = 0; counter < num; ++counter) {
				try {
					byte[] data = (byte[]) stream.ReceiveNext ();
					DeserializeAndQueuePacketData (data);
				} catch (InvalidCastException e) {
					Debug.LogError ("InvalidCastException in PhotonAvatarView::OnPhotonSerializeView()");
				}
			}
		}
	}
}
