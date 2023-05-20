# [BytesBuilder](1 BytesBuilder)

Вспомогательные классы для работы с массивами байтов и битов
Также вспомогательный (для тестирования) класс, реализующий ThreeFish

# [generator](2 generator)

Генератор исходного кода ThreeFish (более быстрая версия, чем в BytesBuilder)

# [cryptoprime](3 cryptoprime)

Основные примитивы шифрования: ThreeFish (генерированный из [generator](2 generator) ) и keccak
Генерированный класс Threefish_Static_Generated требует подготовки для работы с помощью класса Threefish1024, который вычисляет расширения ключа и твика (ключ и твик подаются из полей объекта Threefish1024).
Threefish_Static_Generated2 является копией Threefish_Static_Generated и оставлен для тестирования, если нужно что-то изменить.
