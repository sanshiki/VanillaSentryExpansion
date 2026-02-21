using System;


namespace SummonerExpansionMod.ModUtils
{
    public class NonUniformFloatIntPacker
    {
        private readonly int[] bits;
        private readonly int[] shifts;
        private readonly uint[] masks;
        private readonly int fieldCount;

        public int FieldCount => fieldCount;

        /// <summary>
        /// 构造：传入每个字段的最大值（自动计算最少bit）
        /// 例：new NonUniformFloatIntPacker(100, 3, 1, 500)
        /// </summary>
        public NonUniformFloatIntPacker(params int[] maxValues)
        {
            if (maxValues == null || maxValues.Length == 0)
                throw new ArgumentException("Must provide at least one field.");

            fieldCount = maxValues.Length;
            bits = new int[fieldCount];
            shifts = new int[fieldCount];
            masks = new uint[fieldCount];

            int totalBits = 0;

            for (int i = 0; i < fieldCount; i++)
            {
                if (maxValues[i] < 0)
                    throw new ArgumentException($"Max value at index {i} cannot be negative.");

                bits[i] = RequiredBits(maxValues[i]);
                shifts[i] = totalBits;

                if (totalBits + bits[i] > 32)
                    throw new InvalidOperationException(
                        $"Total bits exceed 32. Cannot pack {fieldCount} fields into one float.");

                masks[i] = (uint)((1UL << bits[i]) - 1);
                totalBits += bits[i];
            }
        }

        /// <summary>
        /// 编码多个int到一个float
        /// </summary>
        public float Encode(params int[] values)
        {
            if (values.Length != fieldCount)
                throw new ArgumentException($"Expected {fieldCount} values.");

            uint packed = 0;

            for (int i = 0; i < fieldCount; i++)
            {
                int v = values[i];
                if (v < 0 || v > masks[i])
                    throw new ArgumentOutOfRangeException(
                        $"Value at index {i} exceeds max storable value ({masks[i]}).");

                packed |= ((uint)v & masks[i]) << shifts[i];
            }

            return BitConverter.Int32BitsToSingle((int)packed);
        }

        /// <summary>
        /// 解码所有字段
        /// </summary>
        public int[] Decode(float data)
        {
            uint packed = (uint)BitConverter.SingleToInt32Bits(data);
            int[] result = new int[fieldCount];

            for (int i = 0; i < fieldCount; i++)
            {
                result[i] = (int)((packed >> shifts[i]) & masks[i]);
            }

            return result;
        }

        /// <summary>
        /// 获取某个字段（O(1)，无需全解码）
        /// </summary>
        public int Get(float data, int index)
        {
            uint packed = (uint)BitConverter.SingleToInt32Bits(data);
            return (int)((packed >> shifts[index]) & masks[index]);
        }

        /// <summary>
        /// 修改某个字段并返回新的float（不影响其他字段）
        /// </summary>
        public float Set(float data, int index, int value)
        {
            if (value < 0 || value > masks[index])
                throw new ArgumentOutOfRangeException(
                    $"Value exceeds max storable value ({masks[index]}).");

            uint packed = (uint)BitConverter.SingleToInt32Bits(data);

            // 清空旧值
            packed &= ~(masks[index] << shifts[index]);

            // 写入新值
            packed |= ((uint)value & masks[index]) << shifts[index];

            return BitConverter.Int32BitsToSingle((int)packed);
        }

        /// <summary>
        /// 获取某字段最大可存值（用于调试/UI）
        /// </summary>
        public int GetMaxValue(int index) => (int)masks[index];

        private static int RequiredBits(int maxValue)
        {
            if (maxValue == 0) return 1;
            int bits = 0;
            while ((1 << bits) <= maxValue)
                bits++;
            return bits;
        }
    }
}