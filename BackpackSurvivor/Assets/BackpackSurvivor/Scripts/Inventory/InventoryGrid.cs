using System;

namespace BS.Inventory {
    public class InventoryGrid
    {
        public event Action OnChanged;

        public int Width { get; }
        public int Height { get; }
        private readonly Item[,] cells;// 二维数组，注意逗号语法

        public Item this[int x, int y] => GetItemAt(x, y);
        //这里只用this直接指的是这个InventoryGrid背包网格，后续可以直接使用grid[x,y]读取cells[x,y]的内容了

        public InventoryGrid(int width, int height)
        {
            //初始化数组 
            Width = width;
            Height = height;
            cells = new Item[width, height];
            //cells[x, y]，x 在前
        }

        //基础API
        public bool CanPlaceAt(int x, int y, Item item)
        {
            if (item == null) return false;
            if (x < 0 || y < 0 || x + item.Width > Width || y + item.Height > Height)
                return false;
            for(int i = 0; i < item.Width; i++)
            {
                for(int j = 0; j < item.Height; j++)
                {
                    if (cells[x+i,y+j] != null) return false;
                }
            }
            return true;
        }

        public bool Place(int x, int y, Item item)
        {
            //如果存在同一个实例就不允许再放
            if (Contains(item)) return false;
            //如果这个位置不可以放就返回false
            if (!CanPlaceAt(x,y,item)) return false;
            for (int i = 0; i < item.Width; i++)
            {
                for (int j = 0; j < item.Height; j++)
                {
                    cells[x + i, y + j] = item;
                }
            }
            OnChanged?.Invoke();
            return true;
        }

        public void Remove(Item item)
        {
            if(item == null) return;
            bool isRemove = false;
            for(int i = 0; i < Width; i++)
            {
                for(int j = 0; j < Height; j++)
                {
                    if(cells[i, j] == item)
                    {
                        cells[i, j] = null;
                        isRemove = true;
                    }
                }
            }
            if(isRemove) OnChanged?.Invoke();
        }

        public bool Contains(Item item)
        {
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    if (cells[i, j] == item)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public Item GetItemAt(int x, int y)
        {
            if(x<0 ||y<0 ||x>=Width ||y>=Height) return null;
            return cells[x,y];
        }

        public bool TryFindFreeArea(Item item, out int x, out int y)
        {
            for(int j = 0; j < Height; j++)
            {
                for(int i = 0; i< Width; i++)
                {
                    if(CanPlaceAt(i,j, item))
                    {
                        x = i;
                        y = j;
                        return true;
                    }
                }
            }
            //反转再遍历一边
            item.Rotate();
            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                {
                    if (CanPlaceAt(i, j, item))
                    {
                        x = i;
                        y = j;
                        return true;
                    }
                }
            }
            //没找到CanPlaceAt
            // 双失败：转回去，物归原样
            item.Rotate();
            x = -1;
            y = -1;
            return false;
        }

        //拖拽
        public bool TryGetAnchor(Item item, out int x, out int y)
        {
            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                {
                    if (cells[i,j] == item)
                    {
                        x = i;
                        y = j;
                        return true;
                    }
                }
            }
            x = -1; y = -1; return false;
        }
    }
}
