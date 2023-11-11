using System.Collections.Generic;
using Unity.Mathematics;

namespace UNTP
{
    public class NetworkWalker : NetworkEnemy, IWalker
    {
        public List<int3> path { get; set; } = new();
    }
}
