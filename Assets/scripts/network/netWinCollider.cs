﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class netWinCollider : MonoBehaviour {

	//This is something that should exist elsewhere
	public Dictionary<GameObject, NetworkPlayer> ballPlayerMap = new Dictionary<GameObject, NetworkPlayer>();
	public GameObject messageTarget;
	networkVariables nvs;

	public void initialize()
	{
		nvs = messageTarget.GetComponent ("networkVariables") as networkVariables; 
		
		foreach(PlayerInfo player in nvs.players)
		{
			ballPlayerMap.Add(player.ballGameObject, player.player);
		}
	}

	void OnCollisionEnter( Collision coll)
	{
		if(Network.isClient){return;}	//server decides when it counts

		foreach( GameObject ball in ballPlayerMap.Keys)
		{
			if ( coll.gameObject == ball)
			{
				messageTarget.networkView.RPC ( "DeclareWinner", RPCMode.All, ballPlayerMap[ball] );
				break;
			}		
		}
	}

}
