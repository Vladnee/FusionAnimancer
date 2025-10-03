using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using Object = System.Object;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
	private static Int32 _joinedPlayers;
	[SerializeField]
	public GameObject playerPrefab;

	public void OnObjectExitAOI( NetworkRunner  runner, NetworkObject obj, PlayerRef player ) { }
	public void OnObjectEnterAOI( NetworkRunner runner, NetworkObject obj, PlayerRef player ) { }
	public void OnPlayerJoined( NetworkRunner runner, PlayerRef player )
	{
		if( !runner.IsServer )
			return;

		if( _joinedPlayers == 2 )
		{
			var playerobj = runner.Spawn( playerPrefab );
			playerobj.transform.name = $"Player - {player}";
			playerobj.AssignInputAuthority( player );
		}


		_joinedPlayers++;
	}

	public void OnPlayerLeft( NetworkRunner                   runner, PlayerRef                                player )                                       { }
	public void OnShutdown( NetworkRunner                     runner, ShutdownReason                           shutdownReason )                               { }
	public void OnDisconnectedFromServer( NetworkRunner       runner, NetDisconnectReason                      reason )                                       { }
	public void OnConnectRequest( NetworkRunner               runner, NetworkRunnerCallbackArgs.ConnectRequest request,       Byte[]                 token )  { }
	public void OnConnectFailed( NetworkRunner                runner, NetAddress                               remoteAddress, NetConnectFailedReason reason ) { }
	public void OnUserSimulationMessage( NetworkRunner        runner, SimulationMessagePtr                     message )                                              { }
	public void OnReliableDataReceived( NetworkRunner         runner, PlayerRef                                player, ReliableKey key, ArraySegment<Byte> data )     { }
	public void OnReliableDataProgress( NetworkRunner         runner, PlayerRef                                player, ReliableKey key, Single             progress ) { }
	public void OnInput( NetworkRunner                        runner, NetworkInput                             input )                      { }
	public void OnInputMissing( NetworkRunner                 runner, PlayerRef                                player, NetworkInput input ) { }
	public void OnConnectedToServer( NetworkRunner            runner )                                                { }
	public void OnSessionListUpdated( NetworkRunner           runner, List<SessionInfo>          sessionList )        { }
	public void OnCustomAuthenticationResponse( NetworkRunner runner, Dictionary<String, Object> data )               { }
	public void OnHostMigration( NetworkRunner                runner, HostMigrationToken         hostMigrationToken ) { }
	public void OnSceneLoadDone( NetworkRunner                runner ) { }
	public void OnSceneLoadStart( NetworkRunner               runner ) { }
}