using System;
using System.Runtime.Serialization;

namespace StarlightGame.GameCore.Ships
{
    public interface IShip : ISerializable
    {
        string Name { get; }
        string Type { get; }

        Empire GetOwner();
        void SetOwner(Empire empire);
    }
}