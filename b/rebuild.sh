# Файл нужно запускать из корневой директории репозитория VinKekFish
configuration=$1
testTags=$2

if [ -z "$configuration" ]
then
    configuration="Release"
fi

# "-not_to_execute" ставим для того, чтобы build.sh понимал,
# что мы передали параметр и не подставлял туда значения сокращённого тестирования
if [ -z "$testTags" ]
then
    testTags="-not_to_execute"
fi

# Очистка сборочной директории
rm -fR ./build

# Очистка проекта builder
rm -fR ./src/builder/builder/bin/
rm -fR ./src/builder/builder/obj/
dotnet clean ./src/builder/builder/
dotnet clean ./src/main/cryptoprime/
dotnet clean ./src/tests/


# Вызов билда
bash ./b/build.sh "$configuration" "$testTags"
