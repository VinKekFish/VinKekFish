# Построение

Программа предназначена для работы под Linux

## 0. Установить .NET 7.0
https://dotnet.microsoft.com/download


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
bash ./b/rebuild.sh Debug

Или быстро перестроить решение (если решение было построено)
bash ./b/fbuild.sh Debug

В случае успешного перестроения последняя строчка консоли будет начинаться с
"Builder successfully ended"


## 3. Исполняемые файлы проекта
Исполняемые файлы проекта билдятся в директорию ./build


# Изменение состава проектов

В проекте нет решения, строительство осуществляется программой с исходниками [src/builder/builder/](src/builder/builder/)
и вызовом dotnet на каждый проект в отдельности

При появлении нового проекта
1. Внести его билд в файл [src/builder/builder/Build-projects.cs](src/builder/builder/Build-projects.cs)
2. Внести очистку директории bin этого проекта в [b/rebuild.sh](b/rebuild.sh) (и, если нужно, dotnet clean)
