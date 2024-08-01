#pragma warning disable CA1821  // Пустые завершающие методы

using System.Collections.Concurrent;
using DriverForTestsLib;
using cryptoprime;
using cryptoprime_tests;
using System.Runtime;

namespace tests;
class Program
{
    static void Main(string[] args)
    {
        // Эти исключения иногда мешают ловить ошибки, сбрасывая весь DriverForTests
        // Вместо исключений предусмотрена проверка после тестов значения errorsInDispose
        BytesBuilderForPointers.Record.doExceptionOnDisposeTwiced       = false;
        BytesBuilderForPointers.Record.doExceptionOnDisposeInDestructor = false;

        GCSettings.LatencyMode = GCLatencyMode.Batch;

        // Изменяем локаль на английскую, чтобы она такая была всегда: в тестах есть сообщения, которые сравниваются с эталоном на английском
        File.WriteAllText("REDEFINE.loc", "en");

        var driver = new DriverForTests();

        var tc = new MainTestConstructor();

        var parser = new TestConditionParser(String.Join(',', args), true);
        tc.conditions = parser.resultCondition;

        driver.ExecuteTests
        (
            new TestConstructor[] { tc },
            new DriverForTests.ExecuteTestsOptions
            {
                SleepInMs_ForFirstOutput = 10*1000,
                LogNamesOfTests          = 3,
                DoKeepLogFile            = false // || true
            }
        );

        // Проверяем, что все деструкторы Record отработали без ошибок
        if (BytesBuilderForPointers.Record.ErrorsInDispose)
        {
            Console.WriteLine("!!! ERROR !!!");
            Console.WriteLine("BytesBuilderForPointers.Record.errorsInDispose is true");
            foreach (var error in BytesBuilderForPointers.Record.errorsInDispose_List)
                Console.WriteLine(error);
        }

/*
        VinKekFish_Utils.Memory.alloc(1);
        VinKekFish_Utils.Memory.alloc(1);
        VinKekFish_Utils.Memory.DeallocateAtBreakage();
*/
        VinKekFish_Utils.Memory.DeallocateAtBreakage();
/*
        // Принудительно пытаемся вызвать деструкторы более агрессивно, чем делали это ранее
        for (int i = 0; i < byte.MaxValue; i++)
        {
            Empty.@do();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }*/
    }
}

class MainTestConstructor : TestConstructor
{
    public MainTestConstructor()
    {}

    public override void CreateTasksLists(ConcurrentQueue<TestTask> tasks)
    {
        // Вызываем, чтобы загрузилась сборка, где есть свои тесты
        if (Keccak_sha_3_512_test.canCreateFile) {}
        // new Empty();

        // Получаем все задачи, которые могут быть автоматически собраны из данного домена приложения
        var list = this.GetTasksFromAppDomain
        (
            // Этот обработчик срабатывает тогда, когда задача либо неавтоматическая,
            // либо не имеет нужного конструктора
            // Здесь мы также проверяем, что мы не забыли поставить ручные (неавтоматические) задачи
            (Type t, bool notAutomatic) =>
            {
                if (!notAutomatic)
                    Console.Error.WriteLine("Incorrect task: " + t.FullName);
                else
                {
                    // Проверяем ручные задачи, что мы никакую не забыли поставить
                    // Так как все ручные задачи мы уже поставили перед вызовом getTasksFromAppDomain
                    // здесь если задача не зарегистрирована на выполнение, то это значит, что мы её забыли
                    foreach (var task in tasks)
                    {
                        var taskType = task.GetType();
                        if (t == taskType)
                            return;
                    }

                    Console.Error.WriteLine($"A notAutomatic task has been declared, but it is not in the list for execution: {t.FullName}");
                }
            }
        );

        // Ставим эти задачи на выполнение
        TestConstructor.AddTasksForQueue(list, tasks);
    }
}

public class Empty
{
    ~Empty()
    {
        throw new Exception("\n!!!!!!!!!!!!!!\n(class Empty)");
    }

    public static void Do()
    {
        _ = new Empty();
    }
}
