# VinKekFish

[Руководство по построению][./build.md]
[Общее описание проекта](https://github.com/VinKekFish)

Программа требует установленной [.NET 7.0](https://dotnet.microsoft.com/download).
Часть функций работает только под Linux (требуются пути "/proc/meminfo", "/dev/random")
    Поиск по шаблону ::onlylinux.sOq1JvFKRxQyw7FQ даст точки зависимости


# Каталоги


## [/b](/b)
Содержит скрипты для сборки (билда) проекта. Запускать с рабочей директорией /

rebuild.sh
            полный ребилд проекта с запуском всех тестов
build.sh 
            ребилд проекта с ребилдом сборшика
fbuild.sh
            "быстрый" билд проекта без ребилда сборщика и с запуском только очень малого количества тестов


## [/Docs](Docs)

Каталог с документацией.

### [/Docs/External](Docs/External)

Каталог с внешними документами, использованными в проекте, и сторонней реализацией ThreeFish.

    SkeinFish-0.5.0.zip

        сторонняя реализация ThreeFish https://github.com/nitrocaster/SkeinFish/


    skein.pdf

        Описание Skein и TreeFish от Брюса Шнейера

            https://www.schneier.com/academic/skein/threefish/


    NIST.FIPS.202.pdf - описание алгоритма keccak в стандартной форме

    Keccak-reference-3.0.pdf - описание алгоритма keccak в авторской форме


### [/Docs/Dev](Docs/Dev)
    
	Документы, касающиеся разработки (описание алгоритма VinKekFish, контрольные списки, планы, задачи)

Содержание: [README.md](./Docs/Dev/README.md)


## [/src/](src)

Общие исходные файлы

### [/src/builder/](src/builder/)
	Построитель проекта: строит проект и запускает тесты автоматически
[Описание директории](src/builder/README.md)

### [/src/main/](src/main/README.md)
Непосредственно файлы базовых примитивов криптографии (keccak и ThreeFish).
Вспомогательные классы для работы с массивами байтов и битов: BytesBuider и прочие.

### [/src/tests/](src/tests/)
Файлы тестов

Тесты находятся в папке src примерно в таком же расположении, в котором находятся оригинальные тестируемые файлы

