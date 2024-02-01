// TODO: tests
namespace vinkekfish;

using cryptoprime;
using maincrypto.keccak;
using static cryptoprime.BytesBuilderForPointers;
using System.Threading.Tasks;

using static CascadeSponge_1t_20230905.InputRegime;
using System.Text;
using static VinKekFish_Utils.Utils;
using static CodeGenerated.Cryptoprimes.Threefish_Static_Generated;

// code::docs:rQN6ZzeeepyOpOnTPKAT:
// ::cp:alg:a7L6XjXsuwWGVxwJSN1x.io:20230930

/// <summary>
/// Это однопоточный эталон для тестирования каскадной губки
/// </summary>
public unsafe partial class CascadeSponge_1t_20230905: IDisposable
{
    /// <summary>
    /// Определяет, как именно вводятся данные в губку.
    /// xor - обычный режим ввода
    /// overwrite - режим ввода с перезаписью данных
    /// </summary>
    public enum InputRegime { error = 0, xor = 2, overwrite = 3 };

    /// <summary>Магическое значение, стоящее в конце reverseConnectionData для InputData. Полная длина reverseConnectionData для выделения памяти - ReserveConnectionFullLen</summary>
    public const ulong MagicNumber_ReverseConnectionLink_forInput = 0xc0b175f182b530ec;

    // code::docs:DAHCEPXmAXk11dSEji7s:
    /// <summary>Ввести данные в каскадную губку</summary>
    /// <param name="data">Данные для ввода. Память может быть выделена через stackalloc, если это требуется</param>
    /// <param name="dataLen">Длина данных для ввода. Не более maxDataLen</param>
    /// <param name="regime">Режим ввода (логический параметр, декларируемый схемой шифрования; может быть любым однобайтовым значением)</param>
    /// <param name="reverseConnectionData">Может быть null. Если данные указаны, то они должны быть длиной ReserveConnectionFullLen, из которых ReserveConnectionLen байтов приходятся на данные обратной связи. После этого должно следовать магическое число MagicNumber_ReverseConnectionLink_forInput. Память может быть выделена через stackalloc byte[ReserveConnectionFullLen]</param>
    /// <param name="inputRegime">Режим ввода. xor - обычный режим ввода, overwrite - режим ввода с перезаписью</param>
    protected void InputData(byte * data, nint dataLen, byte regime, byte * reverseConnectionData = null, InputRegime inputRegime = xor)
    {
        ObjectDisposedCheck("CascadeSponge_1t_20230905.InputData");

        if (reverseConnectionData != null)
            CheckMagicNumber(reverseConnectionData, "CascadeSponge_1t_20230905.InputData: magic != MagicNumber_ReverseConnectionLink_forInput");

        if (dataLen > maxDataLen)
            throw new CascadeSpongeException($"dataLen > maxDataLen ({dataLen} > {maxDataLen})");

        // Вычисляем количество данных, которые будут введены в каждую губку
        var Nnf = (double) dataLen / (double) wide;
        var Nn  = (nint) Math.Ceiling(Nnf);
        if (Nn > Wn)
            throw new CascadeSpongeException($"InputData: Nn > Wn ({Nn} > {Wn}). Nn must be <= Wn");

        var input = KeccakPrime.Keccak_Input64_512;
        if (inputRegime == overwrite)
            input = KeccakPrime.Keccak_InputOverwrite64_512;

        var buffer = stackalloc byte[(int) MaxInputForKeccak];
        nint w   = 0;       // Текущий номер губки для работы
        nint cur = 0;       // Текущий указатель на вводимый байт - cur. cur является числом уже введённых байтов
        for (; w < wide; w++)
        {
            // Определяем количество вводимых байтов из-вне
            nint dataLenToInput = 0;
            if (cur < dataLen)
            {
                dataLenToInput = Nn;            // Длина данных для ввода в выбранную губку w
                if (dataLen - cur < Nn)
                    dataLenToInput = dataLen - cur;
            }
            // Вводим данные из-вне в буфер
            if (dataLenToInput > 0)
            BytesBuilder.CopyTo(dataLenToInput, MaxInputForKeccak, data + cur, buffer);

            // Определяем, нужно ли вводить данные из обратной связи
            nint rcd_len = 0;
            if (reverseConnectionData != null)
            {
                // Данные из обратной связи берутся постольку, поскольку не введены внешние данные
                // Их всё равно будет вводится где-то половина или более, т.к. Wn всегда меньше, чем MaxInputForKeccak (32 или менее против 64-х)
                rcd_len = MaxInputForKeccak - dataLenToInput;
                BytesBuilder.CopyTo(rcd_len, MaxInputForKeccak, reverseConnectionData, buffer + dataLenToInput);
                reverseConnectionData += MaxInputForKeccak; // Здесь мы приращаем данные так, как будто ввели полный блок
            }

            // Console.WriteLine(ArrayToHex(buffer, (int) MaxInputForKeccak));

            input(buffer, (byte) (dataLenToInput + rcd_len), getInputLayerS(w), regime);
            cur += dataLenToInput;
        }

        BytesBuilder.ToNull(MaxInputForKeccak, buffer);
    }

