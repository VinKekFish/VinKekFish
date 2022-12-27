# VinKekFish

For English see translator
https://translate.yandex.ru/?lang=ru-en
or
https://translate.google.com/?sl=ru&tl=en&op=translate

Не уверен, что доведу проект до конца.

Это проект небольшого криптографического приложения с ключом 4096 битов симметричного шифрования. Основной шифр - самодельный VinKekFish стойкостью 4096 битов (на основе комбинации keccak и Threefish).
Так как шифр не проходил криптоанализ, стойкость не гарантируется. Так что на ваш страх и риск.

Реализация на C#.

Пока для пользователя здесь нет ничего интересного. Приходите в 2024 году, возможно, я допишу.


Для программистов.

Реализованы примитивы keccak и Threefish:

keccak версии 512 битов (максимальный из возможных)

	cryptoprime/keccak.cs

	функция getHash512 - это пример, как рассчитать хеш (обратите внимание, этот хеш отличается от sha-3)
	https://github.com/VinKekFish/VinKekFish/blob/master/vinkekfish/keccak/keccak-20200918/keccak-base-20200918.cs

	генератор псевдослучайных чисел (очень медленно работает)
	https://github.com/VinKekFish/VinKekFish/blob/master/vinkekfish/keccak/keccak-20200918/Keccak_PRNG_20201128.cs


Threefish версии 1024 битов (тоже максимальный; только на шифрование). Осторожно, там нужно вычислить третий tweak (tweak1 ^ tweak2) и передать его в массиве твиков.

	cryptoprime/Threefish/Threefish_Static_Generated.cs


Примеры использования смотрите в тестах в проекте main_tests.


VinKekFish пока не реализован
(плохо протестированная и, похоже, нерабочая версия для K = 1 в https://github.com/VinKekFish/VinKekFish/blob/master/cryptoprime/VinKekFish/VinKekFishBase_etalonK1.cs )

	Описание примитива
		/main_tests/Задачи%20и%20другое/Криптография/Размышления/VinKekFish.md

	Сам примитив
		cryptoprime.VinKekFish.VinKekFishBase_etalonK1

	Генератор ключей
		/VinKekFish/VinKekFish/VinKekFish-20210419/VinKekFish-20210419/VinKekFish_k1_base_20210419_keyGeneration.cs

