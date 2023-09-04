# VinKekFishBase_KN_20210525
docs::docs:m0vbJGmf34Sx5nKnLnpz:

Реализация в
src/main/5 main-crypto/VinKekFish/VinKekFish-20210525/
шаблон для поиска
::docs:m0vbJGmf34Sx5nKnLnpz:
Для поиска используйте
fgrep -rm 1 --exclude-dir '.vscode' '::docs:m0vbJGmf34Sx5nKnLnpz:'



Полная реализация шифра для K=[1;3;5;7;9;11;13;15;17;19]. Это все допустимые для VinKekFish значения K.

# Инициализация

Простейшая инициализация
(примеры, см. тесты src/tests/src/main/VinKekFish/Base.cs)

    var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
    using var key = allocator.AllocMemory(ushort.MaxValue);
    user_must_fill_key(key);    // Пользовательская функция, заполняющая значение ключа

    using var k1t1  = new VinKekFishBase_KN_20210525(CountOfRounds: VinKekFishBase_etalonK1.MAX_ROUNDS);
    k1t1 .Init1();
    k1t1 .Init2(key);



