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

				// int playersCount = this._gameplay.gameBoard.players.count;
				// for (int playerIndex = 0; playerIndex < playersCount; ++playerIndex)
				// {
				// 	if (TryGetAllyIndicator(this._gameplay.gameBoard.players[playerIndex], out HudIndicatorData indicator))
				// 	{
				// 		if (requiredIndicatorsCount < this._indicators.Count)
				// 			this._indicators[requiredIndicatorsCount] = indicator;
				// 		else
				// 			this._indicators.Insert(requiredIndicatorsCount, indicator);
				// 		
				// 		++requiredIndicatorsCount;
				// 	}
				// }
				
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

		private bool TryGetAllyIndicator(IPlayer player, out HudIndicatorData indicator)
		{
			indicator = new HudIndicatorData();

			if(player.character != null)
			{
				float2 localPlayerPosition = this._gameplay.gameBoard.players.localPlayer.character.position.xz;
				float2 otherPlayerPosition = player.character.position.xz;
				float2 delta = otherPlayerPosition - localPlayerPosition;

				if (TryGetIndicatorPositionScreenFactor(delta, out indicator.positionScreenFactor))
				{
					indicator.kind = HudIndicatorKind.Ally;
					return true;
				}
			}

			return false;
		}
		
		private bool TryGetEnemyIndicator(IEnemy enemy, out HudIndicatorData indicator)
		{
			indicator = new HudIndicatorData();

			float2 localPlayerPosition = this._gameplay.gameBoard.players.localPlayer.character.position.xz;
			float2 enemyPosition = enemy.position.xz;
			float2 delta = enemyPosition - localPlayerPosition;

			if (TryGetIndicatorPositionScreenFactor(delta, out indicator.positionScreenFactor))
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

		private bool TryGetIndicatorPositionScreenFactor(float2 delta, out float2 positionScreenFactor)
		{
			const int OUT_OF_SIGHT_DISTANCE = 5;
			if (any(abs(delta) > OUT_OF_SIGHT_DISTANCE))
			{
				if (abs(delta.x) > abs(delta.y))
				{
					positionScreenFactor.x = delta.x < 0 ? 0 : 1;
					positionScreenFactor.y = clamp(.5f - delta.y / OUT_OF_SIGHT_DISTANCE * .5f, 0, 1);
				}
				else
				{
					positionScreenFactor.x = clamp(.5f + delta.x / OUT_OF_SIGHT_DISTANCE * .5f, 0, 1);
					positionScreenFactor.y = delta.y > 0 ? 0 : 1;
				}
				return true;
			}

			positionScreenFactor = default;
			return false;
		}
	}
}
