namespace test_dev;

using VinKekFish_Utils;
using cryptoprime;
using Record = cryptoprime.BytesBuilderForPointers.Record;

unsafe class Program
{
    static void Main(string[] args)
    {
        var e = new byte[256];  // Эталонный массив

        var er = Record.getRecordFromBytesArray(e);

        for (int i = 0; i < e.Length; i++)
            e[i] = (byte) i;

        for (int L = 1; L < e.Length << 1; L++)
        {
            /*if (L == e.Length)
                continue;*/

            var b  = new byte[L];
            var br = Record.getRecordFromBytesArray(b);
            for (int i = 0; i < L && i < e.Length; i++)
            {
                b[i] = e[i];
            }

            var dt  = DateTime.Now.Ticks;
            // Utils.SecureCompareFast(er, br);
            Utils.SecureCompare(er, br);
            // SecureCompareBad(er, br, 0, 0, er.len, br.len);
            var dt2 = DateTime.Now.Ticks;
            var sp  = dt2 - dt;
            Console.WriteLine(L.ToString("D2") + ":\t" + sp.ToString() + "\t\t" + (sp*100.0/L).ToString("F2"));
        }
    }

    public static bool SecureCompareBad(Record r1, Record r2, nint start1, nint start2, nint len1, nint len2)
    {
        var len = len1;
        /*if (len > len2)
            len = len2;*/
        if (len != len2)
            return false;

        byte * r1a = r1.array + start1, r2a = r2.array + start2, End1 = r1a + len;

        byte V = 0;
        for (; r1a < End1; r1a++, r2a++)
        {
            V |= (byte) (*r1a ^ *r2a);
        }

        return V == 0 && len1 == len2;
    }
}
