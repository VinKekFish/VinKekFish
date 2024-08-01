using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cryptoprime;
using DriverForTestsLib;

namespace CodeGenerator
{
    class ThreeFish_Gen2: BaseCSharpCodeGenerator
    {
        public ThreeFish_Gen2(string FileName = "./Threefish_Static_Generated2.cs"): base(FileName, "", "System")
        {
            Add("// Only encrypt and only for 1024 threefish (useful for OFB or CFB modes)");
            // Add("// Vinogradov S.V. Generated at " + HelperDateClass.DateToDateString(DateTime.Now));
            Add("// Vinogradov S.V. Generated at 2020-" + DateTime.Now.Year.ToString("D4") + " years");
            Add("namespace CodeGenerated.Cryptoprimes");
            AddBlock();

            this.AddClassHeader("public static unsafe", "Threefish_Static_Generated2");

            // AddBytesToULongConvertFunctions();
            AddFuncThreefish1024_step();

            this.EndBlock();
            this.EndGeneration();
            this.Save();
        }

        private void AddFuncThreefish1024_step()
        {
            /*string gText(int index) =>  $"*((ulong *)(text  + {index*8}))";
            string gKey (int index) =>  $"*((ulong *)(key   + {index*8}))";
            string gTw  (int index) =>  $"*((ulong *)(tweak + {index*8}))";*/

            string gText(int index) =>  $"text{index:D2}";
            string gKey (int index) =>  $"key{index:D2}";
            string gTw  (int index) =>  $"tweak{index:D2}";


            Add("/// <summary>Step for Threefish1024. DANGER! Tweak contain 3 elements of ulong, not 2!!! (third value is a tweak[0] ^ tweak[1])</summary>");
            Add("/// <param name=\"key\">Key for cipher (128 bytes)</param>");
            Add("/// <param name=\"tweak\">Tweak for cipher. DANGER! Tweak is a 8*3 bytes, not 8*2!!! (third value is a tweak[0] ^ tweak[1])</param>");
            Add("/// <param name=\"text\">Open text for cipher</param>");

            AddFuncHeader("public static", "void", "Threefish1024_step", "byte * key, byte * tweak, byte * text");

            var correspondenceTable = new byte[Threefish_slowly.Nw];
            for (byte i = 0; i < correspondenceTable.Length; i++)
            {
                correspondenceTable[i] = i;
            }

            // Компилятор вычисляет адреса переменных в массивах ulong аж через умножение
            // Почему - не знаю. Но это очень долго. Приходится назначать алиасы
            Add("// Aliases");
            for (int i = 0; i <= Threefish_slowly.Nw; i++)
            {
                Add($"ref ulong key{i:D2}   = ref ((ulong *)key)  [{i:D2}];");
                Add($"ref ulong text{i:D2}  = ref ((ulong *)text) [{i:D2}];");
                if (i <= 2)
                    Add($"ref ulong tweak{i:D2} = ref ((ulong *)tweak)[{i:D2}];");
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
                        while (index > Threefish_slowly.Nw)
                            index -= Threefish_slowly.Nw + 1;

                        var sk1 = index;

                        index = s + 2*j + 1;

                        // Осуществляем операцию mod (Nw + 1)
                        while (index > Threefish_slowly.Nw)
                            index -= Threefish_slowly.Nw + 1;

                        var sk2 = index;

                        AddMixTemplate(gText(i1), gText(i2), Threefish_slowly.RC[round & 0x07, j].ToString("D2"), gKey(sk1), gKey(sk2));
                    }
                    else
                    {
                        AddMixTemplate(gText(i1), gText(i2), Threefish_slowly.RC[round & 0x07, j].ToString("D2"));
                    }
                }

                if (max == 6)
                {
                    var i = (Threefish_slowly.Nw - 4);
                    var index = s + i;

                    // Осуществляем операцию mod (Nw + 1)
                    while (index > Threefish_slowly.Nw)
                        index -= Threefish_slowly.Nw + 1;

                    var subkeyL = gKey(index);

                    i = (Threefish_slowly.Nw - 3);
                    index = s + i;

                    // Осуществляем операцию mod (Nw + 1)
                    while (index > Threefish_slowly.Nw)
                        index -= Threefish_slowly.Nw + 1;

                    var subkey = gKey(index);
                    int s3; // = s % 3;
                    string sb2; // = gTw(s3);

                    var i1 = correspondenceTable[i - 1];
                    var i2 = correspondenceTable[i + 0];
                    AddMixTemplate(gText(i1), gText(i2), Threefish_slowly.RC[round & 0x07, i >> 1].ToString("D2"), subkeyL, subkey);

                    i = (Threefish_slowly.Nw - 2);
                    index = s + i;
                    // Осуществляем операцию mod (Nw + 1)
                    while (index > Threefish_slowly.Nw)
                        index -= Threefish_slowly.Nw + 1;

                    subkeyL = gKey(index);

                    i = (Threefish_slowly.Nw - 1);
                    index = s + i;

                    // Осуществляем операцию mod (Nw + 1)
                    while (index > Threefish_slowly.Nw)
                        index -= Threefish_slowly.Nw + 1;

                    subkey = gKey(index);
                    s3 = (s + 1) % 3;
                    sb2 = gTw(s3);

                    i1 = correspondenceTable[i - 1];
                    i2 = correspondenceTable[i + 0];
                    AddMixTemplate(gText(i1), gText(i2), Threefish_slowly.RC[round & 0x07, i >> 1].ToString("D2"), $"{subkeyL:D2} + {sb2:D2}", $"{subkey:D2} + {s:D2}");
                }

                for (byte i = 0; i < correspondenceTable.Length; i++)
                {
                    correspondenceTable[i] = Threefish_slowly.Pi[correspondenceTable[i]];
                }
            }

            Add("");
            Add("// Final");
            for (int i = 0; i <= 12; i++)
            {
                Add($"{gText(i)} += {gKey(i + 3)};");
            }
/*
            Add($"text13 += key16 + tweak02;");
            Add($"text14 += key00 + tweak00;");
            Add($"text15 += key01 + 20;");*/
            Add($"{gText(13)} += {gKey(16)} + {gTw(2)};");
            Add($"{gText(14)} += {gKey(0)}  + {gTw(0)};");
            Add($"{gText(15)} += {gKey(1)} + 20;");


            EndBlock();
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
/*
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
        }*/
    }
}
