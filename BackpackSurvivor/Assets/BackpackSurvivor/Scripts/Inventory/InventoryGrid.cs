using System;
using System.Collections.Generic;

namespace BS.Inventory {
    public class InventoryGrid
    {
        public event Action OnChanged;
        //背包数据结构
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

        //确认x,y是否可以放下
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
                        return true;//成功了就保持旋转
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


        //合并纯数据 API

        //先判断
        public bool CanMerge(Item source, Item target)
        {
            if(source == null || target == null) return false;
            if(source == target) return false;//同一个实例不行
            if(source.Id != target.Id) return false;//id不同不行
            if(source.Level != target.Level) return false;//等级不同不行
            if(target.Level>=target.MaxLevel) return false;//不超过最大等级

            return true;
        }

        public bool TryMerge(Item source, Item target)
        {
            if(!CanMerge(source, target)) return false;//不能合并直接过
            target.IncreaseLevel();
            Remove(source);
            return true;
        }

        //扫描邻接格子匹配邻规则着返回邻接效果
        public List<AdjacencyEffect> ScanAdjacency(IReadOnlyList<AdjacencyRule> rules)
        {
            List<AdjacencyEffect> effects = new List<AdjacencyEffect>();
            HashSet<string> triggeredKeys = new HashSet<string>();//用于邻接效果去重
            if (rules == null || rules.Count ==0) return effects;

            for(int y = 0; y < Height; y++)
            {
                for(int x = 0; x < Width; x++)
                {
                    Item item = cells[x, y];
                    if (item == null) continue;
                    //先检查右边是否有可行的效果
                    TryMatchNeighbor(x, y, x + 1, y,
                        ConnectableSides.Right, ConnectableSides.Left,
                        rules, effects, triggeredKeys);
                    //再检查下边
                    TryMatchNeighbor(x, y, x , y + 1,
                        ConnectableSides.Down, ConnectableSides.Up,
                        rules, effects, triggeredKeys);
                }
            }

            return effects;
        }


        private void TryMatchNeighbor(int x, int y, int nx, int ny,
            ConnectableSides sideA,//当前item的右边或下边
            ConnectableSides sideB,//将要匹配的邻居的左边或上边
            IReadOnlyList<AdjacencyRule> rules,//开始尝试匹配规则
            List<AdjacencyEffect> effects,//将匹配成功的效果保存
            HashSet<string> triggeredKeys)
        {
            Item itemA = GetItemAt(x, y);
            Item itemB = GetItemAt(nx, ny);

            // 1. 基础守卫
            if (itemA == null || itemB == null) return;
            if (itemA == itemB) return;

            // 2. 接口边检查：不是四边万能
            ConnectableSides sidesA = itemA.GetWorldConnectableSides();
            ConnectableSides sidesB = itemB.GetWorldConnectableSides();
            //检查当前item与将要匹配的邻居是否具备左或者右，上或者下的接口
            if ((sidesA & sideA) == 0) return;
            if ((sidesB & sideB) == 0) return;

            //到这则说明存在所需接口

            // 3. 遍历规则表
            foreach (AdjacencyRule rule in rules)
            {
                if (rule == null) continue;

                if (rule.TagA != itemA.Tag) continue;
                if (rule.SideA != sideA) continue;
                if (rule.TagB != itemB.Tag) continue;
                if (rule.SideB != sideB) continue;

                // 4. 生成去重 key
                // 用 itemA / itemB 的 hash 排序，保证同一对物品只触发一次
                // 再拼 rule.EffectId
                int hashA = itemA.GetHashCode();
                int hashB = itemB.GetHashCode();
                int small = Math.Min(hashA, hashB);
                int large = Math.Max(hashA, hashB);
                string key = small + "_" + large + "_" + rule.EffectId;

                // 5. 如果 key 已存在，continue
                // 否则加入 triggeredKeys
                if (triggeredKeys.Contains(key)) continue;
                triggeredKeys.Add(key);

                // 6. new AdjacencyEffect(...) 加入 effects
                AdjacencyEffect effct = new AdjacencyEffect(rule.EffectId,
                    itemA,sideA,itemB,sideB);
                effects.Add(effct);
            }
        }

        
    }
}
