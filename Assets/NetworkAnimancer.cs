using System;
using Animancer;
using Fusion;
using UnityEngine;

public class NetworkAnimancer : NetworkBehaviour, IBeforeAllTicks, IAfterAllTicks
{
	[field: SerializeField]
	public AnimancerComponent Animancer { get; private set; }

	[field: SerializeField]
	public StringAsset VarMoveSpeed { get; private set; }

	[Networked]
	private AnimancerData NetworkData { get; set; }

	private Parameter<Single>             _varMoveSpeed;
	private PropertyReader<AnimancerData> _networkDataReader;

	private NetworkCharacterController _ncc;

	public override void Spawned( )
	{
		_ncc = GetComponent<NetworkCharacterController>( );

		Animancer.Graph.PauseGraph( );

		_networkDataReader = GetPropertyReader<AnimancerData>( nameof(NetworkData) );

		_varMoveSpeed = Animancer.Parameters.GetOrCreate<Single>( VarMoveSpeed );
	}

	public void BeforeAllTicks( Boolean resimulation, Int32 tickCount )
	{
		ApplyState( NetworkData );
	}

	public override void FixedUpdateNetwork( )
	{
		_varMoveSpeed.Value = _ncc.Velocity.magnitude / _ncc.maxSpeed;

		Animancer.Evaluate( Runner.DeltaTime );
	}

	public void AfterAllTicks( Boolean resimulation, Int32 tickCount )
	{
		SaveState( );
	}

	public override void Render( )
	{
		if( !TryGetSnapshotsBuffers( out var fromBuf, out var toBuf, out var alpha ) )
			return;

		var from = _networkDataReader.Read( fromBuf );
		var to   = _networkDataReader.Read( toBuf );

		ApplyState( AnimancerData.Lerp( from, to, alpha ) );
	}

	private void SaveState( )
	{
		var networkData = NetworkData;

		networkData.NetworkLayers.Clear( );

		for( var i = 0; i < Animancer.Layers.Count; i++ )
		{
			var animancerLayer = Animancer.Layers[i];

			var netLayerData = new LayerData
			{
				Weight                = animancerLayer.Weight,
				TargetWeight          = animancerLayer.TargetWeight,
				RemainingFadeDuration = animancerLayer.FadeGroup?.RemainingFadeDuration ?? 0
			};

			for( var j = 0; j < animancerLayer.ChildCount; j++ )
			{
				var animancerState = animancerLayer[j];

				netLayerData.States.Add(
					new StateData
					{
						TransitionIndex = (Byte)Animancer.Graph.Transitions.IndexOf( animancerState.Key ),
						NormalizedTime  = animancerState.NormalizedTime,
						Weight          = animancerState.Weight
					}
				);

				if( animancerState.FadeGroup != null && animancerState.TargetWeight >= 1 )
					netLayerData.RemainingStateFadeDuration = animancerState.FadeGroup.RemainingFadeDuration;

				if( animancerState == animancerLayer.CurrentState )
					netLayerData.CurrentStateTransitionIndex = netLayerData.States[j].TransitionIndex;
			}

			networkData.NetworkLayers.Set( i, netLayerData );
		}

		networkData.VarMoveSpeed = _varMoveSpeed.Value;
		NetworkData              = networkData;
	}

