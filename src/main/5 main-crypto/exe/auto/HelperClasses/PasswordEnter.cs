// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using cryptoprime;
using maincrypto.keccak;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

public unsafe partial class PasswordEnter: IDisposable
{
    // [{( - скобки убираем, проще запоминать просто слово "скобка"
    // < - меньше и больше легко перепутать, оставляем только один из них
    // ! слишком похож на черту и имеет сложное наименование
    // Ч - похоже на 4. ± - сложное наименование, к тому же, похожее на два занака "+-", идущие подряд
    // "qwertyuiopasdfghjkLzxcvbnm1234567890,.<?;':(+-*/=|&^%$#@ΣΔΨλШЫЭЯ";
    /// <summary>Разрешённые символы для стандартного пароля</summary>
    public static readonly string GrantedSymbols = "qwertyuiopasdfghjkLzxcvbnm1234567890[+*/=&^%$#@";

    protected CascadeSponge_mt_20230930 sponge;
    protected Record passwordArray;
    /// <summary>x - вертикальная координата, как при индексировании матриц.</summary>
    public readonly int x = 16, y = 16;
    public readonly byte regime = 0;

    public PasswordEnter(CascadeSponge_mt_20230930 sponge, byte regime, nint countOfSteps = 0, bool doErrorMessage = false)
    {
        // На всякий случай переоткрываем поток ввода, т.к. он может быть перенаправлен с помощью SetIn для получения конфигурации
        Console.SetIn(new StreamReader(Console.OpenStandardInput()));
        Console.Clear();

        this.sponge   = sponge;
        this.regime   = regime;

        var len = x * y * 2;
        if (len % GrantedSymbols.Length != 0)
        {
            len += GrantedSymbols.Length - (len % GrantedSymbols.Length);
        }

        passwordArray = Keccak_abstract.allocator.AllocMemory(len);
        for (int i = 0; i < len; i++)
            passwordArray[i] = (byte) GrantedSymbols[i % GrantedSymbols.Length];

        var  passwd = stackalloc byte[(int) sponge.maxDataLen];
        nint cur    = 0;
        nint pwdLen = 0;
        try
        {
            do
            {
                sponge.doRandomPermutationForBytes(len, passwordArray, countOfSteps, regime);
                doShowPasswordTable();

                var c1 = readKey();
                if (c1 == -1)
                    break;

                var c2 = readKey();
                if (c2 == -1)
                    break;

                if (c1 == -2 || c2 == -2)
                {
                    if (doErrorMessage)
                    {
                        Console.BackgroundColor = ConsoleColor.Red;
                        Clear("!");
                        Console.ReadLine();
                    }

                    continue;
                }

                // Console.WriteLine(c1);
                // Console.WriteLine(c2);
                Console.WriteLine((char) passwordArray[c2 + c1*x]);
                passwd[cur] = passwordArray[c2 + c1*x];
                cur++;
                pwdLen++;
                if (cur >= sponge.maxDataLen)
                {
                    cur = 0;
                    sponge.step(countOfSteps: countOfSteps, regime: regime, data: passwd, dataLen: cur);
                }
            }
            while (true);

            if (cur > 0)
            {
                sponge.step(countOfSteps: countOfSteps, regime: regime, data: passwd, dataLen: cur);
            }

            // Console.WriteLine("\x1b[0m\x1b[0m");
            Console.ResetColor();
            Clear();
            Console.Clear();

            if (pwdLen < 8)
            {
                Console.WriteLine(L("Password length is too small") + $": {pwdLen}");
                throw new Exception("PasswordEnter: " + L("Password length is too small") + $": {pwdLen}");
            }
        }
        finally
        {
            Dispose();
        }
    }

    public void Clear(string c = " ")
    {
        for (int i = 0; i < stepsH[stepsH.Length-1]+1; i++)
        for (int j = 0; j < stepsV[stepsV.Length-1]+1; j++)
        {
            Console.SetCursorPosition(i, j);
            Console.Write(c);
        }
    }

    public int readKey()
    {
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Spacebar)
            return -1;

        int c1 = (int) key.Key;
        if (c1 >= 48 && c1 <= 57)
            c1 -= 48;
        else
        if (c1 >= 65 && c1 <= 70)
            c1 += -65 + 10;
        else
            return -2;

        return c1;
    }


    protected int[]    stepsH  = {4, 6, 8, 11, 13, 15, 18, 20, 22, 25, 27, 29, 32, 34, 36, 38};
    protected int[]    stepsV  = {1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15, 17, 18, 19, 20};
    protected string[] numbers = {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F"};
    public void doShowPasswordTable()
    {
        // \x1b[1m\x1b[48;2;255;0;0m\x1b[38;2;0;0;255mTRUECOLOR\x1b[0m\x1b[0m
        Console.WriteLine("\x1b[1m\x1b[48;2;0;0;255m\x1b[38;2;255;0;0m");
        Clear();
        for (int i = 0; i < y; i++)
        {
            Console.SetCursorPosition(stepsH[i], 0);
            Console.Write(numbers[i]);
        }

        for (int i = 0; i < x; i++)
        {
            Console.SetCursorPosition(0, stepsV[i]);
            Console.Write(numbers[i]);
        }

        for (int i = 0; i < x; i++)
        for (int j = 0; j < y; j++)
        {
            Console.SetCursorPosition(stepsH[i], stepsV[j]);
            Console.Write((char) passwordArray.array[i + j*x]);
        }
    }

    void IDisposable.Dispose()
    {
        Dispose();
    }

    ~PasswordEnter()
    {
        Dispose(true);
    }

    public bool isDisposed = false;
    public virtual void Dispose(bool fromDestructor = false)
    {
        if (isDisposed)
            return;

        TryToDispose(passwordArray);
        isDisposed = true;

        if (fromDestructor)
            BytesBuilderForPointers.Record.errorsInDispose = true;
    }
}
