namespace VinKekFish_EXE;
public enum ProgramErrorCode
{
    success         
        = 0,
    version         
        = 100,
    noArgs
        = 101,
    errorRegimeArgs
        = 102,

    // Возвращаемы коды для работы в режиме сервиса
    noArgs_Service
        = 10_001,
};