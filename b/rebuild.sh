# Файл нужно запускать из корневой директории репозитория VinKekFish
configuration=$1

if [ -z "$configuration" ]
then
    configuration="Release"
fi

# Очистка сборочной директории
rm -fR ./build

# Очистка проекта builder
rm -fR ./src/builder/builder/bin/
dotnet clean ./src/builder/builder/


# Вызов билда
bash ./b/build.sh $configuration
