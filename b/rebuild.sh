# Файл нужно запускать из корневой директории репозитория VinKekFish
configuration=$2
testTags=$1

if [ -z "$configuration" ]
then
    configuration="Release"
fi


if [ -z "$testTags" ]
then
    testTags="?"
fi

# Очистка сборочной директории
rm -fR ./build

# Очистка проекта builder
rm -fR ./src/builder/builder/bin/
rm -fR ./src/builder/builder/obj/
dotnet clean ./src/builder/builder/
dotnet clean './src/main/1 BytesBuilder/'
dotnet clean './src/main/2 generator/'
dotnet clean './src/main/3 cryptoprime/'
dotnet clean './src/main/4 utils/'
dotnet clean './src/main/5 main-crypto/'
dotnet clean ./src/tests/


# Вызов билда
bash ./b/build.sh "$testTags" "$configuration"
