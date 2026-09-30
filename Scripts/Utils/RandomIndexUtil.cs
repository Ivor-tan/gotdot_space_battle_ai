using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class RandomIndexUtil
{
    private struct WeightCache
    {
        public double[] Weights;
        public double TotalWeight;
        public float Strength;
    }

    private static readonly Dictionary<RandomPoolType, WeightCache> _cacheMap = new();

    /// <summary>
    /// 获取权重随机索引：索引越小，概率越大。
    /// </summary>
    /// <param name="count">列表的总长度</param>
    /// <param name="curveStrength">强度：0=等概率, 1=线性衰减, 2+=指数级衰减   CurveStrength 减小到 1.0 甚至 0.5，从而提高高索引（高级/精英）敌人出现的频率</param>
    /// <returns>计算后的随机索引</returns>
    public static int GetWeightedIndex(RandomPoolType poolType, int count, float curveStrength = 1.0f)
    {
        if (count <= 0) return -1;
        if (count == 1) return 0;

        // 检查缓存有效性：不存在、长度改变、或强度改变时刷新
        if (!_cacheMap.ContainsKey(poolType) ||
            _cacheMap[poolType].Weights.Length != count ||
            !Mathf.IsEqualApprox(_cacheMap[poolType].Strength, curveStrength))
        {
            UpdateCache(poolType, count, curveStrength);
        }

        WeightCache cache = _cacheMap[poolType];
        double randomValue = GD.Randf() * cache.TotalWeight;
        double currentSum = 0;

        for (int i = 0; i < count; i++)
        {
            currentSum += cache.Weights[i];
            if (randomValue <= currentSum) return i;
        }

        return 0;
    }

    public static List<int> GetThreeUniqueIndices(int totalCount, float strength = 1.0f)
    {
        HashSet<int> selectedIndices = new HashSet<int>();

        int targetCount = Mathf.Min(3, totalCount);

        while (selectedIndices.Count < targetCount)
        {
            int idx = GetWeightedIndex(RandomPoolType.EnhanceFunction, totalCount, strength);
            selectedIndices.Add(idx);
        }

        return selectedIndices.ToList();
    }
    private static void UpdateCache(RandomPoolType poolType, int count, float strength)
    {
        double[] weights = new double[count];
        double total = 0;

        for (int i = 0; i < count; i++)
        {
            // 核心公式：(count - i) 的 strength 次方
            double w = Math.Pow(count - i, strength);
            weights[i] = w;
            total += w;
        }

        _cacheMap[poolType] = new WeightCache
        {
            Weights = weights,
            TotalWeight = total,
            Strength = strength
        };
    }
}