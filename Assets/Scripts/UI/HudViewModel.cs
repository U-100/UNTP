using System.Collections.Generic;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace UNTP
{
	public class HudViewModel : IHudViewModel
	{
		private readonly IGameplay _gameplay;

		private readonly List<HudIndicatorData> _indicators = new();
		
		public HudViewModel(IGameplay gameplay)
		{
			this._gameplay = gameplay;
		}

		public void Dispose() { }
		
		public ConstructionState constructionState => this._gameplay?.gameBoard.players.localPlayer.constructionState ?? ConstructionState.NoConstruction;
		
		public IReadOnlyList<HudIndicatorData> CalculateIndicators()
		{
			if (this._gameplay is not null)
			{
				int requiredIndicatorsCount = 0;

				int playersCount = this._gameplay.gameBoard.players.count;
				for (int playerIndex = 0; playerIndex < playersCount; ++playerIndex)
				{
					if (TryGetAllyIndicator(this._gameplay.gameBoard.players[playerIndex], out HudIndicatorData indicator))
					{
						if (requiredIndicatorsCount < this._indicators.Count)
							this._indicators[requiredIndicatorsCount] = indicator;
						else
							this._indicators.Insert(requiredIndicatorsCount, indicator);
						
						++requiredIndicatorsCount;
					}
				}
				
				int enemiesCount = this._gameplay.gameBoard.enemies.count;
				for (int enemyIndex = 0; enemyIndex < enemiesCount; ++enemyIndex)
				{
					if (TryGetEnemyIndicator(this._gameplay.gameBoard.enemies[enemyIndex], out HudIndicatorData indicator))
					{
						if (requiredIndicatorsCount < this._indicators.Count)
							this._indicators[requiredIndicatorsCount] = indicator;
						else
							this._indicators.Insert(requiredIndicatorsCount, indicator);
						
						++requiredIndicatorsCount;
					}
				}
			
				if (this._indicators.Count > requiredIndicatorsCount)
					this._indicators.RemoveRange(requiredIndicatorsCount, this._indicators.Count - requiredIndicatorsCount);
			}
			else
				this._indicators.Clear();
			
			return this._indicators;
		}

		public void SetFireAim(float2 fireAim)
		{
			IPlayerCharacter localPlayerCharacter = this._gameplay?.gameBoard.players.localPlayer?.character;
			if (localPlayerCharacter != null)
				localPlayerCharacter.fireAim = new float3(fireAim.x, 0, -fireAim.y);
		}

		private bool TryGetAllyIndicator(IPlayer player, out HudIndicatorData indicator)
		{
			indicator = new HudIndicatorData();

			if(player.character != null && TryGetIndicatorPositionScreenFactor(player.character.position, out indicator.positionScreenFactor))
			{
				indicator.kind = HudIndicatorKind.Ally;
				return true;
			}

			return false;
		}
		
		private bool TryGetEnemyIndicator(IEnemy enemy, out HudIndicatorData indicator)
		{
			indicator = new HudIndicatorData();

			if (TryGetIndicatorPositionScreenFactor(enemy.position, out indicator.positionScreenFactor))
			{
				indicator.kind = enemy switch
				{
					IStrider => HudIndicatorKind.BigEnemy,
					_ => HudIndicatorKind.SmallEnemy,
				};

				return true;
			}
			
			return false;
		}

		private bool TryGetIndicatorPositionScreenFactor(float3 targetPosition, out float2 positionScreenFactor)
		{
			IPlayerCamera playerCamera = this._gameplay.gameBoard.players.localPlayer.playerCamera;
			if(playerCamera != null)
			{
				float3 cameraPosition = playerCamera.position;
				float3 directionToTarget = normalize(targetPosition - cameraPosition);
				float dotx = dot(directionToTarget, playerCamera.right);
				float doty = dot(directionToTarget, playerCamera.up);
				float2 p = new float2(dotx / sqrt(1 - dotx * dotx), doty / sqrt(1 - doty * doty));
				float2 hr = new float2(playerCamera.aspect * tan(radians(playerCamera.fov / 2)), tan(radians(playerCamera.fov / 2)));
				if (any(abs(p) > abs(hr)))
				{
					p = clamp(p, -hr, hr);
					positionScreenFactor = p / (2 * hr) + 0.5f;
					positionScreenFactor.y = 1 - positionScreenFactor.y;
					return true;
				}
			}
			
			positionScreenFactor = default;
			return false;
		}
	}
}
