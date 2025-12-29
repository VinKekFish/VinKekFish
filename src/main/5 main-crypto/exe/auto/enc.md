Данная функция не проверена и находится в тестировании (если найду силы для этого).

# enc. Команда зашифрования
# Требования
Работает только под Linux.

В настоящий момент времени, требуется, чтобы сервис энтропии `vkf` работал (работают пути типа `/dev/vkf/crandom`). Сервис автоматически устанавливается при установке программы. Если этого нет, то программа не сможет нормально зашифровать файл.

# Файл конфигурации
## in
Имя расшифрованного файла, который нужно зашифровать.

Допускается указывать пустое
`in:`
(будет выдано приглашение к вводу при условии наличия zenity и визуального интерфейса)

Примеры:
`in:записки.txt`
`in:/inRam/Письмо.doc`

Допускается указание только одного файла за один вызов команды шифрования.


## out
Имя файла-результата. Этот файл будет содержать зашифрованные данные.

Пример:
`enc:`
`out:записки.txt.vkf`
`out:записки.txt.$date$.vkf`

При указании шаблона "$date$" (как в примере выше), будет подставлена текущая дата и время. Шаблон регистро-зависимый ("$Date$" не будет работать).

При указании пустого "enc:", имя файла будет сформировано из имени из опции "dec:" (уже должна быть указана) по шаблону `file.$date$.vkf`.

# key


# alg
"std.1.202510"
"std.3.202510"
"short.1.202510"


# pwd

# pwd-simple

# novkfrandom


# Примеры
Зашифрование 4096 бит симметричного шифрования.
```
# vkf auto /inRamA/enc.conf

debug:
enc:

in:/inRamA/txt
out:/inRamA/txt.vkf
key:/inRamA/key
start:
```

Расшифровать
```
# vkf auto /inRamA/dec.conf

debug:
dec:

out:/inRamA/txt
in:/inRamA/txt.vkf
key:/inRamA/key
start:
```

Зашифрование 12288 бит симметричного шифрования.
```
debug:
enc:

in:/inRamA/txt
out:/inRamA/txt.vkf
key:/inRamA/key
alg:std.3.202510
start:
```

Расшифровать
```
# vkf auto /inRamA/dec.conf

debug:
dec:

out:/inRamA/txt
in:/inRamA/txt.vkf
key:/inRamA/key
alg:std.3.202510
start:
```


Зашифрование 12288 бит симметричного шифрования с паролем.
Пароль вводится в два приёма: из таблицы и напрямую с клавиатуры.
```
debug:
enc:

in:/inRamA/txt
out:/inRamA/txt.vkf
key:/inRamA/key
alg:std.3.202510
pwd:true
pwd-simple:true
start:
```

Расшифровать
```
# vkf auto /inRamA/dec.conf

debug:
dec:

out:/inRamA/txt
in:/inRamA/txt.vkf
key:/inRamA/key
alg:std.3.202510
pwd:true
pwd-simple:true
start:
```
