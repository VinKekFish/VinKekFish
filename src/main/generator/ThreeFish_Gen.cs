using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cryptoprime;
using DriverForTestsLib;

namespace CodeGenerator
{
    class ThreeFish_Gen: BaseCSharpCodeGenerator
    {
        public ThreeFish_Gen(string FileName = "./Threefish_Static_Generated.cs"): base(FileName, "", "System")
        {
            Add("// Only encrypt and only for 1024 threefish (useful for OFB or CFB modes)");
            // Add("// Vinogradov S.V. Generated at " + HelperDateClass.DateToDateString(DateTime.Now));
            Add("// Vinogradov S.V. Generated at " + DateTime.Now.Year.ToString("D4") + " year");
            Add("namespace CodeGenerated.Cryptoprimes");
            addBlock();

            this.addClassHeader("public static unsafe", "Threefish_Static_Generated");

            AddBytesToULongConvertFunctions();
            AddFuncThreefish1024_step();

            this.endBlock();
            this.EndGeneration();
            this.Save();
        }

        private void AddFuncThreefish1024_step()
        {
            Add("/// <summary>Step for Threefish1024. DANGER! Do not use directly. See Threefish1024 for prepare</summary>");
            Add("/// <param name=\"key\">Key for cipher (128 bytes)</param>");
            Add("/// <param name=\"tweak\">Tweak for cipher. DANGER! Tweak is a 8*3 bytes, not 8*2!!! (third value is a tweak[0] ^ tweak[1])</param>");
            Add("/// <param name=\"text\">Open text for cipher</param>");

            addFuncHeader("public static", "void", "Threefish1024_step", "ulong * key, ulong * tweak, ulong * text");

            var correspondenceTable = new byte[threefish_slowly.Nw];
            for (byte i = 0; i < correspondenceTable.Length; i++)
            {
                correspondenceTable[i] = i;
            }

            // Компилятор вычисляет адреса переменных в массивах ulong аж через умножение
            // Почему - не знаю. Но это очень долго. Приходится назначать алиасы
            Add("// Aliases");
            for (int i = 0; i <= threefish_slowly.Nw; i++)
            {
                Add($"ref ulong key{i:D2}   = ref key  [{i:D2}];");
                Add($"ref ulong text{i:D2}  = ref text [{i:D2}];");
                if (i <= 2)
                    Add($"ref ulong tweak{i:D2} = ref tweak[{i:D2}];");
            }

            // round = d
            for (int round = 0; round < 80; round++)
            {
                Add("");
                Add("// round " + round.ToString("D2"));

                var s = round >> 2;
                var max = (round & 3) == 0 ? 6 : 8;
                for (int j = 0; j < max; j++)
                {
                    var i1 = correspondenceTable[2 * j + 0];
                    var i2 = correspondenceTable[2 * j + 1];

                    if ((round & 3) == 0)
                    {
                        var index = s + 2*j;

                        // Осуществляем операцию mod (Nw + 1)
                        while (index > threefish_slowly.Nw)
                            index -= threefish_slowly.Nw + 1;

                        var sk1 = index;

                        index = s + 2*j + 1;

                        // Осуществляем операцию mod (Nw + 1)
                        while (index > threefish_slowly.Nw)
                            index -= threefish_slowly.Nw + 1;

                        var sk2 = index;

                        AddMixTemplate($"text{i1:D2}", $"text{i2:D2}", threefish_slowly.RC[round & 0x07, j].ToString("D2"), $"key{sk1:D2}", $"key{sk2:D2}");
                    }
                    else
                    {
                        AddMixTemplate($"text{i1:D2}", $"text{i2:D2}", threefish_slowly.RC[round & 0x07, j].ToString("D2"));
                    }
                }

                if (max == 6)
                {
                    var i = (threefish_slowly.Nw - 4);
                    var index = s + i;

                    // Осуществляем операцию mod (Nw + 1)
                    while (index > threefish_slowly.Nw)
                        index -= threefish_slowly.Nw + 1;

                    var subkeyL = $"key{index:D2}";

                    i = (threefish_slowly.Nw - 3);
                    index = s + i;

                    // Осуществляем операцию mod (Nw + 1)
                    while (index > threefish_slowly.Nw)
                        index -= threefish_slowly.Nw + 1;

                    var subkey = $"key{index:D2}";
                    int s3 = s % 3;
                    var sb2 = $"tweak{s3:D2}";

                    var i1 = correspondenceTable[i - 1];
                    var i2 = correspondenceTable[i + 0];
                    AddMixTemplate($"text{i1:D2}", $"text{i2:D2}", threefish_slowly.RC[round & 0x07, i >> 1].ToString("D2"), subkeyL, subkey);

                    i = (threefish_slowly.Nw - 2);
                    index = s + i;
                    // Осуществляем операцию mod (Nw + 1)
                    while (index > threefish_slowly.Nw)
                        index -= threefish_slowly.Nw + 1;

                    subkeyL = $"key{index:D2}";

                    i = (threefish_slowly.Nw - 1);
                    index = s + i;

                    // Осуществляем операцию mod (Nw + 1)
                    while (index > threefish_slowly.Nw)
                        index -= threefish_slowly.Nw + 1;

                    subkey = $"key{index:D2}";
                    s3 = (s + 1) % 3;
                    sb2 = $"tweak{s3:D2}";

                    i1 = correspondenceTable[i - 1];
                    i2 = correspondenceTable[i + 0];
                    AddMixTemplate($"text{i1:D2}", $"text{i2:D2}", threefish_slowly.RC[round & 0x07, i >> 1].ToString("D2"), $"{subkeyL:D2} + {sb2:D2}", $"{subkey:D2} + {s:D2}");
                }

                for (byte i = 0; i < correspondenceTable.Length; i++)
                {
                    correspondenceTable[i] = threefish_slowly.Pi[correspondenceTable[i]];
                }
            }

            Add("");
            Add("// Final");
            for (int i = 0; i <= 12; i++)
            {
                Add($"text{i:D2} += key{(i + 3):D2};");
            }

            Add($"text13 += key16 + tweak02;");
            Add($"text14 += key00 + tweak00;");
            Add($"text15 += key01 + 20;");


            endBlock();
        }

        private void AddMixTemplate(string a, string b, string r, string? k1 = null, string? k2 = null)
        {
            Add($"// Mix {a} {b} {r}");
            if (k1 == null && k2 == null)
            {
                Add($"{a} += {b};");
                Add($"{b} = {b} << {r} | {b} >> (64-{r});");
                Add($"{b} ^= {a};");
            }
            else
            {
                // Здесь кроме mix добавляются подключи
                if (k2 != null)
                    Add($"{b} += {k2};");

                if (k1 != null)
                    Add($"{a} += {b} + {k1};");
                else
                    Add($"{a} += {b};");

                Add($"{b} = {b} << {r} | {b} >> (64-{r});");
                Add($"{b} ^= {a};");
            }
        }

        private void AddBytesToULongConvertFunctions()
        {
            addFuncHeader("public static", "void", "BytesToUlong_128b", "byte * b, ulong * result");
            Add("ulong * br = (ulong *) b;");
            for (int i = 0; i < cryptoprime.threefish_slowly.Nw; i++)
                Add($"result[{i}] = br[{i}];");
            endBlock();

            addFuncHeader("public static", "void", "UlongToBytes_128b", "ulong * u, byte * result");
            Add("ulong * r = (ulong *) result;");
            for (int i = 0; i < cryptoprime.threefish_slowly.Nw; i++)
                Add($"r[{i}] = u[{i}];");
            endBlock();
        }
    }
}
