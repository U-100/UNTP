using System;
using System.Threading.Tasks;

namespace UNTP
{
    public interface IGameplay : IDisposable
    {
        IGameBoard gameBoard { get; }

        Task Start();
        void Play();
        void Stop();
        void Pause();
        void Resume();
    }
}
