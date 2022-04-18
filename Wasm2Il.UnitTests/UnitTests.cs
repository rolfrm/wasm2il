namespace Wasm2Il
{

    class UnitTests
    {
        public static void BuildInstructionEnum()
        {
            var instructionLookup = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines("instruction.list"))
            {
                var l = line.Trim();
                if (string.IsNullOrEmpty(l)) continue;
                if (l[0] == '#') continue;
                var s = l.Split(" ");
                instructionLookup[s[0]] = s[1];
            }

            string code = "public enum Instruction {\n";
            code += string.Join(",\n", instructionLookup.Select(x => x.Key + " = " + x.Value));
            code += "\n}\n";
            File.WriteAllText("instructions.cs", code);
        }

        static string stringDup(string str, int count)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
                sb.Append(str);
            return sb.ToString();
        }

        public static void TestReadWrite()
        {

            var memstr = new MemoryStream();
            var writer = new Wasm.BinWriter(memstr);
            long[] longs = {0xA, 0xAB, 0xABCD, 0xABCDEF, -0XAABBCCDDAABBCC, 0XAABBCCDDAABBCC};
            ulong[] ulongs = {0xA, 0xAB, 0xABCD, 0xABCDEF, 0XAABBCCDDAABBCC, 0x123456789};
            byte[] bytes = {1, 5, 8, 10, 200};
            short[] shorts = {1, 10, 100, 1000, 10000, -30000};
            ushort[] ushorts = {1, 10, 100, 1000, 10000, 30000};
            int[] ints = {1, 10, 100, 1000, 10000, 30000, 3218421, 321830921, 3218832, -49365543, -8321, -342543};
            float[] floats =
                {1.0f, -1.0f, float.MinValue, float.MaxValue, float.NegativeInfinity, float.PositiveInfinity};
            double[] doubles =
            {
                1.0, 0.0, double.NegativeInfinity, double.PositiveInfinity, double.MaxValue, double.MinValue, 100000,
                -100000
            };
            string[] strs =
            {
                "", "abcde", "abcde jdowahjd hwal hd uwh duiw ahl", "🎂🎂🎂🎂🎂", stringDup("🎂", 1024),
                stringDup("🎂§", 10 * 1024)
            };
            foreach (var v in longs)
                writer.WriteLeb(v);
            foreach (var v in ulongs)
                writer.WriteLeb(v);
            foreach (var v in longs)
                writer.Write(v);
            foreach (var v in ulongs)
                writer.Write(v);
            foreach (var v in bytes)
                writer.Write(v);
            foreach (var v in shorts)
                writer.Write(v);
            foreach (var v in ushorts)
                writer.Write(v);
            foreach (var v in ints)
                writer.Write(v);
            foreach (var v in floats)
                writer.Write(v);
            foreach (var v in doubles)
                writer.Write(v);
            foreach (var v in strs)
                writer.WriteStr0(v);
            foreach (var v in strs)
                writer.WriteStrN(v);

            memstr.Seek(0, SeekOrigin.Begin);
            var reader = new BinReader(memstr);
            foreach (var v in longs)
                Assert.AreEqual(v, reader.ReadI64Leb());
            foreach (var v in ulongs)
                Assert.AreEqual(v, reader.ReadU64Leb());
            foreach (var v in longs)
                Assert.AreEqual(v, reader.ReadI64());
            foreach (var v in ulongs)
                Assert.AreEqual(v, reader.ReadU64());
            foreach (var v in bytes)
                Assert.AreEqual(v, reader.ReadU8());
            foreach (var v in shorts)
                Assert.AreEqual(v, reader.ReadI16());
            foreach (var v in ushorts)
                Assert.AreEqual(v, reader.ReadU16());
            foreach (var v in ints)
                Assert.AreEqual(v, reader.ReadI32());
            foreach (var v in floats)
                Assert.AreEqual(v, reader.ReadF32());
            foreach (var v in doubles)
                Assert.AreEqual(v, reader.ReadF64());
            foreach (var v in strs)
                Assert.AreEqual(v, reader.ReadStr0());
            foreach (var v in strs)
                Assert.AreEqual(v, reader.ReadStrN());
        }

    }
}