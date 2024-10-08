# Описание команд верхнего уровня режима auto

Концепция шифрования описана в файле [concept.md](concept.md)

## Режим отладки команд
`debug:`

Команда `debug:` выводит дополнительные сообщения для подтверждения команд, которые вводятся пользователем.
Команда только включает режим отладки команд, выключить режим отладки после его включения невозможно. По умолчанию режим отладки команд выключен.

Команда `debug:` должна быть выполнена до других команд.

### Режим отладки
При неверном вводе команд в режиме отладки выводится сообщение об ошибке и программа даёт возможность продолжить ввод. Без режима отладки программа завершает свою работу.

## disk. Команда шифрованного логического диска

Данная команда используется для 512-битного шифрования диска.
Описание команды см. [disk.md](disk.md).


## enc. Команда зашифрования
`enc:`

Используется для шифрования файлов пользователя (исключая первичный ключевой файл, для него используется команда key-main). Описание команды см. [enc.md](enc.md).

## dec. Команда расшифрования
`dec:`

## Команда генерации первичного ключа шифрования

Команда имеет синонимы:
```
key-main:
key-primary:
key_gen_main:
key-gen-main:
```
Два возможных применения:
1. Генерация случайного файла, пригодного к использованию в качестве ключа.
2. Генерация ключевого форматированного файла, специфичного для VinKekFish.

Описана в файле [key-main.md](key-main.md)

## Команда генерации пароля

## end. Команда выхода без операций
`end:`

Команда ничего не делает, просто прекращает работу программы с кодом [ProgramErrorCode.AbandonedByUser](./../ProgrammErrorCode.cs).
