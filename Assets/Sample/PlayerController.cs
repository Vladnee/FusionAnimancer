using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
	protected NetworkCharacterController _ncc;
	protected NetworkTransform           _nt;

	[Networked]
	public Vector3 MovementDirection { get; set; }

	public override void Spawned( )
	{
		_ncc = GetComponent<NetworkCharacterController>( );
		_nt = GetComponent<NetworkTransform>( );
	}

	public override void FixedUpdateNetwork( )
	{
		Vector3 direction;

		if( GetInput( out NetworkInputPrototype input ) )
		{
			direction = default;

			if( input.IsDown( NetworkInputPrototype.BUTTON_FORWARD ) )
				direction += Vector3.forward;

			if( input.IsDown( NetworkInputPrototype.BUTTON_BACKWARD ) )
				direction -= Vector3.forward;

			if( input.IsDown( NetworkInputPrototype.BUTTON_LEFT ) )
				direction -= Vector3.right;

			if( input.IsDown( NetworkInputPrototype.BUTTON_RIGHT ) )
				direction += Vector3.right;

			if( input.IsDown( NetworkInputPrototype.BUTTON_ACTION1 ) ) // Button E
			{
				var player = GetComponent<Player>( );

				if( player )
					player.Action1( );
			}

			if( input.IsDown( NetworkInputPrototype.BUTTON_ACTION2 ) ) // Button Q
			{
				var player = GetComponent<Player>( );

				if( player )
					player.Action2( );
			}

			if( input.IsDown( NetworkInputPrototype.BUTTON_RELOAD ) ) // Button R
			{ }

			direction         = direction.normalized;
			MovementDirection = direction;
		}
		else
			direction = MovementDirection;

		_ncc.Move( direction );
	}
}