    /// <summary>Получает в массив data выход с нижнего слоя каскадной губки. Заполняет массив this.lastOutput пользовательским выводом</summary>
    protected void outputAllData()
    {
        ObjectDisposedCheck("CascadeSponge_1t_20230905.outputAllData");

        CheckMagicNumber(fullOutput, "CascadeSponge_1t_20230905.outputAllData.fullOutput: magic != MagicNumber_ReverseConnectionLink_forInput");
        CheckMagicNumber(  rcOutput, "CascadeSponge_1t_20230905.outputAllData.rcOutput:   magic != MagicNumber_ReverseConnectionLink_forInput");

        var data = fullOutput.array;
        for (nint w = 0; w < wide; w++)
        {
            var S = getOutputLayerS(w);
            KeccakPrime.Keccak_Output_512(data, MaxInputForKeccak, S: S);
            data += MaxInputForKeccak;
        }

        // Транспонируем состояние в data, чтобы перемешать блоки
        transposeOutput(fullOutput);
        // Копируем fullOutput в rcOutput, чтобы использовать fullOutput для дальнейшего заключительного преобразования
        BytesBuilder.CopyTo(ReserveConnectionLen, ReserveConnectionLen, fullOutput, rcOutput);

        // Console.WriteLine();
        // Console.WriteLine("outputAllData: before ThreeFish"); Console.WriteLine(ArrayToHex(fullOutput, ReserveConnectionLen));

        // Выполняем преобразование обратной связи
        doThreeFish(rcOutput, this.threefishCrypto!.array + 0);                           // Обратная связь
        doSubstitution(rcOutput);

        // Console.WriteLine("outputAllData: out after ThreeFish before transpose"); Console.WriteLine(ArrayToHex(fullOutput, ReserveConnectionLen));

        transposeOutput(rcOutput, 128);

        // Console.WriteLine("outputAllData:  rc after ThreeFish +t"); Console.WriteLine(ArrayToHex(  rcOutput, ReserveConnectionLen));
        // Console.WriteLine("outputAllData: out after ThreeFish +t"); Console.WriteLine(ArrayToHex(fullOutput, ReserveConnectionLen));
        // Console.WriteLine();
    }

