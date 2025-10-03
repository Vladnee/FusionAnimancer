using System;
using Animancer;
using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
	public Material inputAuthColor;
	public Material serverColor;
	public Material proxyColor;

	private NetworkCharacterController _controller;
	private NetworkAnimancer           _networkAnimancer;

	[SerializeField]
	private AvatarMask _ActionMask;
	[Networked]
	private Single moveSpeed { get; set; }

	[Networked]
	private TickTimer _actionDuration2 { get; set; }

	[Networked]
	private TickTimer _actionDuration1 { get; set; }

	private AnimancerLayer _BaseLayer;
	private AnimancerLayer _ActionLayer;

	private void Awake( )
	{
		_networkAnimancer = GetComponent<NetworkAnimancer>( );
		_controller       = GetComponent<NetworkCharacterController>( );
	}

	public override void Spawned( )
	{
		_BaseLayer   = _networkAnimancer.Animancer.Layers[0];
		_ActionLayer = _networkAnimancer.Animancer.Layers[1]; // First access to a layer creates it.

		_ActionLayer.Mask = _ActionMask;
		_ActionLayer.SetDebugName( "Action Layer" );

		var library = _networkAnimancer.Animancer.Transitions.Definition;

		var idle = library.Transitions[0];
		_BaseLayer.Play( idle );

		ColorMeshBasedOnAuthority( );
	}

	public void Action1( )
	{
		if( _actionDuration1.ExpiredOrNotRunning( Runner ) )
		{
			var library = _networkAnimancer.Animancer.Transitions.Definition;

			var pistol = library.Transitions[2];
			var state  = _ActionLayer.GetOrCreateState( pistol );

			_ActionLayer.Play( pistol ).NormalizedTime = 0;

			_actionDuration1 = TickTimer.CreateFromSeconds( Runner, state.Duration );
		}
	}

	public void Action2( )
	{
		if( _actionDuration2.ExpiredOrNotRunning( Runner ) )
		{
			var library = _networkAnimancer.Animancer.Transitions.Definition;

			var golf  = library.Transitions[1];
			var state = _BaseLayer.GetOrCreateState( golf );

			_BaseLayer.Play( golf ).NormalizedTime = 0;

			_actionDuration2 = TickTimer.CreateFromSeconds( Runner, state.Duration );
		}
	}

	public override void FixedUpdateNetwork( )
	{
		if( _actionDuration1.Expired( Runner ) )
		{
			_actionDuration1 = TickTimer.None;

			_ActionLayer.StartFade( 0 );
		}

		if( _actionDuration2.Expired( Runner ) )
		{
			_actionDuration2 = TickTimer.None;

			var library = _networkAnimancer.Animancer.Transitions.Definition;

			var idle = library.Transitions[0];
			_BaseLayer.Play( idle );
		}
	}

	private void ColorMeshBasedOnAuthority( )
	{
		var meshs = GetComponentsInChildren<SkinnedMeshRenderer>( );

		var newMats = new Material[3];

		foreach( var t in meshs )
		{
			for( var i = 0; i < newMats.Length; ++i )
			{
				if( Object.HasStateAuthority )
					newMats[i] = serverColor;
				else if( Object.HasInputAuthority )
					newMats[i] = inputAuthColor;
				else if( Object.IsProxy )
					newMats[i] = proxyColor;
			}

			t.materials = newMats;
		}
	}
}