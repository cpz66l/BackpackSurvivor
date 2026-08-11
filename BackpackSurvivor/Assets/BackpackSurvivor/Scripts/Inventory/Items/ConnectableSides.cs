using System;

namespace BS.Inventory
{
    [Flags]
    public enum ConnectableSides
    {
        None = 0,       //0000没有任何边

        Up = 1 << 0,    //0001上边
        Right = 1 << 1, //0010右边
        Down = 1 << 2,  //0100下边
        Left = 1 << 3,  //1000左边

        All = Up | Right | Down | Left //全部四条边
    }
}