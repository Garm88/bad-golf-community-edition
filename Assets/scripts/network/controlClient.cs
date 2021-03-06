﻿using UnityEngine;
using System.Collections;

public class controlClient : MonoBehaviour {
	float timer = 0;
	PlayerInfo myInfo;
	networkVariables nvs;
	Transform cameraParentTransform;
	GameObject pin;
	GameObject localBallAnalog;	


	void Start() {
		// get variables we need
		nvs = GetComponent("networkVariables") as networkVariables;
		myInfo = nvs.myInfo;
		pin = GameObject.Find ("winningPole") as GameObject;

		localBallAnalog = new GameObject ();
	}

	void Update () {
		// only do key presses if it's not paused
		if (!myInfo.playerIsPaused) {
			// if in buggy
			if (myInfo.currentMode==0) {
				// send packets about keyboard every 0.015s
				timer += Time.deltaTime;
				if (timer > 0.015) {
					timer = 0;
					float h;
					float v;
					#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
						h = Input.acceleration.x;
						v = Input.acceleration.y + .5f;
					#else
						h = Input.GetAxis("Horizontal");
						v = Input.GetAxis("Vertical");
					#endif
					networkView.RPC("KartMovement", RPCMode.Server, h,v);
				}
				// HONK
				if (Input.GetKeyDown(KeyCode.Q)) {
					// need to wait for the audio guys to fix this
					// if you think you've fixed this test in online aswell
					networkView.RPC("IHonked", RPCMode.All, myInfo.player);
				}
				// reset
				if (Input.GetKeyDown(KeyCode.R) && Network.isClient) {
					networkView.RPC("ResetMe", RPCMode.Server);
				}
			}

		} else {
			// send packets about keyboard every 0.015s
			timer += Time.deltaTime;
			if (timer > 0.015) {
				// send a no-keys-pressed message
				networkView.RPC("KartMovement", RPCMode.Server, 0f, 0f);
			}
		}
	}

	// local interpolation - add all other interpolation here aswell
	void FixedUpdate() {
		// if in buggy
		if (myInfo.currentMode==0) {
			// maybe not the best idea to call GetComponent every time - add it to PlayerInfo at some point so it can do a direct reference
			CarController car = myInfo.cartGameObject.transform.GetComponent("CarController") as CarController;
			car.Move(myInfo.h,myInfo.v);
		} else if (myInfo.currentMode==1) {		// if in ball mode
			
		} else if (myInfo.currentMode==2) {		// if in spectate mode
			
		}
	}

	void switchToBall(){
		// if in buggy
		if (myInfo.currentMode==0) {
			myInfo.currentMode = 1;
			networkView.RPC ("PlayerSwap", RPCMode.Others, 1, myInfo.player);	//to ball
			//stop cart
			myInfo.cartGameObject.rigidbody.velocity = Vector3.zero;
			myInfo.cartGameObject.rigidbody.angularVelocity = Vector3.zero;
			// set them at golf ball
			myInfo.ballGameObject.transform.rotation = Quaternion.identity;

			localBallAnalog.transform.position = myInfo.ballGameObject.transform.position;	//hack_answers
			localBallAnalog.transform.rotation = myInfo.ballGameObject.transform.rotation;	//hack_answers
			localBallAnalog.transform.localScale = myInfo.ballGameObject.transform.localScale;	//hack_answers
			//myInfo.characterGameObject.transform.parent = myInfo.ballGameObject.transform;
			myInfo.characterGameObject.transform.parent = localBallAnalog.transform;

			myInfo.characterGameObject.transform.localPosition = new Vector3(1.7f,-.2f,0);
			myInfo.characterGameObject.transform.localRotation = Quaternion.identity * new Quaternion(0f, -Mathf.PI/2, 0f, 1f);

			myInfo.ballGameObject.transform.rotation = Quaternion.LookRotation((pin.transform.position - myInfo.ballGameObject.transform.position) - new Vector3(0, pin.transform.position.y - myInfo.ballGameObject.transform.position.y,0));	
			localBallAnalog.transform.rotation = myInfo.ballGameObject.transform.rotation;

			// lock golf ball
			myInfo.ballGameObject.rigidbody.constraints = RigidbodyConstraints.FreezeAll;
			//*/ move camera - HACKY
			GameObject buggyCam = nvs.myCam.gameObject;
			(buggyCam.GetComponent("FollowPlayerScript") as FollowPlayerScript).enabled = false;
			//buggyCam.transform.parent = myInfo.ballGameObject.transform;
			cameraParentTransform = buggyCam.transform.parent;	// keep a reference for later
			buggyCam.transform.parent = localBallAnalog.transform;	//hack_answers

			buggyCam.transform.localPosition = new Vector3(-6,4,0);
			buggyCam.transform.rotation = myInfo.ballGameObject.transform.rotation;

			//*/// change animation - try and keep the prefabs similar so this doesn't become a massive else if list
			if (myInfo.characterModel=="lil_patrick") {
				myInfo.characterGameObject.transform.FindChild(myInfo.characterModel).animation.Play("golfIdle",PlayMode.StopAll);
			} else {
				myInfo.characterGameObject.animation.Play("golfIdle",PlayMode.StopAll);
			}
		}
	}
	