	private void ApplyState( AnimancerData animancerData )
	{
		for( var i = 0; i < Animancer.Layers.Count; i++ )
		{
			var animancerLayer = Animancer.Layers[i];

			var netLayerData = animancerData.NetworkLayers.Get( i );

			if( netLayerData.TargetWeight > 0 || netLayerData.Weight > 0 )
			{
				foreach( var state in animancerLayer )
					state.Weight = 0;

				foreach( var stateData in netLayerData.States )
				{
					if( !Animancer.Graph.Transitions.TryGetTransition( stateData.TransitionIndex, out var transition ) )
					{
						Debug.LogError( $"Transition Library '{Animancer.Transitions}' doesn't contain transition index {stateData.TransitionIndex}.", Animancer );

						continue;
					}

					var state = animancerLayer.GetOrCreateState( transition.Transition );

					state.IsPlaying      = true;
					state.NormalizedTime = stateData.NormalizedTime;
					state.Weight         = stateData.Weight;

					if( netLayerData.RemainingStateFadeDuration <= 0 )
						continue;

					var targetWeight = netLayerData.CurrentStateTransitionIndex == stateData.TransitionIndex? 1: 0;
					state.StartFade( targetWeight, netLayerData.RemainingStateFadeDuration );
				}
			}

			animancerLayer.Weight = netLayerData.Weight;

			if( !Mathf.Approximately( animancerLayer.Weight, netLayerData.TargetWeight ) )
				animancerLayer.StartFade( netLayerData.TargetWeight, netLayerData.RemainingFadeDuration );
		}

		_varMoveSpeed.Value = animancerData.VarMoveSpeed;
		Animancer.Evaluate( );
	}

	public struct AnimancerData : INetworkStruct
	{
		// parameters______________________________
		[Networked]
		public Single VarMoveSpeed { get; set; }

		// layers__________________________________
		[Networked, Capacity( 2 )]
		public NetworkArray<LayerData> NetworkLayers => default;

		public static AnimancerData Lerp( in AnimancerData from, in AnimancerData to, Single alpha )
		{
			var result = new AnimancerData
			{
				VarMoveSpeed = Mathf.Lerp( from.VarMoveSpeed, to.VarMoveSpeed, alpha )
			};

			for( var i = 0; i < from.NetworkLayers.Length; i++ )
				result.NetworkLayers.Set( i, LayerData.Lerp( from.NetworkLayers[i], to.NetworkLayers[i], alpha ) );

			return result;
		}
	}

	public struct LayerData : INetworkStruct
	{
		[Networked]
		public Single Weight { get; set; }

		[Networked]
		public Single TargetWeight { get; set; }

		[Networked]
		public Single RemainingFadeDuration { get; set; }

		[Networked, Capacity( 8 )]
		public NetworkLinkedList<StateData> States => default;

		[Networked]
		public Byte CurrentStateTransitionIndex { get; set; }

		[Networked]
		public Single RemainingStateFadeDuration { get; set; }

		public static LayerData Lerp( in LayerData a, in LayerData b, Single t )
		{
			var result = new LayerData
			{
				CurrentStateTransitionIndex = a.CurrentStateTransitionIndex,
				Weight                      = Mathf.Lerp( a.Weight, b.Weight, t ),
				TargetWeight                = Mathf.Lerp( a.TargetWeight, b.TargetWeight, t ),
				RemainingFadeDuration       = Mathf.Lerp( a.RemainingFadeDuration, b.RemainingFadeDuration, t ),
				RemainingStateFadeDuration  = Mathf.Lerp( a.RemainingStateFadeDuration, b.RemainingStateFadeDuration, t )
			};

			var count = a.States.Count;

			for( var i = 0; i < count; i++ )
			{
				var sa = a.States[i];
				var sb = b.States[i];

				result.States.Add( StateData.Lerp( sa, sb, t ) );
			}

			return result;
		}
	}

	public struct StateData : INetworkStruct
	{
		[Networked]
		public Byte TransitionIndex { get; set; }

		[Networked]
		public Single Weight { get; set; }

		[Networked]
		public Single NormalizedTime { get; set; }

		public static StateData Lerp( in StateData a, in StateData b, Single t )
		{
			var result = new StateData
			{
				TransitionIndex = a.TransitionIndex,
				NormalizedTime  = b.NormalizedTime >= a.NormalizedTime? Mathf.Lerp( a.NormalizedTime, b.NormalizedTime, t ): b.NormalizedTime,
				Weight          = Mathf.Lerp( a.Weight, b.Weight, t )
			};

			return result;
		}
	}
}