    /// <summary>Транспонирует (перемешивает) данные в выходном массиве для того, чтобы можно было просто взять эти данные на выход, а остальные отправить в обратную связь уже перемешанными</summary><param name="data">Данные для перемешивания. Длина данных - ReserveConnectionLen</param>
    public void transposeOutput(byte * data, int transposeStep = MaxInputForKeccak)
    {
        var buffer = stackalloc byte[(int)ReserveConnectionLen];
        BytesBuilder.CopyTo(ReserveConnectionLen, ReserveConnectionLen, data, buffer);

        nint j = 0, k = 0;
        for (nint i = 0; i < ReserveConnectionLen; i++)
        {
            data[i] = buffer[j];
            j += transposeStep;
            if (j >= ReserveConnectionLen)
                j = ++k;
        }

        if (transposeStep == MaxInputForKeccak)
        if (k != MaxInputForKeccak)
            throw new CascadeSpongeException($"CascadeSponge_1t_20230905.transposeOutput: k != MaxInputForKeccak ({k} != {MaxInputForKeccak})");

        BytesBuilder.ToNull(ReserveConnectionLen, buffer);
    }

    protected void doSubstitution(Record data)
    {
        CheckMagicNumber(data, "CascadeSponge_1t_20230905.doSubstitution: magic != MagicNumber_ReverseConnectionLink_forInput");

        var len = ReserveConnectionLen >> 1;
        var dt  = (ushort *) data.array;
        var sb  = (ushort*) SubstitutionTable;
        for (nint i = 0; i < len; i += 8)
        {
            dt[i+0] = sb[ dt[i+0] ];
            dt[i+1] = sb[ dt[i+1] ];
            dt[i+2] = sb[ dt[i+2] ];
            dt[i+3] = sb[ dt[i+3] ];
            dt[i+4] = sb[ dt[i+4] ];
            dt[i+5] = sb[ dt[i+5] ];
            dt[i+6] = sb[ dt[i+6] ];
            dt[i+7] = sb[ dt[i+7] ];
        }
    }

    /// <summary>Сделать поблочное преобразование ThreeFish1024 для массива обратной связи (указан в параметре data)</summary>
    /// <param name="data">Массив для шифрования, длиной ReserveConnectionLen. Заканчивается магическим числом</param>
    /// <param name="threefishCrypto">Массив ключей и твиков. Первыми в this.threefishCrypto идут ключи для обратной связи.</param>
    protected void doThreeFish(byte * data, byte * threefishCrypto)
    {
        CheckMagicNumber(data, "CascadeSponge_1t_20230905.doThreeFish: magic != MagicNumber_ReverseConnectionLink_forInput");

        ulong tw;

        var keys   = (ulong *)  threefishCrypto;
        var tweaks = (ulong *) (threefishCrypto + 192);
        var dt     = (ulong *) data;
        for (nint i = 0; i < countOfThreeFish_RC; i++)
        {
            Threefish1024_step(keys, tweaks, dt);
            tw = tweaks[0] + CounterIncrement;
            // Если произошло переполнение
            if (tw < tweaks[0])
                tweaks[1]++;

            tweaks[0]  = tw;
            tweaks[2]  = tweaks[0] ^ tweaks[1];

            keys   += threefish_slowly.Nw*2;        // Шаг следования ключей - 256 байтов
            tweaks += threefish_slowly.Nw*2;
            dt     += threefish_slowly.Nw;          // Шаг следования блоков данных - 128 байтов
        }
    }

    /// <summary>Проверяет наличие в массиве, размером ReserveConnectionFullLen (ReserveConnectionLen + 8 байтов магического числа), наличие верного магического числа MagicNumber_ReverseConnectionLink_forInput. Если числа нет, выдаёт исключение CascaseSpongeException(message)</summary><param name="data">Массив для проверки</param><param name="message">Сообщение для выдачи исключения</param>
    public void CheckMagicNumber(byte* data, string message, nint arrayLen = 0)
    {
        if (arrayLen == 0)
            arrayLen = ReserveConnectionLen;

        BytesBuilder.BytesToULong(out ulong magic, data + arrayLen, 8);
        if (magic != MagicNumber_ReverseConnectionLink_forInput)
            throw new CascadeSpongeException($"{message} ({magic:X})");
    }
}