	void switchToCart(){
		myInfo.currentMode = 0;
		networkView.RPC ("PlayerSwap", RPCMode.Others, 0, myInfo.player);	//to cart
		// set them in buggy
		myInfo.characterGameObject.transform.parent = myInfo.cartGameObject.transform;
		myInfo.characterGameObject.transform.localPosition = new Vector3(0,0,0);
		myInfo.characterGameObject.transform.rotation = myInfo.cartGameObject.transform.rotation;
		// unlock golf ball
		myInfo.ballGameObject.rigidbody.constraints = RigidbodyConstraints.None;
		//*/ move camera - HACKY
		GameObject buggyCam = nvs.myCam.gameObject;
		buggyCam.transform.parent = cameraParentTransform;	// put it back
		
		(buggyCam.GetComponent("FollowPlayerScript") as FollowPlayerScript).enabled = true;
		//*/// change animation - try and keep the prefabs similar so this doesn't become a massive else if list
		if (myInfo.characterModel=="lil_patrick") {
			myInfo.characterGameObject.transform.FindChild(myInfo.characterModel).animation.Play("driveIdle",PlayMode.StopAll);
		} else {
			myInfo.characterGameObject.animation.Play("driveIdle",PlayMode.StopAll);
		}

	}
	
	// honks
	[RPC]
	void IHonked(NetworkPlayer player) {
		// find the player
		foreach (PlayerInfo p in nvs.players) {
			if (p.player==player) {
				SoundManager.Get().playSfx3d(p.cartGameObject, "Car Horn 1", 5, 500, 1);
			}
		}
	}

	// when a player changes mode - player needed since it would comes from the server!
	[RPC]
	void PlayerSwap(int newMode, NetworkPlayer player) {
		// find the player
		foreach (PlayerInfo p in nvs.players)
		{
			if (p.player==player) {
				p.currentMode = newMode;
				if (p.currentMode==0) {			// if they're now in a buggy
					// set them in buggy
					p.characterGameObject.transform.parent = p.cartGameObject.transform;
					p.characterGameObject.transform.localPosition = new Vector3(0,0,0);
					p.characterGameObject.transform.rotation = p.cartGameObject.transform.rotation;
					// unlock golf ball
					p.ballGameObject.rigidbody.constraints = RigidbodyConstraints.None;
					// change animation - try and keep the prefabs similar so this doesn't become a massive else if list
					if (p.characterModel=="lil_patrick") {
						p.characterGameObject.transform.FindChild(p.characterModel).animation.Play("driveIdle",PlayMode.StopAll);
					} else {
						p.characterGameObject.animation.Play("driveIdle",PlayMode.StopAll);
					}
					
				} else if (p.currentMode==1) {	// if they're now at golf ball
					// set them at golf ball
					p.ballGameObject.transform.rotation = Quaternion.identity;
					p.characterGameObject.transform.parent = p.ballGameObject.transform;	
					p.characterGameObject.transform.localPosition = new Vector3(1.7f,-.2f,0);
					p.characterGameObject.transform.localRotation = Quaternion.identity * new Quaternion(0f, -Mathf.PI/2, 0f, 1f);	//90degrees to camera angle
					p.ballGameObject.transform.rotation = Quaternion.LookRotation((pin.transform.position - p.ballGameObject.transform.position) - new Vector3(0, pin.transform.position.y - p.ballGameObject.transform.position.y,0));	

					// lock golf ball
					p.ballGameObject.rigidbody.constraints = RigidbodyConstraints.FreezeAll;
					// change animation - try and keep the prefabs similar so this doesn't become a massive else if list
					if (p.characterModel=="lil_patrick") {
						p.characterGameObject.transform.FindChild(p.characterModel).animation.Play("golfIdle",PlayMode.StopAll);
					} else {
						p.characterGameObject.animation.Play("golfIdle",PlayMode.StopAll);
					}

				}
				
				// reset keyboard buffer
				p.h = 0f;
				p.v = 0f;
			}
		}
	}

	// blank for server use only
	[RPC]
	void KartMovement(float h, float v) {}
	[RPC]
	void SpawnBall(NetworkViewID viewId) {}
	[RPC]
	void ResetMe() {}
}
