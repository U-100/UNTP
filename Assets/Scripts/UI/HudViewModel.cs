using System.Collections.Generic;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace UNTP
{
	public class HudViewModel : IHudViewModel
	{
		private readonly IGameplay _gameplay;

		private readonly List<HudIndicator> _indicators = new();
		
		public HudViewModel(IGameplay gameplay)
		{
			this._gameplay = gameplay;
		}

		public void Dispose() { }

		public ConstructionState constructionState => this._gameplay?.gameBoard.players.localPlayer.constructionState ?? ConstructionState.NoConstruction;
		
		public IReadOnlyList<HudIndicator> CalculateIndicators()
		{
			if (this._gameplay is not null)
			{
				int enemiesCount = this._gameplay.gameBoard.enemies.count;
				int requiredIndicatorsCount = 0;
				for (int enemyIndex = 0; enemyIndex < enemiesCount; ++enemyIndex)
				{
					IEnemy enemy = this._gameplay.gameBoard.enemies[enemyIndex];

					float2 playerPosition = this._gameplay.gameBoard.players.localPlayer.character.position.xz;
					float2 enemyPosition = enemy.position.xz;
					float2 delta = enemyPosition - playerPosition;

					const int OUT_OF_SIGHT_DISTANCE = 5;
					if (any(abs(delta) > OUT_OF_SIGHT_DISTANCE))
					{
						HudIndicator indicator = new HudIndicator();
						if (abs(delta.x) > abs(delta.y))
						{
							indicator.positionScreenPercent.x = delta.x < 0 ? 0 : 100;
							indicator.positionScreenPercent.y = clamp(50 - delta.y / OUT_OF_SIGHT_DISTANCE * 50, 0, 100);
						}
						else
						{
							indicator.positionScreenPercent.x = clamp(50 + delta.x / OUT_OF_SIGHT_DISTANCE * 50, 0, 100);
							indicator.positionScreenPercent.y = delta.y > 0 ? 0 : 100;
						}

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
	}
}
