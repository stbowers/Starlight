using System;

namespace StarlightGame.GameCore.Ships
{
    public interface IShip
    {
        Empire Owner { get; }
    }
}