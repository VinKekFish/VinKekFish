namespace VinKekFish_EXE;
public enum ProgramErrorCode
{                                 // <summary>Успех</summary>
    success         
        = 0,                      // <summary>Запрошена версия программы</summary>
    version         
        = 100,                    // <summary>Неверные аргументы</summary>
    noArgs
        = 101,
    errorRegimeArgs
        = 102,

    // <summary>Нет правильного аллокатора для памяти (несовместимая операционная система)</summary>
    wrongMemoryAllocator = 1,

    // <summary>Работа программы досрочно прекращена пользователем</summary>
    AbandonedByUser = 103,
    /// <summary>Работа программы прекращена по какой-либо причине, которая не отражена в коде возврата (отражена в консоли)</summary>
    Abandoned       = 104,

    // Возвращаемы коды для работы в режиме сервиса
    noArgs_Service
        = 10_001,
    noOptions_Service
        = 10_002,

    // <summary>Работа программы прекращена в результате получения программой некорректных входных значений</summary>
    wrongCryptoParams = 20_001,
    // <summary>Работа программы прекращена из-за несовпадения хеша расшифрованных данных.</summary>
    wrongCryptoHash = 20_002,

};
