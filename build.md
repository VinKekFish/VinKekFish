# Построение

Программа предназначена для работы под Linux

## 0. Установить .NET 7.0

Для Ubuntu Linux и Linux Mint необходимо выполнить в командной строке команду
sudo apt install dotnet7
для иных ОС необходимо смотреть
https://dotnet.microsoft.com/download

При возникновении проблем с libfuse (она используется только для создания символьного устройства /dev/vkf/crandom и больше ни для чего)
    Попробуйте закомментировать символом "#" строку "character device in /dev" и строку под ней в файле настроек
    Файл настроек располагается в ./Crypto/VinKekFish/src/main/5 main-crypto/exe/service/options_files/service.options
    После этого нужно сребилдить проект заново и запустить скрипт установки (или просто вручную заменить файл настроек в /opt/VinKekFish/options/ и перезапустить сервис vkf [sudo systemctl restart vkf])


## 1. Извлечь вспомогательный репозиторий

mkdir Директория_где_вы_хотите_поместить_проекты
cd Директория_где_вы_хотите_поместить_проекты

mkdir -p ./Compiler
cd Compiler
git clone https://github.com/VinCryptoOS/tree-lang-main

cd ..

mkdir -p ./Crypto
cd Crypto
git clone https://github.com/VinKekFish/VinKekFish


## 2. Вызвать один из сценариев билда
Перейти в директорию VinKekFish
cd VinKekFish


Полностью перестроить решение
bash ./b/rebuild.sh

Или быстро перестроить решение (если решение было построено)
bash ./b/fbuild.sh

В случае успешного перестроения последняя строчка консоли будет начинаться с
"Builder successfully ended"
При построении тесты запускаются автоматически. При успешном билде, обычно, не должно быть проблем с сообщениями о том, что тесты прошли плохо. Однако, успешный билд иногда может пройти с плохими тестами на производительность (читайте сообщения тестов).


## 3. Исполняемые файлы проекта
Исполняемые файлы проекта билдятся в директорию ./Crypto/VinKekFish/build
(после выполнения пункта 2 мы уже находимся в ./Crypto/VinKekFish и оттуда запускаем билд)

## 4. Установка программы

Скрипт установки ./Crypto/VinKekFish/build/arcs/dotnet7/exe/install.sh
В скрипте необходимо поменять второй параметр на путь к файлу vkf-dotnet7.7z . После билда архивный файл расположен в папке ./Crypto/VinKekFish/build/arcs/.

После этого скрипт запускается из-под пользователя root.


# Изменение состава проектов

В проекте нет решения, строительство осуществляется программой с исходниками [src/builder/builder/](src/builder/builder/)
и вызовом dotnet на каждый проект в отдельности

При появлении нового проекта
1. Внести его билд в файл [src/builder/builder/Build-projects.cs](src/builder/builder/Build-projects.cs)
2. Внести очистку директории bin этого проекта в [b/rebuild.sh](b/rebuild.sh) (и, если нужно, dotnet clean)
