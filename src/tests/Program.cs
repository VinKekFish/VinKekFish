using System.Collections.Concurrent;
using DriverForTestsLib;
using cryptoprime;
using cryptoprime_tests;

namespace tests;
class Program
{
    static void Main(string[] args)
    {
        var driver = new DriverForTests();

        var tc = new MainTestConstructor();

        var parser = new TestConditionParser(String.Join(',', args), true);
        tc.conditions = parser.resultCondition;


        driver.ExecuteTests
        (
            new TestConstructor[] { tc },
            new DriverForTests.ExecuteTestsOptions
            {
                sleepInMs_ForFirstOutput = 10*1000,
                doKeepLogFile            = false // || true
            }
        );
    }
}

class MainTestConstructor : TestConstructor
{
    public MainTestConstructor()
    {}

    public override void CreateTasksLists(ConcurrentQueue<TestTask> tasks)
    {
        // Вызываем, чтобы загрузилась сборка
        if (Keccak_sha_3_512_test.canCreateFile) {}
        /*
        var PTT = new ParallelTasks_Tests(this, canCreateFile: canCreateFile);
        TestConstructor.addTasksForQueue
        (
            source:     PTT.getTasks(),
            tasksQueue: tasks
        );
        */

        // Получаем все задачи, которые могут быть автоматически собраны из данного домена приложения
        var list = this.getTasksFromAppDomain
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
        TestConstructor.addTasksForQueue(list, tasks);
    }
}
